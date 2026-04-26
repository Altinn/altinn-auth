# Step 62 — Revert Step 48: `ApprovePackageRequest` security regression

## Goal

Remediate the changes introduced in [Step 48](48_Fix_6_7f_ApprovePackageRequest.md)
after tech-lead review flagged them as a security regression rather than a bug
fix. The audit preceding this step is captured in the chat log; this doc
summarises the findings and the actions taken.

## Findings from the Step 48 audit

1. **The root-cause analysis in Step 48 is wrong.** The step doc claimed
   `CheckPackage` → `GetAssignableAccessPackages` "validates that the approving
   organisation already holds the package it is about to assign." It does not.
   `ConnectionService.CheckPackage` calls
   `GetAssignableAccessPackages(party, auditAccessor.AuditValues.ChangedBy, packageIds)`
   — the underlying SQL query checks whether the **authenticated user
   (`ChangedBy`)** has permission to delegate the package on behalf of `party`.
   This is the normal, correct delegation-rights authorization and is the same
   path shipped and verified (manual + automated) for the Dec 2025 and
   Feb/Mar 2026 Access-Package delegation releases.

2. **The Step 48 "fix" removed the authorization entirely.** Replacing
   `connectionService.AddPackage(…)` with
   `assignmentService.ImportAssignmentPackages(…)` in the public approval
   endpoint means the approver's delegation rights are no longer checked.
   `ImportAssignmentPackages` is the A2 role-import helper ("*ikke laget for å
   skulle brukes av noe annet enn rolle-importen vår fra A2 som også skal
   opprette ett pakkeforhold*"); reusing it here also bypassed
   `PackageValidation.PackageIsAssignableToRecipient`, the connection-exists
   precondition (`Problems.MissingConnection`), and the
   `altinn2Client.ClearReporteeRights` post-step.

3. **A secondary undocumented change was bundled in.** Commit `103f8592` also
   removed `ValidatePartyIsOrg(toEntity, …)` from
   `AssignmentService.GetOrCreateAssignment`, silently allowing Person as the
   `to` side of a Rightholder assignment. This was not mentioned in the step
   doc and has no independent justification.

4. **The secondary `SaveChangesAsync` null-guard widens a narrow contract.**
   `ImportAssignmentPackages` is called with an explicit `AuditValues` in its
   legitimate (A2 import) call path; the `values is null ? SaveChangesAsync(ct)
   : SaveChangesAsync(values, ct)` branches only exist to support the improper
   new HTTP caller introduced in the same step.

5. **The test's premise is also wrong.**
   `Receiver_ApprovesPendingPackageRequest_ReturnsApproved` authenticates as
   `OddHalvorsen` (the request's sender, a Person) while calling
   `party=BakerJohnsen` (the receiver). No seeded connection grants Odd
   delegation rights on Baker's behalf, so `CheckPackage` correctly returned a
   validation error → the endpoint's original 400 was the expected behaviour,
   not a bug. The companion `NonReceiver_…` test fails earlier on a receiver
   mismatch and therefore does not exercise `CheckPackage` either — so after
   Step 48 no test covered the security-critical authorization path that Step
   48 removed.

## Actions

### Code reverts

- **`RequestController.ApprovePackageRequest`** restored to the pre-Step-48
  shape: call `assignmentService.GetOrCreateAssignment(request.To.Id,
  request.From.Id, Rightholder)` and then
  `connectionService.AddPackage(request.To.Id, request.From.Id,
  request.Package.Id.Value, ConfigureConnections, ct)`. This restores the
  validated delegation path (`CheckPackage` →
  `PackageValidation.AuthorizePackageAssignment` +
  `PackageValidation.PackageIsAssignableToRecipient`).

  Note on argument order: on an approve, the approver (`request.To`) is the
  delegator and the request sender (`request.From`) is the recipient, so the
  call is `GetOrCreateAssignment(fromEntityId: request.To.Id,
  toEntityId: request.From.Id, …)`. PR #2918 (`cd9be59b`) had fixed this
  exact mixup on `main` after Step 48; Step 61's initial revert accidentally
  re-swapped the arguments because it copied the pre-Step-48 text verbatim
  without merging the #2918 follow-up. Corrected here.

  File: `src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Api.Enduser/Controllers/RequestController.cs`

- **`AssignmentService.ImportAssignmentPackages`** — both `SaveChangesAsync`
  call sites restored to the explicit-audit overload
  `db.SaveChangesAsync(values, cancellationToken)`. The method's contract is
  once again narrow: callers must supply `AuditValues`. The A2 role-import
  callers already do.

  File: `src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Services/AssignmentService.cs`

- **`AssignmentService.GetOrCreateAssignment`** — the
  `ValidatePartyIsOrg(toEntity, ref errors, "$QUERY/to")` check is restored
  alongside the existing `from`-side check. The same silent removal was
  present in **both** `AssignmentService.cs` files (the newer
  `AccessMgmt.Core` EF implementation and the older `AccessMgmt.Persistence`
  repository-based implementation used by `RightsInternalController` and the
  admin role-sync path). Both are restored.

  Files:
  - `src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Core/Services/AssignmentService.cs`
  - `src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.Persistence/Services/AssignmentService.cs`

### Test change

- **`Receiver_ApprovesPendingPackageRequest_ReturnsApproved`** is
  `[Fact(Skip = …)]` with a TODO comment explaining the mis-seeding and the
  shape of a proper rewrite (e.g. authenticate as `MalinEmilie`, MD of
  `DumboAdventures`; seed the `RequestAssignment` with `To=DumboAdventures` and
  `From=<entity with an existing Rightholder connection to Dumbo>`; request a
  package the MD role permits Malin to delegate). The companion
  `NonReceiver_…` test is left unchanged — it still passes because it fails on
  a receiver-mismatch before reaching `CheckPackage`.

  File: `src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Controllers/RequestControllerTest.cs`

## Deferred

- **Proper rewrite of `Receiver_ApprovesPendingPackageRequest_ReturnsApproved`**
  with a correctly seeded approver (DAGL/MD of the receiver) and an existing
  Rightholder connection between receiver and sender.
- **Add a negative-auth test** asserting that an approver *without* delegation
  rights on the receiver's behalf receives `400/403` from `PUT
  /received/approve`. This assertion does not exist in the current suite and
  is the test that would have caught the Step 48 regression.
- Step 48's `TODO (6.7f)` items for `Sender_ConfirmsDraftRequest_ReturnsPending`
  remain deferred (environmental investigation) — unchanged by this step.

## Verification

Built and ran the affected test project (`Altinn.AccessManagement.Enduser.Api.Tests`)
after the reverts. Results recorded in the commit accompanying this step.

## Impact on the step log / coverage accounting

Step 30 had noted that `ApprovePackageRequest` was among the "five untested
`RequestController` endpoints" closed in that step. After Step 62 that endpoint
is again exercised only by the `NonReceiver_…` negative test plus the
`[Skip]`ped happy path — so the real coverage of the authorization path is
back to what it was before Step 48. The coverage % measured in Step 30 will
drop marginally; this is intentional and should not be re-closed until the
deferred rewrite above lands.
