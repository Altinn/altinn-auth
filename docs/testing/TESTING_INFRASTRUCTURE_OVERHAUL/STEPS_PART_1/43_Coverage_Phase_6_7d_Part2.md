# Step 6.7d (Part 2) — Coverage: ValidationComposer, OrgUtil, DbHelperMethods, PostgresQueryBuilder

## Goal

Continue Phase **6.7d** by adding pure-logic unit tests to two assemblies that had no container
dependencies but were still largely uncovered after Part 1:

| Assembly | Coverage before (Part 1) | Targets this step |
|---|---|---|
| `AccessMgmt.Core` | 17.31% | `ValidationComposer`, `OrgUtil` |
| `AccessMgmt.Persistence.Core` | 8.78% | `DbHelperMethods`, `PostgresQueryBuilder` |

---

## What Changed

### New test file — `Validation/ValidationComposerTest.cs` (15 tests)

Covers `Altinn.AccessMgmt.Core.Validation.ValidationComposer` via direct invocation — no
mocks or container required:

- `Validate` — no rules → null, single pass → null, single fail → problem, all pass → null,
  one fails → problem, all fail → problem
- `All` — no rules → null, all pass → null, one fail → non-null, all fail → non-null
- `Any` — all pass → null, some pass → null, all fail → non-null, no rules → non-null (edge
  case: `results.Count == funcs.Length` when both are 0)

### New test file — `Utils/OrgUtilTest.cs` (12 tests)

Covers `Altinn.AccessMgmt.Core.Utils.OrgUtil` using `ClaimsPrincipal` with inline claims:

- `GetMaskinportenScopes` — claim present, absent, multiple scopes
- `GetAuthenticatedParty` — valid consumer claim, absent, empty, wrong authority, missing
  `ID` field, invalid JSON
- `GetSupplierParty` — valid supplier claim, absent, wrong authority, ID with no colon (no org
  number separator)

### New test file — `PersistenceCore/DbHelperMethodsTest.cs` (16 tests)

Covers `Altinn.AccessMgmt.Persistence.Core.Helpers.DbHelperMethods.GetPostgresType` for
both `PropertyInfo` and `Type` overloads:

- All supported .NET→`NpgsqlDbType` mappings: `string`, `int`, `long`, `short`, `Guid`,
  `bool`, `DateTime`, `DateTimeOffset`, `float`, `double`, `decimal`
- Nullable types (`Guid?`, `int?`) — unwraps underlying type correctly
- `PropertyInfo` overload for `string` and `Guid` properties
- Unsupported type (`object`) throws `NotSupportedException`

### New test file — `PersistenceCore/PostgresQueryBuilderTest.cs` (11 tests)

Covers `Altinn.AccessMgmt.Persistence.Core.QueryBuilders.PostgresQueryBuilder` SQL
generation — no database connection needed. Sets up `DbDefinitionRegistry` with a local
`SimpleModel` record and `AccessMgmtPersistenceOptions` via `Options.Create`:

- `GetTableName` — without alias contains schema + model name; with alias contains `AS` clause
- `BuildInsertQuery` — contains `INSERT INTO`, all `@param` names, table name
- `BuildUpdateQuery` — contains `UPDATE`, `WHERE id = @_id`, `SET Name = @Name`
- `BuildSingleNullUpdateQuery` — contains `Name = NULL`, `WHERE id = @_id`
- `BuildBasicSelectQuery` — contains `SELECT` and `FROM`

---

## Verification

All tests in the affected project pass with zero failures:

```
Test run completed. Ran 111 test(s). 111 Passed, 0 Failed, 0 Skipped
```

(65 pre-existing + 46 new)

Build successful.

---

## Deferred Items

- `AccessMgmt.Core` **validation rule classes** (`EntityTypeValidation`, `RoleValidation`,
  `ResourceValidation`, `AssignmentPackageValidation`) — all `internal`. Require
  `InternalsVisibleTo` in source project; deferred to a follow-up sub-step.
- `AccessMgmt.Core` **mapper classes** (`DtoMapper.*`) — require construction of EF model
  objects from `AccessMgmt.PersistenceEF`; feasible but left for a dedicated sub-step.
- `AccessMgmt.Persistence.Core` **`DbConverter.ConvertToResult<T>`** — testable via
  `DataTable`/`DataTableReader`; left for a follow-up sub-step.
- `AccessMgmt.Persistence.Core` **`PostgresQueryBuilder.BuildBasicSelectQuery`** with
  filters and `BuildExtendedSelectQuery` with joins — require relation registration; deferred.
- Remaining assemblies (`AccessMgmt.Persistence` 32.51%, `AccessManagement.Persistence`
  44.94%, `AccessManagement.Api.Internal` 46.74%, `AccessManagement.Integration` 47.57%)
  still at low coverage; planned for 6.7d Part 3+.

---

## Commit

`4ed65018` — `test(6.7d-p2): add 46 pure-logic unit tests for ValidationComposer, OrgUtil, DbHelperMethods, PostgresQueryBuilder`
