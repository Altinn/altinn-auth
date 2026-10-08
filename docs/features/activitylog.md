# Activity log — technical description

## Solution overview

The activity log is an event log over every access change in Access Management — assignments, delegations and requests, including their package, resource and instance children. It answers "who did what with my accesses, when" for endusers, support and internal tooling.

### How entries are written

23 AFTER triggers on the 10 source tables write events in the same transaction as the change itself — no application code can forget to log, and backend jobs, imports and sync are captured too. Attribution reuses the audit mechanism already in production: INSERT reads the row's own audit columns, UPDATE reads session variables set by EF on save, DELETE reads the audit context from the session's temp table. Name snapshots are resolved from the live tables with fallback to the history schema when the parent row is already gone (cascades). If the trigger fails, the whole change rolls back — the same guarantee as audit. All trigger DDL is versioned through the EF migrations via the same custom SQL generator that emits the audit triggers.

### Storage and partitioning

One range-partitioned table (`dbo.activitylog`) on the event timestamp, with denormalized name snapshots (from/to/via/performed by, role, package/resource) — queries need no joins, and names read as they were when the event happened. Operation id and parent id are stored on the rows for future event grouping.

The partition layout has three parts:

- **Yearly partitions 2000–2025** carry the backfilled history. That data is written once by the backfill and never changes, and historical queries are rare — monthly granularity here would mean 300+ partitions with nothing gained; yearly gives 26.
- **Monthly partitions from 2026-01** carry live data. Most queries hit recent time ranges, so partition pruning keeps them on a few small partitions, per-partition indexes stay small, and future archiving can detach one month at a time.
- **A default partition** catches anything outside the created ranges, so a business transaction can never fail because a partition is missing.

The migration creates all of this up front, including monthly partitions 24 months ahead. Ongoing maintenance is the `dbo.activitylog_ensure_month_partitions(months_ahead)` function (SECURITY DEFINER, executable by the app role), which creates any missing months up to the horizon. The FFB job `ActivityLogPartitions` calls it: it can be run manually from the backfill page or put on a recurring schedule via the FFB job scheduler, and it only ever *creates* partitions. Retention is unlimited for now, so there is deliberately no cleanup/detach job — that comes together with a retention decision. Should maintenance ever be forgotten, writes simply land in the default partition (nothing fails), but those months would then have to be moved out of it manually before their partitions can be created — which is why the job should be on a schedule well before the 24-month runway runs out.

### Activity type catalog

35 event types (combinations of type/subtype/trigger/status) with name and description in Bokmål, Nynorsk and English, defined in code constants and seeded into a helper table (`dbo.activitytype`). The log keeps its four raw dimension columns; the catalog is a pure lookup layer — type definitions can change without touching a table with ~86 million rows.

### History and rollout

A backfill job generates events from existing data, with a read-only analysis mode first (PROD ≈ 86.6 million events, roughly 50–80 GB — manageable). Names on backfilled events are resolved **as of each event's time** from the audit history (the version valid at the event wins), so they carry the same point-in-time snapshots as trigger-written events — with the precision the audit history itself has (raw-SQL writers that never refreshed the audit columns stay imprecise). A manual install/rollback script installs the whole schema in test environments without EF migration bookkeeping, so triggers, analysis and backfill can be tried at scale before the real migration ships; guards refuse both operations in any EF-managed environment.

### Dogfooding

The FFB tool has a complete page against all environments — party anchor with direction, an activity type picker (tree with cascading selection and descriptions), filter pickers driven by the filter value endpoints, and grouping on operation/parent. This is where the UX flow the frontend will build has been verified.

### Alternatives considered

- **Logging from application code** — rejected: many write paths (APIs, jobs, imports, A2 sync) make it easy to miss events, and logging happens outside the transaction.
- **Outbox/event-based** — rejected: eventual consistency and more moving parts, and operational experience already shows the outbox solution is fragile. (The log is in fact being considered as a replacement data source for notifications, not the other way around.)
- **Deriving the log from the audit/history tables at read time** — rejected: would require temporal joins across 10+ tables per query over ~86 million events, with no stable event semantics and no name snapshots.
- **CDC/logical replication** — rejected: new infrastructure, latency, and the enrichment layer would still be needed.

**Why triggers won:** complete (captures every write path, including manual ones), atomic (same transaction, same guarantee as audit), correct snapshots at event time, one read-optimized table with no joins — and zero new infrastructure, since the pattern (trigger DDL in migrations, audit attribution) is already proven in this schema.

## Enduser API (v2 areas)

The enduser surface is split into three **areas**, each mounted under its domain root in the v2 API (`Controllers/V2`, `accessmanagement/api/v{version}/enduser/…`). The area is the authorization boundary and decides which slice of the log exists there — there is no cross-area type filter:

| Area root | Events | Feature flag | Auth |
|---|---|---|---|
| `…/enduser/connections/activitylog` | Assignment + Delegation (Maskinporten schema events excluded) | `AccessManagement.Enduser.ConnectionsActivityLogApi` | The connections model keyed on `direction`: directional read scopes + the person access-manager rule, + access-management party read |
| `…/enduser/request/activitylog` | Access requests incl. package/resource children and status changes | `AccessManagement.Enduser.RequestActivityLogApi` | The existing requests read scopes + access-management party read, like the neighboring request endpoints |
| `…/enduser/maskinporten/activitylog` | Maskinporten schema delegations (the Supplier-role slice) | `AccessManagement.Enduser.MaskinportenActivityLogApi` | Directional maskinporten scopes like the neighboring endpoints — supplier read for `direction=from`, consumer read for `direction=to` — + the maskinporten PDP read policy |

Every area exposes the same four routes:

| Route | Purpose |
|---|---|
| `GET {area}` | The area's log entries, newest first |
| `GET {area}/filter/{field}` | Values occurring in the party's slice for one filter field |
| `GET {area}/filter/fields` | The filter fields the area offers (static, anonymous, cached 1h) |
| `GET {area}/types` | The catalog entries the area accepts as `typeId` input (static, anonymous, cached 1h) |

### 1. Main query — `GET {area}`

Returns the area's log entries for the party, ordered newest first (`when` descending, id as tiebreaker).

**Anchoring:** `party` and `direction` are both **required**. `direction` is `from` (access given by the party) or `to` (access received by the party) — the directional scope policies and the person access-manager rule key on this parameter, and `via` returns with the client-administration needs. `from` and `to` are plain counterpart filters: they may equal the party (self-events are a valid query), and the service overwrites the anchored side with the party.

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

**typeId expansion:** each `typeId` references one catalog entry and expands to its whole `(type, subtype, trigger, status)` combination; multiple values are OR'ed as complete combinations. A catalog entry with `subtype = null` matches only main-record entries (exact null match), while `status = null` is a wildcard matching any status. Unknown ids — and ids outside the area's accepted set (`GET {area}/types`) — give `400`.

**Paging:** page-based via `pageSize` (default 100, clamped to 1–1000) and `pageNo` (0-based). The response is the standard paginated envelope: the items plus `links.next`, a ready-to-follow URL present only when more entries exist (built from the request with `pageNo` incremented).

**Response items** (`ActivityLogDto`): the event dimensions (`type`, `subtype`, `trigger`, `status`), `when`, actor and channel (`byId`/`byName`, `sourceId`/`sourceName`), `operationId`, the relation with name snapshots (`fromId`/`fromName`/`fromType`, `toId`/`toName`/`toType`, `viaId`/`viaName`/`viaType`, `roleId`/`roleName`, `viaRoleId`/`viaRoleName`), the object (`packageId`/`packageName`, `resourceId`/`resourceName`, `instanceId`), row identity (`itemId`, `parentId`), a `details` JSON blob (previous status, request action, provenance), and `activityTypeId` — the catalog entry resolved with the most-specific-wins rule (exact status match, else the status-null fallback), so clients can display catalog name/description without mapping the raw dimensions themselves.

```
GET /accessmanagement/api/v2/enduser/connections/activitylog?party={party}&direction=from&typeId={guid}&after=2026-01-01T00:00:00Z&pageSize=50
```

### 2. Filter value endpoints — `GET {area}/filter/{field}`

*(They return the distinct values occurring in the log, scoped to the current search.)*

Returns the distinct `(id, name)` pairs occurring in the party's slice of the area for one field, so filter pickers only offer values that actually give hits. `field` must be one of the fields the area offers (`GET {area}/filter/fields`) — e.g. the maskinporten area offers no `role` field, since the Supplier role is pinned; anything else gives `400`.

- **Same filter surface as the main query** — party, direction and all filter parameters apply, so the picker narrows along with the search the user has already built.
- **Own-field rule:** the filter for the field being looked up is ignored (a lookup on `package` disregards any `package` filter), so users can extend a multi-select without the list collapsing to their current choices. The `party` anchor is never ignored.
- **`term`:** case-insensitive substring match on name only.
- **`orderBy`:** `Name` (default, alphabetical — stable across pages) or `When` (newest occurrence per value first — new events can shift pages).
- **Duplicates are intentional:** names are point-in-time snapshots, so one id can recur with different names (e.g. after a rename); all pairs are returned so every historical label is findable. For `source` and `activitytype` the names come from the respective catalogs instead of snapshots.
- **Language:** `activitytype` values are translated from the catalog (bokmål, nynorsk, english via `Accept-Language`) before term matching and ordering, so the term matches the names the user actually sees. Snapshot names are data and are never translated.
- **The Supplier role never appears as a value:** Maskinporten schema events never reach the regular surfaces (see cross-cutting behavior), so their role is not offered either.
- **Paging and envelope:** identical to the main query (`pageSize`/`pageNo`, `links.next`).

```
GET /accessmanagement/api/v2/enduser/connections/activitylog/filter/package?party={party}&direction=from&term=skatt&pageSize=20
```

### 3. Area discovery — `GET {area}/filter/fields` and `GET {area}/types`

`filter/fields` lists the filter fields the area offers (the valid values for the filter route), and `types` lists the catalog entries the area accepts as `typeId` input — `ActivityTypeDto` with `id`, the key fields (`type`, `subtype`, `trigger`, `status`), `name` and `description`. The maskinporten slice cannot be derived from the global catalog (Supplier events share the assignment type), which is why each area serves its own accepted subset.

Both are static metadata without personal data, hence anonymous and response-cached (1 hour, any location; `types` varies on `Accept-Language`). `types` serves `name` and `description` in the requested language — bokmål, nynorsk or english, translated in memory from the same constant lists the ingest writes to the translation table. Content only changes on deploy; the source of truth is `ActivityTypeConstants`, which also seeds the `dbo.activitytype` helper table.

```
GET /accessmanagement/api/v2/enduser/maskinporten/activitylog/types
```

### Cross-cutting behavior

- **Maskinporten schema events live only in the maskinporten area, and the two slices never mix:** the Supplier role is used exclusively for Maskinporten schema delegations, and the service itself has two disjoint surfaces — the regular one always excludes the role (connections and request), the maskinporten one serves only it. There is no opt-in flag; no caller combination can produce a mixed result.
- **Validation:** empty `party`, missing/`via` `direction`, `typeId` values outside the area, and filter fields the area does not offer all return `400` with problem details. The directional scope policies key on the same `direction` parameter, so on the connections and maskinporten areas a query without a valid direction is rejected with `403` before validation runs.
- **Feature flags:** one per area — `AccessManagement.Enduser.{Connections|Request|Maskinporten}ActivityLogApi` — all declared in the app's own terraform next to the other AccessManagement flags (created disabled; toggled per environment in App Configuration). The portal frontend uses these same area endpoints; there is no separate internal surface.
- **Ordering:** `(when desc, id desc)` — a total, stable order. Page traversal uses OFFSET, so events arriving mid-traversal can shift rows between pages; pin the window with `before` (and `after`) when a stable walk through a live log matters.
- **No joins at read time:** every name in the response is a denormalized snapshot from the log table itself; the log is served from a single range-partitioned table.
