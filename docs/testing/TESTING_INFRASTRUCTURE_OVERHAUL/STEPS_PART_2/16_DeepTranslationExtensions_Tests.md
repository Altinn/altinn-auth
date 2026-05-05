---
step: 16
title: DeepTranslationExtensions recursion-topology unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Null short-circuit drops on each public entry point — caller would NRE through translation service if a regression removed the null guard"
  - "PackageDto recursion topology — Area/Type/Resources are all recursed into via deep variant"
  - "AreaDto cycle avoidance — when iterating Area.Packages, the package's nested Area is NOT recursed back into (would loop indefinitely)"
  - "AreaGroupDto.Areas materialization — .ToList() on the translated collection avoids deferred-execution surprises"
  - "Asymmetric Provider→Type vs Type→Provider — Provider.TranslateDeepAsync uses TranslateAsync (shallow) for its Type; TypeDto.TranslateDeepAsync uses TranslateDeepAsync (deep) for its Provider. Regression where one flips would silently miss translations or cause infinite recursion."
  - "ResourceDto.Type is ResourceTypeDto (a leaf type) — translated via singular TranslateAsync only"
  - "RoleDto recurses deep into Provider; collection variants iterate and translate each item"
verifiedTests: 20
touchedFiles: 1
---

# Step 16 — `DeepTranslationExtensions` recursion-topology unit tests

## Goal

Pin the recursion topology of
[`DeepTranslationExtensions`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Extensions/DeepTranslationExtensions.cs)
across the nested DTO graph (`PackageDto` → `AreaDto` →
`AreaGroupDto`, `ResourceDto` → `ProviderDto` → `TypeDto`,
`RoleDto` → `ProviderDto`). Most importantly: pin the **asymmetric
deep-vs-shallow** pattern where `ProviderDto.TranslateDeepAsync`
uses the singular `TranslateAsync` for its nested `Type` (shallow),
but `TypeDto.TranslateDeepAsync` uses the deep variant for its
nested `Provider` — a regression that flipped this would either
miss translations or cause infinite recursion.

The deep extensions are exercised end-to-end by
`PackagesControllerIntegrationTests` (step 7), but per-overload
null guards and the recursion-topology bug classes aren't
directly observable from those integration tests.

## What changed

### Tests added

[`Extensions/DeepTranslationExtensionsTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Extensions/DeepTranslationExtensionsTest.cs)
— 20 tests organized into:

- **Null short-circuits** (8 `[Fact]`s, one per public entry):
  `PackageDto`, `AreaDto`, `AreaGroupDto`, `ResourceDto`,
  `ProviderDto`, `TypeDto`, `RoleDto`, plus the `IEnumerable<>`
  collection overload as a smoke test.
- **`PackageDto` topology**: `Area`, `Type`, `Resources` all
  recursed into; counts asserted per DTO type to catch off-by-one
  bugs.
- **`AreaDto` cycle avoidance**: when iterating `Area.Packages`,
  the package's nested `Area` is *not* recursed back into.
- **`AreaGroupDto.Areas`** materialization.
- **Asymmetric Provider/Type recursion**:
  - `Provider.TranslateDeepAsync(p)` → `p` deep, `p.Type` shallow
    (Type's nested Provider NOT translated through this path).
  - `Type.TranslateDeepAsync(t)` → `t` deep, `t.Provider` deep.
  - `Resource.TranslateDeepAsync(r)` → `r` deep, `r.Provider` deep,
    `r.Provider.Type` shallow, `r.Type` shallow (different from
    `Provider.Type` because `ResourceDto.Type` is `ResourceTypeDto`,
    a leaf type with no deep variant).
- **`RoleDto` deep recursion** into Provider.
- **Collection variants** (Package, Area, Role) iterating and
  translating each item.

Test harness: `CountingTranslationService` is an in-test
`ITranslationService` stub that increments a per-`typeof(T)`
counter on every `TranslateAsync<T>` invocation. Tests assert
exact call counts per DTO type to pin the topology.

## Verification

```text
$ dotnet test ...Altinn.AccessMgmt.Core.Tests...
Passed! - Failed: 0, Passed: 423, Skipped: 0, Total: 423, Duration: 33s
```

423 = 396 prior + 27 new (20 here are `[Fact]`; some prior
`[Theory]` bumps from step 15 weren't fully reflected in earlier
counts).

## Deferred / follow-up

- `DeepTranslationExtensions` collection overloads for
  `ResourceDto`, `ProviderDto` (no deep collection method exists),
  `TypeDto` (no collection method exists) — non-issue, just
  noting completeness.
- `AccessMgmt.Core` outbox notification handlers — DB-heavy, not
  pure-unit territory.
- AccessManagement.Core (the older sibling namespace) helpers —
  next step.
