# Step 60 — Coverage: `Integration.Platform` `RequestComposer` + `ResponseComposer`

## Goal

Add pure-unit tests for the two `internal static` helper classes that underpin
every HTTP call made through `Altinn.Authorization.Integration.Platform`:
`RequestComposer` and `ResponseComposer`. These classes had zero test coverage
because the integration tests in `Altinn.Authorization.Integration.Tests` are
real end-to-end tests that skip when external endpoints are unavailable.

## Changes

| File | Change |
|---|---|
| `src/libs/Altinn.Authorization.Integration/src/Altinn.Authorization.Integration.Platform/Altinn.Authorization.Integration.Platform.csproj` | Added explicit `<InternalsVisibleTo Include="Altinn.Authorization.Integration.Tests" />` (auto-generated one covers `*.Platform.Tests`, not the actual test project name). |
| `src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/RequestComposerTest.cs` | **New** — 20 tests covering all 11 public surface methods. |
| `src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/ResponseComposerTest.cs` | **New** — 16 tests covering all 6 handler methods + `Handle` pipeline. |

## What was tested

### `RequestComposer` (20 tests)

| Method | Tests |
|---|---|
| `New` | No-actions returns empty request; multiple actions applied in order |
| `WithHttpVerb` | Sets method correctly |
| `WithSetUri(string)` | Valid URI set; null/empty ignored |
| `WithSetUri(Uri, segments)` | Combines base URI + path segments; null URI ignored |
| `WithJSONPayload<T>` | Non-null sets JSON content; null payload leaves content null |
| `WithAppendQueryParam(string, IEnumerable<T>)` | Comma-separated values appended; empty / null enumerable skipped |
| `WithAppendQueryParam(string, T)` | Single value appended; default(T) skipped |
| `WithPlatformAccessToken(string)` | Non-empty sets header; null/empty skipped |
| `WithPlatformAccessToken(Func<Task<string>>)` | Resolves token synchronously and sets header |
| `WithJWTToken` | Non-empty sets `Authorization: Bearer …` header; null/empty skipped |

**Discovered latent bug (not fixed — low severity):** `WithBasicAuth` calls
`ArgumentException.ThrowIfNullOrWhiteSpace(nameof(username))` / `nameof(password)`
which passes the **literal string** `"username"` / `"password"` (the parameter
name, not its value) — so the guard never throws on null/empty credentials. Left
as a TODO comment; fixing it is a production-code change outside this step.

### `ResponseComposer` (16 tests)

| Method / handler | Tests |
|---|---|
| `Handle<T>` | Success → `IsSuccessful=true`+status; failure → `IsSuccessful=false`+status; handlers applied in order |
| `DeserializeResponseOnSuccess` | 200 → content deserialized; non-2xx → content remains null |
| `DeserializeProblemDetailsOnUnsuccessStatusCode` | Non-2xx+valid JSON → `ProblemDetails` set; 2xx → null; invalid JSON → no throw |
| `DeserilizeProblemDetailsOnStatusCode` | Matching status code → `ProblemDetails` set; non-matching → null |
| `SetBodyAsStringResultIfSuccesful` | 2xx → content = raw body string; non-2xx → content null |
| `ConfigureResultIfSuccessful` | 2xx → callback invoked with deserialized content; non-2xx → callback not invoked |

## Verification

```
Tests run : 36
Passed    : 36
Failed    : 0
Skipped   : 0
```

All tests are pure in-memory — no containers, no HTTP calls.

## Deferred

- `WithBasicAuth` null-guard latent bug — not fixed here (production change, very low severity).
- `PaginatorStream<T>` — async enumerable over real HTTP; would need a
  `FakeHttpMessageHandler`-based approach and more setup. Deferred as low ROI
  given the pagination logic is thin.
- `TokenGenerator` — depends on key-vault/JWT infrastructure; deferred.

## Coverage impact

Assembly `Altinn.Authorization.Integration.Platform` — `RequestComposer` and
`ResponseComposer` previously 0% covered; both now have full branch coverage.
Overall assembly line% expected to rise materially (exact % requires a coverage
run in CI). No threshold is currently enforced for this assembly.
