# Step 50 — Coverage: AccessManagement.Integration `DelegationRequestProxy`

## Goal

Add direct unit tests for `DelegationRequestProxy` in `AccessManagement.Integration`
(the only non-`[ExcludeFromCodeCoverage]` class in that assembly not yet covered).

No container dependency — `HttpClient` is exercised via a lightweight inline
`FakeHttpMessageHandler` that captures the request URI and returns a
pre-canned response.

## Pattern

```csharp
var handler = new FakeHttpMessageHandler(statusCode, body, capturedUris);
var httpClient = new HttpClient(handler);
var settings = Options.Create(new SblBridgeSettings { BaseApiUrl = "https://..." });
var logger = Mock.Of<ILogger<DelegationRequestProxy>>();
var sut = new DelegationRequestProxy(httpClient, settings, logger);
```

`FakeHttpMessageHandler` is a private `sealed class` nested in the test class
that overrides `SendAsync`, records `request.RequestUri` for query-string
assertions, and returns the configured `HttpResponseMessage`.

## Files Changed

| File | Description |
|---|---|
| `test/AccessMgmt.Tests/Services/DelegationRequestProxyTest.cs` | **New** — 5 tests covering all branches of `GetDelegationRequestsAsync` |

## Test Cases

| Test | Scenario |
|---|---|
| `GetDelegationRequestsAsync_OkResponse_ReturnsDeserializedRequests` | 200 OK + JSON body → returned `DelegationRequests` with correct fields |
| `GetDelegationRequestsAsync_NonOkResponse_ReturnsNull` | 500 response → returns `null` |
| `GetDelegationRequestsAsync_AllOptionalParams_IncludedInQueryString` | `serviceCode`, `serviceEditionCode`, two `status` values, `continuation` → all present in URL |
| `GetDelegationRequestsAsync_EmptyServiceCodeAndNullEditionCode_OmittedFromQuery` | empty string / null optional params → absent from URL |
| `GetDelegationRequestsAsync_MultipleItems_AllDeserialized` | 3-item list → all 3 entries mapped |

## Test Results

```
5 / 5 passed  (0 failed, 0 skipped)
```

## Coverage Impact

`AccessManagement.Integration` baseline: **47.57% line** (Step 12).
`DelegationRequestProxy` is the only class in the assembly not annotated with
`[ExcludeFromCodeCoverage]`; these 5 tests bring it to full branch coverage.
Exact percentage requires a full `run-coverage.ps1` run with the DB container.

## Deferred

- All `Clients/` HTTP clients in `AccessManagement.Integration`
  (`AccessListAuthorizationClient`, `Altinn2ConsentClient`, `Altinn2RightsClient`,
  `AltinnRolesClient`, `AuthenticationClient`, `PartiesClient`, `ProfileClient`,
  `ResourceRegistryClient`) are marked `[ExcludeFromCodeCoverage]` and are out
  of scope.
