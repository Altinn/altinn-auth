# Step 54 — Coverage: `IdentifierUtil`, `HashUtil`, `DtoMapperEntityVariant` (Phase 6.7d continued)

## Goal

Add pure-logic unit tests (no containers, no DI) for three previously untested
utility/mapper classes spread across two assemblies:

| Class | Assembly | Methods |
|---|---|---|
| `IdentifierUtil` | `Altinn.AccessManagement` | `IsValidOrganizationNumber`, `MaskSSN`, `GetIdentifierAsAttributeMatch` |
| `HashUtil` | `Altinn.AccessManagement` | `GetOrderIndependentHashCode<T>` |
| `DtoMapperEntityVariant` | `Altinn.AccessMgmt.Core` | `Convert(EntityVariant)`, `Convert(EntityType)` |

---

## What Changed

### New test files

#### `AccessMgmt.Tests/Utilities/IdentifierUtilTest.cs` — 13 tests

Tests all three methods of `IdentifierUtil`:

| Method | Cases |
|---|---|
| `IsValidOrganizationNumber` | Known-valid (974760673) → true; wrong check digit → false; too short (8 digits) → false; too long (10 digits) → false; contains letter → false; empty string → false |
| `MaskSSN` | 11-digit SSN → first 6 chars + "*****" |
| `GetIdentifierAsAttributeMatch` | "organization" + valid org number → `OrganizationNumberAttribute`; "organization" + missing header → throws; "organization" + invalid org number → throws; "person" + valid SSN → `PersonId`; "person" + missing header → throws; "person" + non-numeric SSN → throws; numeric party id → `PartyAttribute`; zero party id → throws; non-numeric party → throws |

**Fix note:** The initial test for "invalid org number" used `"000000000"` — this
is paradoxically valid per the modulo-11 algorithm (all-zero digit sum → ctrlDigit
wraps from 11 to 0, which equals the last digit). Corrected to `"123456789"` which
genuinely fails (expected check digit 5, actual last digit 9). Similarly, the
initial "invalid SSN" test used `"00000000000"` which `PersonIdentifier.TryParse`
accepts as a format-valid 11-digit string; corrected to the non-numeric
`"not-valid-ssn"`.

#### `AccessMgmt.Tests/Utilities/HashUtilTest.cs` — 5 tests

Tests `HashUtil.GetOrderIndependentHashCode<T>`:

| Case | Assertion |
|---|---|
| Empty collection | Returns 0 |
| [1, 2, 3] vs [3, 1, 2] | Same hash (order independence) |
| Single element 42 | Equals `EqualityComparer<int>.Default.GetHashCode(42)` |
| Different single elements | Different hashes |
| String list order independence | ["a","b"] == ["b","a"] |

#### `Altinn.AccessMgmt.Core.Tests/Utils/Mappers/DtoMapperEntityVariantTest.cs` — 3 tests

Tests `DtoMapperEntityVariant` — a standalone mapper class (not a partial of
`DtoMapper`) that was not included in the Step 45 `DtoMapperTest` sweep:

| Method | Cases |
|---|---|
| `Convert(EntityVariant)` — explicit Type | All fields map; `Type` taken from `obj.Type` |
| `Convert(EntityVariant)` — null Type | Falls back to `EntityTypeConstants.TryGetById(TypeId)` → Organization constant |
| `Convert(EntityType)` | `Id`, `Name`, `ProviderId` all mapped |

---

## Verification

All 24 new tests pass (0 failed, 0 skipped):

```
Test run completed. Ran 24 test(s). 24 Passed, 0 Failed
```

No regressions in either the `AccessMgmt.Tests` or `Altinn.AccessMgmt.Core.Tests` assemblies.

---

## Coverage Impact

| Assembly | Before | After (est.) |
|---|---|---|
| `AccessManagement` (main app) | 58.19% | ↑ (IdentifierUtil + HashUtil covered) |
| `Altinn.AccessMgmt.Core` | 17.31%→↑ | ↑ (DtoMapperEntityVariant covered) |

The `AccessManagement` main assembly remains warn-only (threshold: 60%) but the
`IdentifierUtil` and `HashUtil` additions contribute to closing that gap.

---

## Deferred

- `IdentifierUtil.GetIdentifierAsAttributeMatch` with org-header path uses
  `context.Request.Headers[OrganizationNumberHeader]` which returns a
  `StringValues`; the `IsValidOrganizationNumber` call with a `StringValues`
  argument works because implicit conversion to `string` is applied — no further
  test cases needed.
- `DtoMapperEntityVariant.Convert(EntityVariant)` with an unknown `TypeId` (not
  in `EntityTypeConstants`) would `NullReferenceException` on `type.Entity` —
  this is a pre-existing production bug outside scope.
- `DtoMapperConnectionQuery.ConvertToOthers` / `ConvertFromOthers` remain
  deferred (complex `Connection` graph setup per Step 53 notes).
