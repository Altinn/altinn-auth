# Step 9 — Shared Fixture for Authorization.Tests (Phase 2.4)

## Goal

Eliminate unnecessary test server creation in Authorization.Tests integration
tests by sharing the `AuthorizationApiFixture` server instance across tests
that don't need per-test service overrides.

Fixes audit issue **M5**: factory rebuilt per-test via `WithWebHostBuilder` even
when no per-test overrides are needed.

## Before

Three decision test classes (`AltinnApps_DecisionTests`,
`ResourceRegistry_DecisionTests`, `ExternalDecisionTest`) called
`GetTestClient()` for every test method. This method used
`_fixture.WithWebHostBuilder(...)` which creates a **new derived factory and
test server** for each call — even when the builder callback registered zero
additional services.

**Impact:** ~44 tests each created their own test server unnecessarily.

## Changes

### Shared `_client` via `BuildClient()`

Each of the three decision test classes was refactored to:

1. Add a `_client` field initialized in the constructor via `fixture.BuildClient()`
2. Replace `GetTestClient()` (no-args) calls with `_client` — the shared client
   reuses the fixture's single test server
3. Keep `GetTestClient(args)` with `WithWebHostBuilder` only for tests that need
   per-test mock verification (e.g., asserting `IEventsQueueClient` call counts)

### Dead code cleanup

- `PDP_Decision_AltinnApps0010`: Removed unused local `Mock<IFeatureManager>`,
  `Mock<IEventsQueueClient>`, and `AuthorizationEvent` variables that were
  created but never registered in DI (the test called `GetTestClient()` with no
  args, so these mocks were dead code).

## Test count by class

| Class | Total tests | Using shared `_client` | Still using `GetTestClient(args)` |
|---|---|---|---|
| `AltinnApps_DecisionTests` | 30 | 19 | 11 |
| `ResourceRegistry_DecisionTests` | 22 | 19 | 3 |
| `ExternalDecisionTest` | 21 | 14 | 7 |
| **Total** | **73** | **52** | **21** |

**52 tests** no longer create per-test servers. **21 tests** that need per-test
mock verification still use `WithWebHostBuilder`.

## Files Changed

| File | Action |
|---|---|
| `AltinnApps_DecisionTests.cs` | Added `_client` field; 19 tests use shared client |
| `ResourceRegistry_DecisionTests.cs` | Added `_client` field; 19 tests use shared client |
| `ExternalDecisionTest.cs` | Added `_client` field; 14 tests use shared client |

## Verification

- [x] Build passes (0 errors)
- [x] All 402 Authorization.Tests pass (0 failed, 0 skipped)
- [x] All 73 decision tests pass individually
