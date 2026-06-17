# Test Naming Convention

## Standard

All test methods follow this pattern:

```
MethodUnderTest_Scenario_ExpectedResult
```

The three segments answer three questions:

- **MethodUnderTest** — what is exercised: a method, or for HTTP an action/endpoint.
- **Scenario** — the input or state that makes this case distinct.
- **ExpectedResult** — the *observable end result*: what the caller gets back or what changes. This is the segment that carries the most value. Describe **what is returned or what happens**, not merely a status code.

This is the classic `UnitOfWork_Scenario_ExpectedBehavior` shape (Osherove); the emphasis here is that the third segment names the *behaviour/outcome*, in domain terms, rather than a bare technical code.

### The result segment

The result segment must describe the concrete outcome. For an HTTP endpoint the observable result is the response, so name the status **and** the meaningful body or effect: a status code on its own (`Returns400BadRequest`) tells the reader the shape of the failure but not *what* failed.

**HTTP endpoints** — `Returns{Status}With{Outcome}`:

```csharp
// ✅ describes the result, not just the code
Post_Delegation_ValidRequest_Returns200WithDelegatedRights()
Post_Delegation_InvalidFromAndTo_Returns400WithInvalidPartyUrnErrors()
Get_Delegations_NoneExist_Returns200WithEmptyList()
Post_Delegation_CallerLacksAccess_Returns403WithMissingDelegationAccessError()
Get_Resource_UnknownId_Returns404WithResourceNotFoundProblem()

// ✅ status-only — only when the response has no meaningful body
Delete_Delegation_Existing_Returns204NoContent()

// ❌ status-only where a body/effect exists — say what is returned
Post_Delegation_ValidRequest_Returns200Ok()
Post_Delegation_InvalidFrom_Returns400BadRequest()
```

**Service / unit tests** — `{Verb}{ConcreteResult}` naming the return value or side effect:

```csharp
// ✅
GetUser_UnknownId_ReturnsNull()
ListDelegations_NoneExist_ReturnsEmptyList()
MapToDto_FullEntity_ReturnsDtoWithAllFieldsPopulated()
Authorize_MatchingDenyRule_ReturnsDeny()
WriteContextResponse_PermitResult_WritesPermitDecision()
CreateXacmlJsonRequest_MissingSubject_ThrowsArgumentException()

// ❌ opaque, or names the function instead of the outcome
HandleRequirementAsync_TC01()
MatchAttributes_StringIsIn()
```

### Guidelines

| Principle | Detail |
|---|---|
| **Describe the result, not just the code** | The result segment says what the caller gets or what changed: `Returns400WithInvalidPartyUrnError`, not `Returns400BadRequest`. |
| **Keep the status as a prefix (HTTP)** | For HTTP, start the result with the numeric status so the contract stays unambiguous: `Returns{NNN}With{Outcome}`. |
| **Status-only is the exception** | `Returns204NoContent` / `Returns200Ok` are fine only when the response carries no meaningful body or effect. |
| **No opaque IDs** | `TC01`, `0001`, `AltinnApps0001` mean nothing without the body. Describe the scenario and result instead. |
| **Use the method or action** | Start with the method, or for HTTP the action (`Post_Delegation_…`, `Get_Delegations_…`). |
| **Scenario before result** | Input/state first, then the outcome. |
| **Keep it concise** | Readable at a glance; abbreviate unambiguous terms. |
| **Async suffix** | Not required on test method names (suppressed via `VSTHRD200`). |

### Result vocabulary

The result segment is a status (HTTP) or verb (service) plus a description of the outcome.

| Context | Form | Examples |
|---|---|---|
| HTTP success with body | `Returns{NNN}With{What}` | `Returns200WithDelegatedRights`, `Returns201WithCreatedDelegation`, `Returns200WithEmptyList` |
| HTTP failure | `Returns{NNN}With{Cause}` | `Returns400WithInvalidPartyUrnError`, `Returns403WithMissingDelegationAccessError`, `Returns404WithResourceNotFoundProblem` |
| HTTP, no meaningful body | `Returns{NNN}{Name}` | `Returns204NoContent`, `Returns200Ok` |
| Service return value | `Returns{Outcome}` | `ReturnsNull`, `ReturnsEmptyList`, `ReturnsFalse`, `ReturnsMappedDto` |
| Service side effect | `{Verb}{Effect}` | `WritesPermitDecision`, `PersistsDelegation`, `RevokesAllRights` |
| Expected exception | `Throws{Exception}` | `ThrowsArgumentException`, `ThrowsValidationExceptionForFrom` |

Status numbers map to names as before (200 Ok, 201 Created, 202 Accepted, 204 NoContent, 206 PartialContent, 400 BadRequest, 401 Unauthorized, 403 Forbidden, 404 NotFound, 500 InternalServerError). Keep the number; append what the response actually contains.

Do **not** use a bare status word (`BadRequest`), a synonym (`Success`, `OK`, `Valid`, `ReturnOk`), or a numeric code with no name (`Returns400`) as the result: those are ambiguous, or describe the scenario rather than the outcome.

### Scope

Applies to all test projects: the AccessManagement projects (`AccessMgmt.Tests`, `Enduser.Api.Tests`, `Api.Tests`, `ServiceOwner.Api.Tests`, `Api.Internal.Tests`, `Core.Tests`, `PersistenceEF.Tests`), the ABAC test project (`Altinn.Authorization.ABAC.Tests`), and the Authorization test projects. New tests must follow it; existing tests were aligned in #3500.

This is a review-time convention, not CI-enforced. Reviewers should flag bare status words (`BadRequest`), numeric codes with no name (`Returns400`), opaque ids (`_TCxx`), and status-only HTTP results where the response has a meaningful body.
