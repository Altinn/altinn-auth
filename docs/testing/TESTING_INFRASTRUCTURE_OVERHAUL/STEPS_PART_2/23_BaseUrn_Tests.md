---
step: 23
title: BaseUrn URN-string composition unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "URN segment lost casing → urn:Altinn:Person silently breaks all URN matching"
  - "URN hierarchy nesting wrong (e.g. EnterpriseUser.Organization.Uuid composing to urn:altinn:organization:uuid instead of urn:altinn:enterpriseuser:organization:uuid)"
  - "Hardcoded URN constants (ResourceRegistryId, AppOwner, AppId) drift from spec"
  - "InternalIds collection drops/adds attribute identifiers — silently changes which entities are treated as internal"
verifiedTests: 13
touchedFiles: 1
---

# Step 23 — `BaseUrn` URN-string composition unit tests

Pin URN string composition in
[`BaseUrn`](../../../src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement.Core/Resolvers/BaseUrn.cs)
— the canonical source of `urn:altinn:*` strings used across
the access-management surface. A regression that produces
mixed-case or wrongly-nested URNs would silently break URN
matching against XACML attributes.

## Tests

[`AccessMgmt.Tests/Resolvers/BaseUrnTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Resolvers/BaseUrnTest.cs)
— 13 tests pinning Person, Organization, EnterpriseUser (with
nested Organization), and Resource URN composition at every
level, plus the `InternalIds` collection membership.

## Verification

```text
13/13 pass.
```
