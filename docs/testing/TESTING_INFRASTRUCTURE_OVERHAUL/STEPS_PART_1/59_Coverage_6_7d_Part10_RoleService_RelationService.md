# Step 59 — Coverage 6.7d Part 10: `RoleService` + `RelationService` Moq Unit Tests

## Goal

Add pure Moq unit tests (no container, no DB) for `RoleService` and `RelationService` in
`Altinn.AccessMgmt.Persistence` to raise its 32.51% line coverage baseline.

Both services are fully constructor-injectable — all repository dependencies are interfaces —
making them ideal candidates for fast, deterministic unit tests with Moq.

---

## Changes

### New test files

| File | Tests | Coverage targets |
|------|-------|-----------------|
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Services/RoleServiceTest.cs` | 18 | `RoleService` — all 7 methods |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Services/RelationServiceTest.cs` | 21 | `RelationService` — all 8 methods |

### `RoleServiceTest` (18 tests)

Covers all branches of all 7 `IRoleService` methods:

| Method | Tests | Branches |
|--------|-------|---------|
| `GetById` | 3 | not-found → null; found, no legacy code; found, with legacy code |
| `GetAll` | 3 | null result → null; with roles, no lookup; with lookup → sets legacy fields |
| `GetByProvider` | 2 | null result → null; with roles → mapped DTOs |
| `GetByCode` | 2 | null result → null; with roles → matching code |
| `GetByKeyValue` | 2 | empty lookup → null; found lookup → role DTO |
| `GetLookupKeys` | 2 | null result → null; duplicate keys → distinct |
| `GetPackagesForRole` | 3 | empty + role exists → empty list; empty + role missing → null; with packages → DTO with Package |

**Key implementation notes:**
- `GenericFilterBuilder<T>` implements `IEnumerable<GenericFilter>` — filter-based `GetExtended`
  calls are mocked via `It.IsAny<IEnumerable<GenericFilter>>()`.
- `ExtRolePackage.Package` must be set; `RolePackageDto(ExtRolePackage)` calls
  `new PackageDto(rolePackage.Package)` and will NRE if `Package` is null.
- `RoleLookup`/`RolePackage` constructors auto-set `Id = Guid.CreateVersion7()` — object
  initializers are safe.

### `RelationServiceTest` (21 tests)

Covers all branches of all 8 `IRelationService` methods:

| Method | Tests | Branches |
|--------|-------|---------|
| `GetConnectionsToOthers` (full) | 4 | empty; Direct relation → grouped; non-Direct → excluded; two relations same `To` → distinct roles |
| `GetConnectionsFromOthers` (full) | 2 | empty; with relation → grouped by `From` |
| `GetConnectionsToOthers` (compact) | 3 | empty; Direct → grouped; non-Direct → excluded |
| `GetConnectionsFromOthers` (compact) | 2 | empty; with relation → grouped by `From` |
| `GetPackagePermissionsFromOthers` | 3 | empty → empty; null package → empty; with package → grouped by package ID |
| `GetPackagePermissionsToOthers` | 2 | empty; with package → grouped |
| `GetResourcePermissionsFromOthers` | 3 | empty; null package or resource → excluded; both set → grouped by resource ID |
| `GetResourcePermissionsToOthers` | 2 | empty; both set → grouped |
| `GetAssignablePackagePermissions` | 1 | delegates to repository |

**Key implementation notes:**
- `RelationService` has two overloads for each of `GetConnectionsToOthers`/`GetConnectionsFromOthers`:
  a 6-param full version (`IRelationPermissionRepository`) and a 4-param compact version
  (`IRelationRepository`). C# cannot always disambiguate calls with all-optional trailing params.
  - Full overload: use `packageId: null` named arg (compact overload has no `packageId` param).
  - Compact overload: pass `CancellationToken.None` as 4th positional arg (full's 4th param is
    `Guid?`, not `CancellationToken`, so it unambiguously selects the compact overload).
  - The IDE (Roslyn incremental analysis) may report false-positive CS0121 errors — `dotnet build`
    confirms these are not real errors.

---

## Verification

```
dotnet test --filter "FullyQualifiedName~RoleServiceTest|FullyQualifiedName~RelationServiceTest"
```

**Result: 39 / 39 passed, 0 failed, 0 skipped** (2.8 s)

---

## Coverage impact

Both services are new to test coverage in `AccessMgmt.Persistence`.
The 39 tests exercise all method bodies and branching logic in `RoleService` (≈130 LOC) and
`RelationService` (≈230 LOC), contributing meaningfully toward raising the assembly baseline
above the 32.51% measured in Step 56.

A fresh `dotnet-coverage` run is required to get the updated figure (deferred to next coverage
measurement step).

---

## Deferred

- Live-DB path of `IRoleRepository`/`IRelationRepository` (Npgsql) — still requires a container.
  These are the bulk of the remaining gap in `AccessMgmt.Persistence`.
- `DtoMapper.ConvertResources(IEnumerable<AssignmentResource>)` — low marginal value, previously
  deferred.
