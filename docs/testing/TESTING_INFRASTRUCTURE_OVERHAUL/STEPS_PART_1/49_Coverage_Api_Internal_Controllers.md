# Step 49 — Coverage: AccessManagement.Api.Internal Controllers

## Goal

Improve line coverage of `Altinn.AccessManagement.Api.Internal` (baseline 46.74%)
by adding direct Moq-based unit tests for the three untested controllers:
`InternalConnectionsController`, `SystemUserClientDelegationController`, and `PartyController`.

No WAF/container dependency — controllers are instantiated directly with mocked services.

## Pattern

Matches the direct-controller pattern established in Step 42 (`PackagesControllerTest`):

```csharp
var controller = new MyController(mockedService.Object);
controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
var result = await controller.SomeAction(...);
Assert.IsType<OkObjectResult>(result);
```

Key type-resolution lessons applied:
- `ConnectionOptions` → alias `Altinn.AccessMgmt.Core.Services.ConnectionOptions`
- `ValidationErrors` → alias `Altinn.AccessMgmt.Core.Utils.Models.ValidationErrors`
- `Assignment`/`Delegation`/`Role` from `Altinn.AccessMgmt.PersistenceEF.Models` (not `Persistence.Models`)
- `Result<T>` constructed via `new Result<T>(value)` (no static `.Success()` factory)
- `null` returns from `Task<T>` mocks via `Returns(Task.FromResult<T>(null))`
- `ValidationProblemInstance` (for error paths) via `ValidationComposer.Validate(...)` helper
- `PartyBaseDto` requires three `required` fields: `EntityType`, `EntityVariantType`, `DisplayName`
- `JwtSecurityTokenHandler.WriteToken` produces signature-free tokens readable by `ReadJwtToken`

## Files Changed

| File | Description |
|---|---|
| `test/Altinn.AccessManagement.Api.Tests/Controllers/InternalConnectionsControllerTest.cs` | **New** — 17 tests covering all 6 actions (success + failure branches) |
| `test/Altinn.AccessManagement.Api.Tests/Controllers/SystemUserClientDelegationControllerTest.cs` | **New** — 13 tests covering GetClients (3 branches), GetClientDelegations, PostClientDelegation, DeleteDelegation (4 branches), DeleteAssignment (5 branches) |
| `test/Altinn.AccessManagement.Api.Tests/Controllers/PartyControllerTest.cs` | **New** — 6 tests covering AddParty (null token, no app claim, wrong app, service problem, party created, party existing) |

## Test Results

```
34 / 34 passed  (0 failed, 0 skipped)
```

| Test class | Tests | Passed |
|---|---|---|
| `InternalConnectionsControllerTest` | 17 | 17 ✅ |
| `SystemUserClientDelegationControllerTest` | 13 | 13 ✅ |
| `PartyControllerTest` | 6 | 6 ✅ |

## Coverage Impact

Baseline for `AccessManagement.Api.Internal`: **46.74% line** (Step 12 baseline).

These 34 tests exercise the core branch logic of all three main controllers.
The `Bff/ConsentController` remains untested (deferred — depends on `IPDP` +
`UserUtil` which require deeper service graph setup).

Estimated impact: **+10–15 pp line** on `AccessManagement.Api.Internal`
(exact measurement requires a full `run-coverage.ps1` run with the DB container).

## Deferred

- **`Bff/ConsentController`** — uses `IPDP` + `UserUtil` that require more
  service graph mocking; low-ROI for the complexity. Deferred to a future step.
- **`DelegationRequestProxy` tests** in `AccessMgmt.Tests` — the
  `AccessManagement.Integration` HTTP clients are all `[ExcludeFromCodeCoverage]`
  except `DelegationRequestProxy`; deferred as the remaining untested integration
  code is dominated by real HTTP calls that need `MockHttpMessageHandler`.

## Notes

- `SystemUserClientDelegationController.DeleteAssignment` uses `assignment.Role.Code`
  where `Role` is typed as `Altinn.AccessMgmt.PersistenceEF.Models.Role` (the
  `Assignment` class in `PersistenceEF.Models` already has a `Role` navigation
  property — no `ExtendedAssignment` needed).
- The `DeleteDelegation` success path depends on `assignmentService.GetAssignment`
  returning a matching `ToId == party`; the test constructs the mock accordingly.
