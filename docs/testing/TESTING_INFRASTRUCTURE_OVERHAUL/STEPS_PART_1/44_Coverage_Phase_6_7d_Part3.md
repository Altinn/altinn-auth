# Step 6.7d (Part 3) — Coverage: Validation Rule Classes & DbConverter

## Goal

Continue Phase **6.7d** by covering the deferred items from Parts 1–2:

| Target | Assembly | Notes |
|---|---|---|
| Internal validation rule classes | `AccessMgmt.Core` | Needed `InternalsVisibleTo` + PersistenceEF ref |
| `DbConverter.ConvertToResult<T>` | `AccessMgmt.Persistence.Core` | Testable via `DataTable`/`DataTableReader` |

---

## What Changed

### `Altinn.AccessMgmt.Core.csproj` — `InternalsVisibleTo`

Added an `AssemblyAttribute` item that exposes `internal` members to
`Altinn.AccessMgmt.Core.Tests`:

```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
    <_Parameter1>Altinn.AccessMgmt.Core.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```

### `Altinn.AccessMgmt.Core.Tests.csproj` — PersistenceEF reference

Added a direct `<ProjectReference>` to `Altinn.AccessMgmt.PersistenceEF` so that
`Entity`, `Role`, `Delegation`, `AssignmentPackage`, `Package`, `EntityType`, and
`EntityTypeConstants` can be used in test code without relying on transitive resolution.

### New test file — `Validation/ValidationRuleClassesTest.cs` (28 tests)

Covers every internal validation-rule factory method by directly invoking the returned
`RuleExpression` delegate and asserting that its inner `ValidationRule?` is `null`
(pass) or non-null (fail):

| Class | Methods tested |
|---|---|
| `EntityValidation` | `ReadOp` (public, 5 cases), `EntityExists`, `FromExists`, `ToExists`, `FromIsNotTo` |
| `EntityTypeValidation` | `IsOfType`, `FromIsOfType`, `ToIsOfType` |
| `RoleValidation` | `RoleExists` |
| `AssignmentPackageValidation` | `HasAssignedPackages` (empty, null, non-empty) |
| `DelegationValidation` | `HasDelegationsAssigned` (empty, null, non-empty) |
| `PackageValidation` | `PackageExists` |

**Note:** `BaseAssignmentPackage.set_Id` enforces v7 UUIDs; tests use `Guid.CreateVersion7()`.

### New test file — `PersistenceCore/DbConverterTest.cs` (10 tests)

Covers `DbConverter.ConvertToResult<T>` via `DataTable`/`DataTableReader` — no database:

- Empty reader → `Data` is empty
- Single row → `Id` (Guid), `Name` (string), `Count` (int) all mapped correctly
- Multiple rows → all three rows present
- Column names are case-insensitive (converter lowercases both sides)
- `DBNull` string column → property remains `null`
- Nullable `Guid?` with `DBNull` → property is `null`
- Nullable `Guid?` with valid string → property parsed correctly
- `_rownumber` column → `Page.FirstRowOnPage` and `Page.LastRowOnPage` set
- Unknown extra columns → silently ignored
- `PreloadCache(...)` → does not throw

---

## Verification

```
Test run completed. Ran 151 test(s). 151 Passed, 0 Failed, 0 Skipped
```

(111 pre-existing + 28 validation rule class tests + 10 DbConverter tests + 2 new = 40 new)

Build: **0 errors**, 312 pre-existing StyleCop warnings (no change).

---

## Deferred Items

- `AccessMgmt.Core` **mapper classes** (`DtoMapper.*`, `DtoMapperRolePackage.*`, etc.) — require
  construction of EF model object graphs from `AccessMgmt.PersistenceEF`; feasible but left for
  a follow-up sub-step.
- `AccessMgmt.Persistence.Core` **`PostgresQueryBuilder.BuildExtendedSelectQuery`** with joins,
  and **filter paths** in `BuildBasicSelectQuery` — require relation registration; deferred.
- `ResourceValidation.PackageIsAssignableToRecipient` / `PackageUrnLookup` and
  `PackageValidation.AuthorizePackageAssignment` — require `AccessPackageDto` / `ResourceDto`
  contract types; deferred to a dedicated sub-step.
- Remaining assemblies (`AccessMgmt.Persistence` 32.51%, `AccessManagement.Persistence`
  44.94%, `AccessManagement.Api.Internal` 46.74%, `AccessManagement.Integration` 47.57%)
  — planned for 6.7d Part 4+.

---

## Commit

`3f5b1038` — `test(6.7d-p3): add 40 pure-logic unit tests for validation rule classes and DbConverter`
