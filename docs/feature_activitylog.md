# Activity log — technical description

## Solution overview

The activity log is an event log over every access change in Access Management — assignments, delegations and requests, including their package, resource and instance children. It answers "who did what with my accesses, when" for endusers, support and internal tooling.

### How entries are written

23 AFTER triggers on the 10 source tables write events in the same transaction as the change itself — no application code can forget to log, and backend jobs, imports and sync are captured too. Attribution reuses the audit mechanism already in production: INSERT reads the row's own audit columns, UPDATE reads session variables set by EF on save, DELETE reads the audit context from the session's temp table. Name snapshots are resolved from the live tables with fallback to the history schema when the parent row is already gone (cascades). If the trigger fails, the whole change rolls back — the same guarantee as audit. All trigger DDL is versioned through the EF migrations via the same custom SQL generator that emits the audit triggers.

### Storage and partitioning

One range-partitioned table (`dbo.activitylog`) on the event timestamp, with denormalized name snapshots (from/to/via/performed by, role, package/resource) — queries need no joins, and names read as they were when the event happened. Partitions are yearly for backfilled history (2000–2025) and monthly from 2026 and 24 months ahead; a maintenance function creates new months continuously, and a default partition guarantees a business transaction can never fail on a missing partition. Old partitions can be detached/archived cheaply later. Operation id and parent id are stored on the rows for future event grouping.

### Activity type catalog

35 event types (combinations of type/subtype/trigger/status) with name and description in Bokmål, Nynorsk and English, defined in code constants and seeded into a helper table (`dbo.activitytype`). The log keeps its four raw dimension columns; the catalog is a pure lookup layer — type definitions can change without touching a table with ~86 million rows.

### History and rollout

A backfill job generates events from existing data, with a read-only analysis mode first (PROD ≈ 86.6 million events, roughly 50–80 GB — manageable). A manual install/rollback script installs the whole schema in test environments without EF migration bookkeeping, so triggers, analysis and backfill can be tried at scale before the real migration ships; guards refuse both operations in any EF-managed environment.

### Dogfooding

The FFB tool has a complete page against all environments — party anchor with direction, an activity type picker (tree with cascading selection and descriptions), filter pickers driven by the filter value endpoints, and grouping on operation/parent. This is where the UX flow the frontend will build has been verified.

### Alternatives considered

- **Logging from application code** — rejected: many write paths (APIs, jobs, imports, A2 sync) make it easy to miss events, and logging happens outside the transaction.
- **Outbox/event-based** — rejected: eventual consistency and more moving parts, and operational experience already shows the outbox solution is fragile. (The log is in fact being considered as a replacement data source for notifications, not the other way around.)
- **Deriving the log from the audit/history tables at read time** — rejected: would require temporal joins across 10+ tables per query over ~86 million events, with no stable event semantics and no name snapshots.
- **CDC/logical replication** — rejected: new infrastructure, latency, and the enrichment layer would still be needed.

**Why triggers won:** complete (captures every write path, including manual ones), atomic (same transaction, same guarantee as audit), correct snapshots at event time, one read-optimized table with no joins — and zero new infrastructure, since the pattern (trigger DDL in migrations, audit attribution) is already proven in this schema.

## Enduser API

The API surface is three endpoints under `accessmanagement/api/v1/enduser/activitylog`, gated by the `EnableEnduserActivityLogApi` feature flag:

| Endpoint | Purpose | Auth |
|---|---|---|
| `GET /activitylog` | The log itself: entries involving a party, newest first | Enduser activity log read + access management enduser read |
| `GET /activitylog/filters/{field}` | Filter values: the values occurring in the party's log for one field, for populating filter pickers | Same as above |
| `GET /activitylog/types` | The activity type catalog: every valid event combination with display name and description | Anonymous, response-cached 1h |

### 1. Main query — `GET /activitylog`

Returns log entries involving `party`, ordered newest first (`when` descending, id as tiebreaker).

**Anchoring:** `party` (required) must be involved in every entry. The optional `direction` pins which side: `From` (access given by the party), `To` (access received), `Via` (delegations facilitated by the party). Without `direction`, any involvement matches (from, to, via or performed by).

**Filters** (all repeatable; values within one parameter are OR'ed, different parameters are AND'ed):

| Parameter | Type | Matches |
|---|---|---|
| `typeId` | guid | Activity type catalog entries — see expansion rules below |
| `type` / `subtype` / `trigger` / `status` | enum | The raw event dimension columns |
| `from` / `to` / `via` / `by` / `role` / `package` / `resource` | guid | The corresponding id column |
| `source` | guid | The system/channel the change came through |
| `operation` | string | Operation (trace) id — all entries written by one user action |
| `instance` | string | Instance URN |
| `itemId` / `parentId` | guid | The affected row / its main record |
| `after` / `before` | datetime | Bounds on `when` |

**typeId expansion:** each `typeId` references one catalog entry and expands to its whole `(type, subtype, trigger, status)` combination; multiple values are OR'ed as complete combinations. A catalog entry with `subtype = null` matches only main-record entries (exact null match), while `status = null` is a wildcard matching any status. Unknown ids give `400`.

**Paging:** page-based via `pageSize` (default 100, clamped to 1–1000) and `pageNo` (0-based). The response is the standard paginated envelope: the items plus `links.next`, a ready-to-follow URL present only when more entries exist (built from the request with `pageNo` incremented).

**Response items** (`ActivityLogDto`): the event dimensions (`type`, `subtype`, `trigger`, `status`), `when`, actor and channel (`byId`/`byName`, `sourceId`/`sourceName`), `operationId`, the relation with name snapshots (`fromId`/`fromName`/`fromType`, `toId`/`toName`/`toType`, `viaId`/`viaName`/`viaType`, `roleId`/`roleName`, `viaRoleId`/`viaRoleName`), the object (`packageId`/`packageName`, `resourceId`/`resourceName`, `instanceId`), row identity (`itemId`, `parentId`), a `details` JSON blob (previous status, request action, provenance), and `activityTypeId` — the catalog entry resolved with the most-specific-wins rule (exact status match, else the status-null fallback), so clients can display catalog name/description without mapping the raw dimensions themselves.

```
GET /accessmanagement/api/v1/enduser/activitylog?party={guid}&direction=From&typeId={guid}&after=2026-01-01T00:00:00Z&pageSize=50
```

### 2. Filter value endpoints — `GET /activitylog/filters/{field}`

*(Also referred to as facet endpoints; "filter value lookup" is the plain-language name — they return the distinct values occurring in the log, scoped to the current search.)*

Returns the distinct `(id, name)` pairs occurring in the party's log for one field, so filter pickers only offer values that actually give hits. `field` is one of `from`, `to`, `via`, `by`, `role`, `package`, `resource`, `source`, `activitytype`.

- **Same filter surface as the main query** — party, direction and all filter parameters apply, so the picker narrows along with the search the user has already built.
- **Own-field rule:** the filter for the field being looked up is ignored (a lookup on `package` disregards any `package` filter), so users can extend a multi-select without the list collapsing to their current choices. The `party` anchor is never ignored.
- **`term`:** case-insensitive substring match on name only.
- **`orderBy`:** `Name` (default, alphabetical — stable across pages) or `When` (newest occurrence per value first — new events can shift pages).
- **Duplicates are intentional:** names are point-in-time snapshots, so one id can recur with different names (e.g. after a rename); all pairs are returned so every historical label is findable. For `source` and `activitytype` the names come from the respective catalogs instead of snapshots.
- **Paging and envelope:** identical to the main query (`pageSize`/`pageNo`, `links.next`).

```
GET /accessmanagement/api/v1/enduser/activitylog/filters/package?party={guid}&term=skatt&pageSize=20
```

### 3. Activity type catalog — `GET /activitylog/types`

Returns all valid event combinations (currently 35) as `ActivityTypeDto`: `id`, the key fields (`type`, `subtype`, `trigger`, `status`), `name` and `description`. The hierarchy is Type → Subtype (`null` = the main record itself) → Trigger → Status (`null` = fallback for any status; entries with a status override it for that value).

Static metadata without personal data, hence anonymous and response-cached (1 hour, any location). Content only changes on deploy; the source of truth is `ActivityTypeConstants`, which also seeds the `dbo.activitytype` helper table. The `id` values are fixed guids and are what the `typeId` filter accepts.

```
GET /accessmanagement/api/v1/enduser/activitylog/types
```

### Cross-cutting behavior

- **Validation:** empty `party` and unknown `typeId` values return `400` with problem details.
- **Feature flag:** the whole controller sits behind `EnableEnduserActivityLogApi`.
- **Ordering guarantee:** `(when desc, id desc)` — stable and duplicate-free across pages while paging.
- **No joins at read time:** every name in the response is a denormalized snapshot from the log table itself; the log is served from a single range-partitioned table.
