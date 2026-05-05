---
step: 18
title: MaskinportenSchemaAuthorizer unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "delegations.admin scope check skips/breaks — admin should authorize regardless of requested scope"
  - "Empty/whitespace requested scope without admin: regression to authorize would let any consumer enumerate other parties' delegations"
  - "Consumer-prefix match without colon boundary: 'altinn:supplier' prefix must NOT match 'altinn:supplierfoo:read' (regression: substring match instead of prefix-with-separator)"
  - "Multiple consumer_prefix claims: ANY-match semantics (one matching prefix is enough)"
  - "Missing consumer_prefix claim + no admin scope returns false (no NRE)"
verifiedTests: 12
touchedFiles: 1
---

# Step 18 — `MaskinportenSchemaAuthorizer` unit tests

Pin the auth decision matrix for delegation-lookup endpoints in
[`MaskinportenSchemaAuthorizer.IsAuthorizedDelegationLookupAccess`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement/Utilities/MaskinportenSchemaAuthorizer.cs):

1. `delegations.admin` scope short-circuits → always authorized.
2. Otherwise the requested scope must start with one of the
   consumer's `consumer_prefix` claims followed by a colon.

Auth bug. False-positive enumerates other parties' delegations;
false-negative breaks legitimate supplier/admin lookups.

## Tests

[`AccessMgmt.Tests/Utilities/MaskinportenSchemaAuthorizerTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Utilities/MaskinportenSchemaAuthorizerTest.cs)
— 12 tests. `[Theory]` over null/empty/whitespace requested
scope; the rest cover admin-short-circuit, prefix-matching with
colon boundary, multiple-prefix any-match, and the no-claims
default.

## Verification

```text
12/12 pass; ~5 s.
```
