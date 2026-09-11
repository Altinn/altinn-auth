# Rollout: denormalize `topartyuuid` onto `consent.consentevent`

## Why
`GetConsentEventsForParty` pages a party's consent events ordered by `consenteventid`, but the party
filter (`topartyuuid`) lived only on `consent.consentrequest`. At ~28M events the planner either
seq-scans the whole event table or does a per-event request lookup to apply the filter — ~88s in
prod. The materialized-CTE rewrite (currently shipped) brings it to ~12s but still degrades for
parties that own tens of thousands of requests.

Fix: copy `topartyuuid` onto `consentevent` and index `(topartyuuid, consenteventid)`. The query then
becomes a single ordered range scan that stops at `LIMIT pageSize` — O(pageSize) regardless of table
size, no join.

## Why it is phased
Two operations cannot live in a transactional Yuniql migration:
- the backfill `UPDATE` of ~28M rows (long transaction + table bloat + lock), and
- `CREATE INDEX CONCURRENTLY` (cannot run inside a transaction).

So they are run manually between deploys. The hard constraint: the join-free read query (Phase 4)
must not ship until every existing row is backfilled (Phase 2) and the index exists (Phase 3) —
otherwise rows with `topartyuuid IS NULL` are silently dropped from results.

---

## Phase 1 — Deploy: column + forward-fill  (included in this change set)
Already in this branch:
- Migration `00-alter-consentevent-add-topartyuuid.sql` adds the nullable column (metadata-only, no
  table rewrite).
- `ConsentRepository.EventQuery` populates `topartyuuid` from the parent request on every insert, so
  all **new** events are populated immediately after deploy.
- The read query stays on the materialized-CTE form (B); it returns correct results for rows whose
  `topartyuuid` is still NULL, and uses `idx_consentrequest_topartyuuid`.

**Verify after deploy:** new events get a non-null value:
```sql
SELECT count(*) FILTER (WHERE topartyuuid IS NOT NULL) AS filled,
       count(*) FILTER (WHERE topartyuuid IS NULL)     AS remaining
FROM consent.consentevent;
```
`filled` should start increasing as events are created.

## Phase 2 — Backfill existing rows  (manual, batched)
Run against prod in a session that is NOT inside a long transaction. Repeat until it reports
`UPDATE 0`. Each batch commits on its own, so it is safe to pause/resume.
```sql
WITH batch AS (
    SELECT ce.consenteventid
    FROM consent.consentevent ce
    WHERE ce.topartyuuid IS NULL
    LIMIT 50000
)
UPDATE consent.consentevent ce
SET topartyuuid = cr.topartyuuid
FROM consent.consentrequest cr, batch
WHERE ce.consenteventid = batch.consenteventid
  AND cr.consentrequestid = ce.consentrequestid;
```
Driver options: psql `\watch`, a shell loop, or run repeatedly.

**Exit gate:** must be 0 before Phase 4.
```sql
SELECT count(*) FROM consent.consentevent WHERE topartyuuid IS NULL;   -- expect 0
```

## Phase 3 — Build the index  (manual, concurrent)
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_consentevent_topartyuuid_feed
    ON consent.consentevent (topartyuuid, consenteventid);
```
If interrupted it leaves an INVALID index — drop and retry:
```sql
DROP INDEX CONCURRENTLY IF EXISTS consent.idx_consentevent_topartyuuid_feed;
```
**Verify:** index is valid, and the join-free query uses it (no seq scan, double-digit ms):
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT ce.consentrequestid, ce.consenteventid, ce.eventtype, ce.created
FROM consent.consentevent ce
WHERE ce.topartyuuid = '<party-with-many-events>'::uuid
  AND ce.eventtype <> 'created'
  AND ce.consenteventid < '<recent-uuidv7-bound>'::uuid
ORDER BY ce.consenteventid ASC
LIMIT 100;
```

## Phase 4 — Deploy: switch read query  (gated on Phase 2 = 0 and Phase 3 valid)
In `ConsentRepository.GetConsentEventsForParty`, replace the materialized-CTE body with the
join-free form (parameters unchanged):
```sql
SELECT ce.consentrequestid, ce.consenteventid, ce.eventtype, ce.created
FROM consent.consentevent ce
WHERE ce.topartyuuid = @partyUuid
  AND ce.eventtype <> 'created'
  AND ce.consenteventid < @uuid7SafetyBound
  AND (@consentRequestId IS NULL OR ce.consentrequestid = @consentRequestId)
  AND (@eventTypes       IS NULL OR ce.eventtype = ANY(@eventTypes::consent.event_type[]))
  AND (@createdAfter     IS NULL OR ce.created >= @createdAfter)
  AND (@createdBefore    IS NULL OR ce.created <  @createdBefore)
  AND (@continueFrom     IS NULL OR ce.consenteventid > @continueFrom)
ORDER BY ce.consenteventid ASC
LIMIT @pageSize;
```
**Verify:** worst-party latency drops to ms; result set matches the previous (B) query for a sample
party.

## Phase 5 — Deploy: cleanup  (transactional migration, after Phase 4 is live)
The bridge index is now unused; optionally enforce the invariant:
```sql
DROP INDEX IF EXISTS consent.idx_consentrequest_topartyuuid;
-- optional, once Phase 2 verified complete:
ALTER TABLE consent.consentevent ALTER COLUMN topartyuuid SET NOT NULL;
```

---

## Index summary
| Index | Table | Purpose | Lifetime |
|-------|-------|---------|----------|
| `idx_consentrequest_topartyuuid` | consentrequest | serves the interim CTE query (Phase 1–3) | drop in Phase 5 |
| `idx_consentevent_topartyuuid_feed` | consentevent | serves the final join-free query | permanent |

## Rollback
- Phases 1–3 are additive and safe to leave in place; they do not change read results.
- If Phase 4 misbehaves, revert the query to the materialized-CTE form (still supported by
  `idx_consentrequest_topartyuuid`) and do not run Phase 5.
