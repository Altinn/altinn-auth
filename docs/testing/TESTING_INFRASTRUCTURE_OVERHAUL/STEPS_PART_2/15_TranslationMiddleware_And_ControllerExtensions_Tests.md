---
step: 15
title: TranslationMiddleware and ControllerExtensions unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Accept-Language q-value ordering broken — high-q language not selected over low-q"
  - "Accept-Language region suffix not stripped (en-US not normalized to eng)"
  - "Unsupported language code → empty/null/throw instead of falling back to DefaultLanguageCode"
  - "Unsupported high-q skipped wrongly — supported low-q should still win when high-q is unrecognized"
  - "X-Accept-Partial-Translation header parsing wrong values (regression: case-sensitive, missing yes/no aliases, default flips to false on missing)"
  - "Content-Language response header not set after middleware runs (regression: clients can't read which language was used)"
  - "ControllerExtensions.GetLanguageCode silently bypasses HttpContext.Items when value is empty string instead of falling back"
  - "ControllerExtensions.GetLanguageCode header fallback ignores quality values and takes the wrong language when q-ordering matters (intentional — middleware does the q-ordering; ControllerExtensions is the no-middleware fallback)"
  - "AllowPartialTranslation default flips from true to false (security/UX regression)"
  - "AllowPartialTranslation NREs or returns wrong value when HttpContext.Items has the key with a non-bool value"
verifiedTests: 33
touchedFiles: 2
---

# Step 15 — `TranslationMiddleware` + `ControllerExtensions` unit tests

## Goal

Pin the language-code resolution behavior across the two
production paths:

1. [`TranslationMiddleware`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Utils/TranslationMiddleware.cs)
   — the *primary* normalization point for HTTP requests; sets
   `HttpContext.Items[LanguageCodeKey]` and the
   `X-Accept-Partial-Translation` flag for downstream code.
2. [`ControllerExtensions.GetLanguageCode` / `AllowPartialTranslation`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Extensions/ControllerExtensions.cs)
   — controller-side helpers that read `HttpContext.Items` and
   fall back to a *second* Accept-Language parser when the
   middleware hasn't run.

The two paths share a normalization mapping but use different
parsers (the middleware honors q-values; `ControllerExtensions`
just takes the first comma-separated entry). That asymmetry is
intentional but easy to break in a way that only manifests at
runtime.

## What changed

### Tests added

[`Utils/TranslationMiddlewareTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Utils/TranslationMiddlewareTest.cs)
— 19 tests across 4 sections:

- Accept-Language single language → ISO 639-2 (`[Theory]` over en/nb/nn/no/eng/nob/nno)
- Region suffix stripping (`en-US` → `eng`) — `[Theory]`
- Multiple languages with q-values → highest q wins
- Multiple languages without q-values → first wins (stable sort preserves order)
- Unsupported language fallback (`fr` → `nob`)
- Unsupported high-q → supported low-q still wins
- Empty / missing Accept-Language → default
- `X-Accept-Partial-Translation` parsing — `[Theory]` over the
  recognized aliases (`true`/`1`/`yes` ↔ `false`/`0`/`no`,
  case-insensitive, garbage → default true)
- Content-Language response header set after `_next`

[`Extensions/ControllerExtensionsTest.cs`](../../../src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Extensions/ControllerExtensionsTest.cs)
— 14 tests:

- `GetLanguageCode` precedence — `HttpContext.Items` set / empty / missing
- Header-fallback normalization — `[Theory]` over the same
  language code matrix as the middleware
- Multi-language header — first comma entry wins (q-values
  ignored on this path; pinning the documented asymmetry)
- Region suffix stripping
- `AllowPartialTranslation` — true / false / missing / wrong-type
- `SetContentLanguage` writes the response header

Test harness: `DefaultHttpContext` for the middleware (real
HttpContext, no mocking); a small `TestController : ControllerBase`
subclass for the controller helpers (since `ControllerBase` is
abstract).

## Verification

```text
$ dotnet test ...Altinn.AccessMgmt.Core.Tests... --filter-namespace "Altinn.AccessMgmt.Core.Tests.Utils" + Extensions
Passed! - Failed: 0, Passed: 128, Skipped: 0, Total: 128, Duration: 4s
```

128 = previous `Utils` + `Extensions` namespaces (96) + 33 new
here. No regressions.

## Deferred / follow-up

- `TranslationMiddleware.ParseAcceptLanguageHeader` handles
  malformed `q=...` values silently (e.g. `en;q=invalid` → falls
  back to default 1.0). Pinning that specifically isn't covered
  here; could add if a regression appears.
- `DeepTranslationExtensions` (still deferred from step 14).
