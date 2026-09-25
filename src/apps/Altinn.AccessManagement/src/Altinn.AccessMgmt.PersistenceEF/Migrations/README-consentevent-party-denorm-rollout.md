# Rollout: denormalize the party columns onto `consent.consentevent`

## Why
`GetConsentEventsForParty` pages a party's consent events ordered by `consenteventid`, but the party
filter lives on `consent.consentrequest`, not on the event. Since #3999 the filter is

```sql
WHERE topartyuuid = @partyUuid OR handledbypartyuuid = @partyUuid
```

so a party matches as **recipient** (`topartyuuid`) *or* as **handler** (`handledbypartyuuid`). At the
event table's production scale, resolving that filter through the request table either seq-scans the whole
event table or does a per-event request lookup, which is slow. The materialized-CTE rewrite (currently
shipped) improves it substantially but still degrades for parties that own very large numbers of requests.

Fix: copy **both** party columns onto `consentevent` and index each `(<party column>, consenteventid)`.
The read query then filters the event table directly, no join.

> Both columns are required. `topartyuuid` alone would silently drop every event on requests where the
> party is only the handler — exactly the OR above. The denormalization, the backfill, the feed indexes
> and the Phase 4 query all carry both columns for that reason.

## Current phase
**Phase 1 is what this PR ships.** The columns and the insert-time forward-fill are in; the read query
stays on the materialized-CTE form. Phases 2–5 are operational steps run between deploys, tracked here.

## Why it is phased
Two operations cannot live in a transactional migration:
- the backfill `UPDATE` of the full event table (long transaction + table bloat + lock), and
- `CREATE INDEX CONCURRENTLY` (cannot run inside a transaction).

So they are run manually between deploys. The hard constraint: the join-free read query (Phase 4) must
not ship until every existing row is reconciled with its parent request (Phase 2) and both feed indexes
exist (Phase 3) — otherwise matching events are silently dropped from results.

---

## Phase 1 — Deploy: columns + forward-fill  (this change set)
- EF migration `20260922113000_ConsentEventPartyDenormColumns` adds the two nullable columns
  (`topartyuuid`, `handledbypartyuuid`) via `migrationBuilder.Sql` — metadata-only, no table rewrite —
  and they are mirrored onto the `consentevent` table in `ConsentSchema.sql` for fresh provisioning.
  The **feed indexes are deliberately not in `ConsentSchema.sql`**: a transactional build in the
  baseline would fail on an old table that lacks the columns and would lock a large table where it
  does not. They are built out of band in Phase 3, on fresh databases as well.
- `ConsentRepository.EventQuery` populates both columns from the parent request on every insert, so all
  **new** events are populated immediately after deploy.
- The read query stays on the materialized-CTE form; it returns correct results while existing rows are
  still NULL, backed by `idx_consentrequest_topartyuuid` and `idx_consentrequest_handledbypartyuuid`
  (owned by EF migration `20260908092106_ConsentRequestPartyIndexes`, #4015).

**Verify after deploy:** new events get non-null values (handler may be null when the request has none):
```sql
SELECT count(*) FILTER (WHERE topartyuuid IS NOT NULL) AS to_filled,
       count(*) FILTER (WHERE topartyuuid IS NULL)     AS to_remaining
FROM consent.consentevent;
```
`to_filled` should start increasing as events are created.

## Phase 2 — Backfill existing rows  (manual, batched)
Run against prod in a session that is NOT inside a long transaction. Reconciles both columns with the
parent request; `IS DISTINCT FROM` makes it null-safe (a request with no handler yields a null handler
on its events, which is correct). Repeat until it reports `UPDATE 0`; each batch commits on its own, so
it is safe to pause/resume.
```sql
WITH batch AS (
    SELECT ce.consenteventid
    FROM consent.consentevent ce
    JOIN consent.consentrequest cr ON cr.consentrequestid = ce.consentrequestid
    WHERE ce.topartyuuid        IS DISTINCT FROM cr.topartyuuid
       OR ce.handledbypartyuuid IS DISTINCT FROM cr.handledbypartyuuid
    LIMIT 50000
)
UPDATE consent.consentevent ce
SET topartyuuid        = cr.topartyuuid,
    handledbypartyuuid = cr.handledbypartyuuid
FROM consent.consentrequest cr, batch
WHERE ce.consenteventid = batch.consenteventid
  AND cr.consentrequestid = ce.consentrequestid;
```

**Exit gate:** must be 0 before Phase 4 — every event reconciled with its parent.
```sql
SELECT count(*)
FROM consent.consentevent ce
JOIN consent.consentrequest cr ON cr.consentrequestid = ce.consentrequestid
WHERE ce.topartyuuid        IS DISTINCT FROM cr.topartyuuid
   OR ce.handledbypartyuuid IS DISTINCT FROM cr.handledbypartyuuid;   -- expect 0
```

## Phase 3 — Build the feed indexes  (manual, concurrent)
These are not created by any migration — run them on every database, fresh ones included (instant on an
empty table), before switching the read query in Phase 4.
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_consentevent_topartyuuid_feed
    ON consent.consentevent (topartyuuid, consenteventid);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_consentevent_handledbypartyuuid_feed
    ON consent.consentevent (handledbypartyuuid, consenteventid);
```
If interrupted either leaves an INVALID index — drop and retry:
```sql
DROP INDEX CONCURRENTLY IF EXISTS consent.idx_consentevent_topartyuuid_feed;
DROP INDEX CONCURRENTLY IF EXISTS consent.idx_consentevent_handledbypartyuuid_feed;
```
**Verify:** both indexes are valid and the join-free query uses them (no seq scan of consentevent):
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT ce.consentrequestid, ce.consenteventid, ce.eventtype, ce.created
FROM consent.consentevent ce
WHERE (ce.topartyuuid = '<party-with-many-events>'::uuid
    OR ce.handledbypartyuuid = '<party-with-many-events>'::uuid)
  AND ce.consenteventid < '<recent-uuidv7-bound>'::uuid
ORDER BY ce.consenteventid ASC
LIMIT 100;
```
Expect a `BitmapOr` of the two feed indexes feeding a bounded sort. Because the predicate is an OR, the
planner cannot walk a single index in `consenteventid` order and stop at `LIMIT`; it collects the
party's matching rows from both indexes, then sorts and limits. That is still bounded by the party's own
events (index-driven), not the full event table.

## Phase 4 — Deploy: switch read query  (gated on Phase 2 = 0 and Phase 3 valid)
In `ConsentRepository.GetConsentEventsForParty`, replace the materialized-CTE body with the join-free
form (parameters unchanged):
```sql
SELECT ce.consentrequestid, ce.consenteventid, ce.eventtype, ce.created
FROM consent.consentevent ce
WHERE (ce.topartyuuid = @partyUuid OR ce.handledbypartyuuid = @partyUuid)
  AND ce.consenteventid < @uuid7SafetyBound
  AND (@consentRequestId IS NULL OR ce.consentrequestid = @consentRequestId)
  AND (@eventTypes       IS NULL OR ce.eventtype = ANY(@eventTypes::consent.event_type[]))
  AND (@createdAfter     IS NULL OR ce.created >= @createdAfter)
  AND (@createdBefore    IS NULL OR ce.created <  @createdBefore)
  AND (@continueFrom     IS NULL OR ce.consenteventid > @continueFrom)
ORDER BY ce.consenteventid ASC
LIMIT @pageSize;
```
**Verify:** worst-party latency drops sharply, and the result set matches the previous (materialized-CTE)
query for a sample party — including a party that appears **only** as handler.

## Phase 5 — Deploy: cleanup  (optional, after Phase 4 is live)
- Do **not** drop `idx_consentrequest_topartyuuid` / `idx_consentrequest_handledbypartyuuid`. They are
  owned by EF migration #4015 and still back other consentrequest lookups.
- `NOT NULL` is intentionally **not** enforced: `handledbypartyuuid` is legitimately null for requests
  with no handler, and legacy `topartyuuid` may be null on old requests.

---

## Index summary
| Index | Table | Purpose | Owner / lifetime |
|-------|-------|---------|------------------|
| `idx_consentevent_topartyuuid_feed` | consentevent | join-free feed, recipient predicate | this PR — permanent |
| `idx_consentevent_handledbypartyuuid_feed` | consentevent | join-free feed, handler predicate | this PR — permanent |
| `idx_consentrequest_topartyuuid` | consentrequest | interim CTE + other lookups | EF #4015 — keep |
| `idx_consentrequest_handledbypartyuuid` | consentrequest | interim CTE + other lookups | EF #4015 — keep |

## Rollback
- Phases 1–3 are additive and safe to leave in place; they do not change read results.
- If Phase 4 misbehaves, revert the query to the materialized-CTE form (still backed by the
  consentrequest indexes) and leave the columns and feed indexes in place.
