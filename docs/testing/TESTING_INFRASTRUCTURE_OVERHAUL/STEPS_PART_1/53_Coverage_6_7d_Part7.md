# Step 53 — Coverage 6.7d Part 7 — `ResourceValidation`, `DelegationCheckDtoMapper`, `QueryWrapper`, `DelegationCheckHelper`, `SearchCache`, `DbDefinitionBuilder` (Phase 6.7d continued)

## Goal

Continue Phase **6.7d** with 54 new pure-logic unit tests across six previously untested
classes in `Altinn.AccessMgmt.Core` and `Altinn.AccessMgmt.Persistence.Core`.
All tests run without any container runtime (no Postgres, no Docker).

---

## What Changed

All tests were added to the existing `Altinn.AccessMgmt.Core.Tests` project.

### 1. `Validation/ResourceValidationTest.cs` — 18 tests

Tests all 8 internal factory methods in `ResourceValidation` using the `Fails`/`Passes`
helper pattern established in Step 44's `ValidationRuleClassesTest`.

| Method | Cases |
|---|---|
| `ResourceExists` | null resource → fail; non-null → pass |
| `AuthorizeResourceAssignment` | all Result=true → pass; any false → fail |
| `PackageIsAssignableToRecipient` | org + MainAdmin URN → fail; org + other URN → pass; person + MainAdmin → pass |
| `PackageUrnLookup` | empty result → fail; count mismatch → fail; exact match → pass |
| `HasAssignedResources` | empty list → pass; non-empty → fail |
| `ResourceTypeIs` | matching name → pass; wrong name → fail; null resource → fail |
| `PolicyClearFailed` | non-null version → pass; null → fail |
| `PolicyCascadeClearFailed` | non-null version → pass; null → fail |

### 2. `Utils/DelegationCheckDtoMapperTest.cs` — 4 tests

Tests `DtoMapper.Convert(IEnumerable<PackageDelegationCheck>)` — the pure data
transformation that groups `PackageDelegationCheck` rows by package ID.

| Case | Assertion |
|---|---|
| Single row → single DTO | Package fields + Reasons mapped correctly |
| Multiple rows, same package, all false | Grouped into one DTO; `Result = false` |
| Multiple rows, same package, one true | Grouped; `Result = true` (any-true semantics) |
| Two distinct packages | Returns two DTOs; fields per-package correct |

### 3. `Utils/QueryWrapperTest.cs` — 3 tests

Tests internal `QueryWrapper.WrapQueryResponse<T>` (accessible via `InternalsVisibleTo`
already set to `Altinn.AccessMgmt.Core.Tests` in the csproj).

| Case | Assertion |
|---|---|
| Non-empty collection | `Page.TotalSize`, `PageSize`, `LastRowOnPage` all equal count |
| Empty collection | All counts zero |
| Single item | PageInfo reflects single item |

### 4. `Utils/DelegationCheckHelperTest.cs` — 5 tests

Tests `DelegationCheckHelper.IsAccessListModeEnabledAndApplicable`.

| Case | Assertion |
|---|---|
| `Enabled` + Organization GUID | `true` |
| `Disabled` + Organization GUID | `false` |
| `Enabled` + Person GUID | `false` |
| `Disabled` + Person GUID | `false` |
| `Enabled` + `Guid.Empty` | `false` |

### 5. `PersistenceCore/SearchCacheTest.cs` — 6 tests

Tests `SearchCache<T>` (in `Altinn.AccessMgmt.Persistence.Core`) using a real
`MemoryCache` (no mocking needed).

| Case | Assertion |
|---|---|
| `GetData` before `SetData` | returns `null` |
| After `SetData`, `GetData` | returns stored items |
| Two `GetData` calls | return different list instances (defensive copy) |
| `int` list round-trip | values preserved exactly |
| Empty list | returns non-null empty list |
| `SetData` twice | latest data wins |

### 6. `PersistenceCore/DbDefinitionBuilderTest.cs` — 18 tests

Tests `DbDefinitionBuilder<T>` fluent builder. Uses a local `SampleModel` POCO
(`Id`, `Name`, `Count`) — no DB connection or registry needed.

| Method | Cases |
|---|---|
| `Build()` | `ModelType` = `typeof(T)`; default type = Table; default version = 1 |
| `SetVersion` | custom version stored |
| `SetType` | `DbDefinitionType.View` stored |
| `SetQuery` | query stored; extended query stored alongside |
| `EnableTranslation` | off by default; `true`/`false` variants |
| `EnableAudit` | enabled flag stored |
| `RegisterProperty` | 1 property; 3 properties; nullable flag; non-nullable flag |
| `AddManualDependency` | type added to `ManualDependencies` |
| Fluent chaining | all operations compose correctly |

---

## Verification

```
Test run completed. Ran 262 test(s). 262 Passed, 0 Failed
```

All 262 tests in `Altinn.AccessMgmt.Core.Tests` pass (54 new tests added in this step).
Build: ✅ successful.

---

## Deferred Items

- `DelegationCheckHelper.GetFirstAccessorValuesFromPolicy` / `DecomposePolicy` / `BuildDelegationRuleTarget` —
  require building `XacmlPolicy`/`XacmlRule` object graphs; deferred to a dedicated XACML unit-test step.
- `TranslationExtensions` — thin wrapper over `ITranslationService`; minimal independent logic.
- `DeepTranslationExtensions` — already tested via DB-backed tests in `AccessMgmt.Tests`.
- `AccessManagement.Persistence` (44.94%) and `AccessMgmt.Persistence` (32.51%) — dominated by
  Npgsql repository code; require a live DB.
