---
step: 14
title: TranslationExtensions unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "TranslateAsync(TDto): null/default short-circuit drops calls — must NOT invoke ITranslationService.TranslateAsync on null inputs"
  - "TranslateAsync(IEnumerable<TDto>): null short-circuit drops calls — must NOT invoke TranslateCollectionAsync on null"
  - "TranslateAsync(IEnumerable<TDto>): empty collection is NOT short-circuited — service still invoked (so language-code validation isn't bypassed)"
  - "TranslateAsync(IEnumerable<TDto>): collection path delegates to TranslateCollectionAsync, not the singular TranslateAsync"
  - "TranslateAsync(Task<TDto>): awaits the task before checking for default — null result of an awaited Task<T> is treated like a sync null"
  - "MapAndTranslateAsync(TSource): null source short-circuits BEFORE mapper invocation — mapper not called on null"
  - "MapAndTranslateAsync(IEnumerable<TSource>): null source returns Enumerable.Empty (not null) — caller can iterate without null check"
  - "MapAndTranslateAsync(IEnumerable<TSource>): mapper is invoked once per element, then translation is delegated to TranslateCollectionAsync"
  - "languageCode and allowPartial flags are propagated to ITranslationService unchanged"
verifiedTests: 11
touchedFiles: 1
---

# Step 14 — `TranslationExtensions` unit tests

## Goal

Pin null/default short-circuit behavior and service-delegation
contract on the five overloads in
[`Altinn.AccessMgmt.Core.Extensions.TranslationExtensions`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Extensions/TranslationExtensions.cs).
The class wraps `ITranslationService` with null-handling and
mapper-fusion overloads; regressions in the short-circuit logic
would either NRE on null inputs or fire spurious translation
service calls that would themselves NRE.

`DeepTranslationExtensions` is exercised end-to-end via
`PackagesControllerIntegrationTests` (step 7) but the per-overload
short-circuit branches there aren't directly reachable from
integration tests. This step covers the simpler
`TranslationExtensions`; the deep variant could be a follow-up
step under the same Task.

## What changed

### Tests added

[`Extensions/TranslationExtensionsTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Extensions/TranslationExtensionsTest.cs)
— 11 tests covering the 5 method overloads:

- `TranslateAsync(TDto, ...)` — null short-circuit + non-null delegation
- `TranslateAsync(IEnumerable<TDto>, ...)` — null short-circuit + empty pass-through + populated path
- `TranslateAsync(Task<TDto>, ...)` — task-awaited null short-circuit + non-null delegation
- `MapAndTranslateAsync(TSource, ...)` — null source skips mapper + non-null invokes mapper then translates
- `MapAndTranslateAsync(IEnumerable<TSource>, ...)` — null source returns Empty + populated path maps each then translates collection

Test harness: `RecordingTranslationService` is an in-test stub
implementing `ITranslationService` that counts calls and records
the language code / `allowPartial` flag passed in. No real
translation happens; the stub returns the input unchanged so we
can assert pure flow behavior.

## Verification

```text
$ dotnet test ...Altinn.AccessMgmt.Core.Tests... --filter-namespace "Altinn.AccessMgmt.Core.Tests.Extensions"
Passed! - Failed: 0, Passed: 11, Skipped: 0, Total: 11, Duration: 2s
```

## Deferred / follow-up

- **`DeepTranslationExtensions`** (~16 method overloads recursing
  through `PackageDto` → `AreaDto` → `AreaGroupDto` →
  `ResourceDto` → `ProviderDto` → `TypeDto` → `RoleDto`). The
  deep traversal has an asymmetric recursion pattern — some
  nested `Type` translations call the deep variant, others call
  the shallow `TranslateAsync` directly. That's a real bug class
  to pin (a future regression where the asymmetry flips would be
  hard to spot in code review). Worth a follow-up step under
  this Task.
- **`AccessMgmt.Core.Extensions.ControllerExtensions`,
  `ServiceProviderExtensions`** — not yet audited under the
  real-logic-vs-pass-through filter; both could yield small
  test additions.
