---
step: 21
title: ConnectionQueryFilter HasAny + default-flag pin
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "HasAny expanded to include ViaIds/ViaRoleIds/ExcludeRoleIds — would let unsafe broad-scan queries through Validate"
  - "HasAny shrunk to drop e.g. InstanceIds — blocks legitimate instance-only queries"
  - "Validate silently passes when no filters set (regression: ArgumentException would be skipped)"
  - "Default flags flipped — silently changes the shape of every unfiltered query"
verifiedTests: 13
touchedFiles: 1
---

# Step 21 — `ConnectionQueryFilter.HasAny` + default-flag pin

The filter object gates every connection-query scan; `HasAny` is
the gate predicate that decides whether `Validate()` accepts a
filter set as scoped enough to run. Pinning which collections
participate (and which intentionally do *not*) prevents silent
regressions that broaden or narrow the scan envelope.

## Tests

[`AccessMgmt.Tests/Queries/ConnectionQueryFilterTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Queries/ConnectionQueryFilterTest.cs)
— 13 tests:

- `HasAny` returns `false` when all collections are null or empty.
- `[Theory]` — `HasAny` returns `true` when any of `FromIds` /
  `ToIds` / `RoleIds` / `PackageIds` / `ResourceIds` has items.
- `HasAny` returns `true` for `InstanceIds` only (string
  collection).
- `HasAny` returns `false` for `ViaIds` / `ViaRoleIds` /
  `ExcludeRoleIds` only — pinning the documented exclusion.
- `Validate()` throws `ArgumentException` when `!HasAny`; passes
  silently when any filter is set.
- Default-flag pin — `OnlyUniqueResults`, `EnrichEntities`,
  `IncludePackages`, `IncludeResources`, `IncludeInstances`,
  `EnrichPackageResources`, `ExcludeDeleted`, `IncludeDelegation`,
  `IncludeKeyRole`, `IncludeSubConnections`,
  `IncludeMainUnitConnections` — all asserted at construction.

## Verification

```text
13/13 pass; ~9s (build dominated; test exec <1s).
```
