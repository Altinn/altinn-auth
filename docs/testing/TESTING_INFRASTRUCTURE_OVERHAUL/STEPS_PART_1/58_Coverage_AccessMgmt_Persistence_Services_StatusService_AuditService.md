# Step 58 — Coverage: `AccessMgmt.Persistence` `StatusService` + `AuditService` middleware

## Goal

Add pure Moq unit tests (no container required) for two previously-uncovered
service classes in `Altinn.AccessMgmt.Persistence`:

- **`StatusService`** — all four public methods with every branch.
- **`AuditService`** — the single `IMiddleware.InvokeAsync` method with all five branches.

## What Changed

### Production

| File | Change |
|---|---|
| `src/…/Altinn.AccessMgmt.Persistence/Altinn.AccessMgmt.Persistence.csproj` | Added two `InternalsVisibleTo` attributes: `AccessMgmt.Tests` (so the test project can see `internal` types) and `DynamicProxyGenAssembly2` (so Moq's Castle proxy can implement the `internal` `IDbAuditService` interface). |

### Tests (new files)

| File | Tests | Covers |
|---|---|---|
| `test/AccessMgmt.Tests/Services/StatusServiceTest.cs` | 14 | `StatusService.GetOrCreateRecord` (3), `RunFailed` (2), `RunSuccess` (3), `TryToRun` (6) — all branches |
| `test/AccessMgmt.Tests/Services/AuditServiceTest.cs` | 5 | `AuditService.InvokeAsync` — no endpoint, endpoint w/o attribute, attribute w/ missing claim, attribute w/ non-GUID claim, attribute w/ valid GUID claim → `IDbAuditService.Set` called |

**Total new tests: 19**

## Coverage impact

Both classes are in `Altinn.AccessMgmt.Persistence` (baseline 32.51% line).
The 19 tests cover all logic in both classes (both were previously 0%).

## Verification

```
Test run completed. Ran 19 test(s). 19 Passed, 0 Failed
```

## Deferred

- `RoleService`, `RelationService`, `ConnectionService`, `DelegationService`,
  `AssignmentService`, `PackageService` — contain DB-backed repository calls
  mixed with logic; the filter-building branches could be Moq-tested in a future
  step but the payoff per-test is lower than the two classes addressed here.
- `AccessManagement.Persistence` (`PolicyRepository`, `ConsentRepository`,
  `DelegationMetadataRepository`, `ResourceMetadataRepository`) — all Npgsql /
  Azure Blob; no unit-testable logic surface beyond what's `[ExcludeFromCodeCoverage]`.
