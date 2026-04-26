# Step 6.7d (Part 1) — Coverage: AccessMgmt.Persistence.Core, AccessMgmt.Core FuzzySearch, and Api.Metadata Controllers

## Goal

Phase 6 priority item **6.7d** targets the persistence/core assemblies with the lowest coverage.
This first pass focused on the three assemblies that have **pure-logic classes testable without any
container runtime**:

| Assembly | Coverage before | Target |
|---|---|---|
| `AccessMgmt.Persistence.Core` | 8.78% | ↑ significant gain |
| `AccessMgmt.Core` | 17.31% | ↑ FuzzySearch / SearchPropertyBuilder |
| `AccessManagement.Api.Metadata` | 16.59% | ↑ controller unit tests |

---

## What Changed

### 1. `Altinn.AccessMgmt.Core.Tests.csproj`

Added a `ProjectReference` to `Altinn.AccessMgmt.Persistence.Core` so the test project can cover
that assembly without a dedicated test project.

### 2. New test file — `PersistenceCore/PersistenceCoreFuzzySearchTest.cs` (15 tests)

Covers `Altinn.AccessMgmt.Persistence.Core.Utilities.Search`:

- `FuzzySearch.PerformFuzzySearch` — empty/null term, exact match, no match, fuzzy typo (High
  fuzziness), empty data list, two-property builder, collection combined/detailed modes
- `SearchPropertyBuilder<T>` — `Add`, `AddCollection` (combined + detailed), `Build`, weight/fuzziness
  stored correctly, selector callable

### 3. New test file — `PersistenceCore/GenericFilterBuilderTest.cs` (16 tests)

Covers `Altinn.AccessMgmt.Persistence.Core.Helpers`:

- `GenericFilterBuilder<T>` — `Empty` (empty and non-empty), `Add` (value, comparer, int property,
  fluent return), `Equal`, `NotSet` (null value), `In` (multiple values, null throws, empty throws),
  `NotIn` (values + null guard), `IEnumerable` (generic + non-generic)

### 4. New test file — `Utils/FuzzySearchTest.cs` (19 tests)

Covers `Altinn.AccessMgmt.Core.Utils.FuzzySearch` and the embedded `SearchPropertyBuilder<T>`
(same file, different assembly):

- `PerformFuzzySearch` — all same branches as Persistence.Core variant plus multi-input check,
  two-property scoring, collection combined/detailed
- `SearchPropertyBuilder<T>` — all property/collection builder paths, weight/fuzziness storage,
  selector invocation

### 5. New test file — `Controllers/Metadata/PackagesControllerTest.cs` (12 tests)

Covers `Altinn.AccessManagement.Api.Metadata.Controllers.PackagesController` via direct
instantiation with mocked `IPackageService` / `ITranslationService`:

- `Search` — results → 200 OK, no results → 204, null result → 204, invalid typeName → Problem,
  valid typeName passed through to service
- `GetHierarchy` — results → 200, empty → 204
- `GetGroups` — results → 200, empty → 204
- `GetGroup` — found → 200, not found → 404

### 6. New test file — `Controllers/Metadata/TypesControllerTest.cs` (3 tests)

Covers `Altinn.AccessManagement.Api.Metadata.Controllers.TypesController.GetOrganizationSubTypes`
(the only `[HttpGet]`-decorated public method; the other two are `private`):

- Returns non-null value
- Returns non-empty list
- All items have non-empty `Name`

> **Note:** `GetOrganizationSubTypes` returns `List<SubTypeDto>` directly (not via `Ok()`), so
> `ActionResult<T>.Value` holds the payload while `.Result` is null. Tests use `.Value` accordingly.

---

## Verification

All 65 new tests pass with zero failures:

```
Test run completed. Ran 65 test(s). 65 Passed, 0 Failed, 0 Skipped
```

Build remains successful.

---

## Deferred Items

- `AccessMgmt.Persistence.Core` — `PostgresQueryBuilder` / `MssqlQueryBuilder` SQL generation
  and `DbConverter` IDataReader conversion require more setup; left for a future sub-step.
- `AccessMgmt.Core` services (RequestService, ConnectionService, etc.) require either a real
  Postgres DB or extensive mocking chains; left for step 6.7d Part 2.
- `AccessManagement.Api.Metadata` — `RolesController` endpoints not yet unit-tested here (existing
  `MetadataTests.cs` covers them via `PostgresFixture`).

---

## Commit

`18e0999b` — `test(coverage): step 6.7d - add unit tests for AccessMgmt.Persistence.Core, AccessMgmt.Core FuzzySearch, and Api.Metadata controllers`
