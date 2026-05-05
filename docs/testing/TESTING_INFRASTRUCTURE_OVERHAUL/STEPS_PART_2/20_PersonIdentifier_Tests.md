---
step: 20
title: PersonIdentifier SSN-validation unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Length guard regression — 10 or 12 digit string accepted as a valid SSN"
  - "Non-numeric content (letters, dashes, whitespace) accepted as valid"
  - "Modulo-11 algorithm regression — control digits k1 / k2 not validated against the new 4-candidate-k1 rule"
  - "JSON deserialization of invalid SSN silently succeeds (must throw JsonException)"
  - "TryFormat: small destination buffer silently corrupts (must return false with charsWritten=0)"
  - "Equality semantics — operator==(null, null) returns false, operator==(null, value) returns true, etc."
verifiedTests: 21
touchedFiles: 1
---

# Step 20 — `PersonIdentifier` SSN-validation unit tests

Pin the parsing and equality contract of
[`Altinn.Authorization.Api.Contracts.Register.PersonIdentifier`](../../../src/libs/Altinn.Authorization.Api.Contracts/src/Altinn.Authorization.Api.Contracts/Register/PersonIdentifier.cs)
— the SSN value object used across the entire codebase. Was used
in many tests but no direct unit test covered the validation
algorithm.

## Tests

[`AccessMgmt.Tests/Models/Urn/PersonIdentifierTest.cs`](../../../src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Models/Urn/PersonIdentifierTest.cs)
— 21 tests across five sections:

- Length guard `[Theory]` — empty / 10 / 12 digits, plus null
  string.
- Content guard `[Theory]` — letters, spaces, dashes.
- Algorithm — three known-valid SSNs, plus a flipped-k2 invalid
  case (`02013299996`, the valid `02013299997` with the last
  control digit changed).
- Equality — `PersonIdentifier == PersonIdentifier`,
  `PersonIdentifier == string`, null/null, null/value.
- JSON round-trip + invalid-deserialize-throws.
- `TryFormat` buffer-too-small / buffer-OK paths.

Note: `00000000000` is *valid* under the new modulo-11 algorithm
(both k1 and k2 reduce to 0), so a flipped-digit case from a
known-valid SSN was used as the negative example instead.

## Verification

```text
21/21 pass; ~0.65s.
```
