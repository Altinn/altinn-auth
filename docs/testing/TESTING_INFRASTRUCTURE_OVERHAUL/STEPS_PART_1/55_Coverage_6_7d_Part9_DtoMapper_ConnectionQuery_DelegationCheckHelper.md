# Step 55 — Coverage 6.7d Part 9: `DtoMapper` Connection-Query Methods + `DelegationCheckHelper` Deferred Tests

## Goal

Close the two areas explicitly marked **deferred** in Steps 44–54:

1. **`DtoMapper` connection-query overloads** (`ConvertToOthers`, `ConvertFromOthers`,
   `ConvertSubConnections*`, `ConvertPackages`, `ConvertResources`, `ConvertToAgentDto`,
   and all `Extract*` instance methods) — previously deferred due to "complex `Connection`
   graph setup".  Both the `ConnectionQueryExtendedRecord` path and the old `Connection`
   model path are plain POCOs; no database is needed.

2. **`DelegationCheckHelper` deferred methods** (`GetFirstAccessorValuesFromPolicy`,
   `DecomposePolicy`, `BuildDelegationRuleTarget`, `CalculateRightKeys`, `IsAppResource`,
   `CheckIfErrorShouldBePushedToErrorQueue`) — previously deferred due to "XACML object
   graph setup required".  All XACML types (`XacmlRule`, `XacmlPolicy`, `XacmlTarget`,
   `XacmlAnyOf`, `XacmlAllOf`, `XacmlMatch`, `XacmlAttributeDesignator`,
   `XacmlAttributeValue`) are directly constructable without a database or container.

## What Changed

### New file

`src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Utils/Mappers/DtoMapperConnectionQueryTest.cs`

**38 new tests** covering:

| Method | Tests | Key scenarios |
|---|---|---|
| `ConvertToOthers` | 7 | empty; single Assignment; KeyRole excluded/included; Delegation excluded; same-ToId dedup; ViaId → sub-connections; packages mapped |
| `ConvertFromOthers` | 5 | empty; single record; Hierarchy excluded/included; same-FromId dedup |
| `ConvertSubConnectionsToOthers` | 3 | null input; empty; groups by ToId with roles + packages |
| `ConvertSubConnectionsFromOthers` | 2 | empty; groups by FromId with roles + resources |
| `ConvertPackages` | 2 | single package with permission; same package across two records → 2 permissions |
| `ConvertResources` (ExtendedRecord) | 1 | single resource maps with permission |
| `ConvertToAgentDto` | 2 | single agent with roles + packages; two agents → two DTOs |
| `Convert(ConnectionQueryPackage)` | 1 | Id, Urn, AreaId mapped |
| `Convert(ConnectionQueryResource)` | 1 | Id, Name, RefId mapped |
| `ExtractRelationDtoToOthers` | 2 | only "Direct"; includeSubConnections=true |
| `ExtractRelationDtoFromOthers` | 1 | all, grouped by From.Id |
| `ExtractSubRelationDtoToOthers` | 2 | filters by via + non-Direct; Direct excluded |
| `ExtractSubRelationDtoFromOthers` | 1 | filters by via + non-Direct |
| `ExtractRelationPackageDtoToOthers` | 1 | Direct with packages |
| `ExtractRelationPackageDtoFromOthers` | 1 | all from with packages |
| `ExtractSubRelationPackageDtoToOthers` | 1 | via-filtered with packages |
| `ExtractSubRelationPackageDtoFromOthers` | 1 | via-filtered from with packages |

### Extended file

`src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Utils/DelegationCheckHelperTest.cs`

**25 new tests** (class-level description updated from "IsAccessListModeEnabledAndApplicable only" to all methods).  Added usings: `Altinn.AccessManagement.Core.Constants`, `Altinn.Authorization.ABAC.Constants`, `Altinn.Authorization.ABAC.Xacml`.

Private XACML builder helpers added to the class:
- `MakeXacmlMatch(category, attributeId, value)` — constructs a single `XacmlMatch`
- `MakeRule(subject?, resources?, action?)` — builds a `XacmlRule` with separate
  AnyOfs per category (mirrors `BuildDelegationRuleTarget` structure)
- `MakePolicy(params XacmlRule[])` — wraps rules in a minimal `XacmlPolicy`

| Method | Tests | Key scenarios |
|---|---|---|
| `GetFirstAccessorValuesFromPolicy` | 4 | empty target; single subject match → formatted value; two matches in one AllOf → excluded (count ≠ 1); wrong category → empty |
| `DecomposePolicy` | 4 | matching resource + role → one Right; non-matching resource → empty; non-user subject (PartyUuid) filtered → empty; access-package subject → included |
| `BuildDelegationRuleTarget` | 3 | 3 AnyOfs returned; subject has PartyUuid attrId + toId value; resource AnyOf has one match per entry |
| `CalculateRightKeys` | 3 | regular resource matched → hashed key starts with "01", length 66; non-matching → empty; org+app rewritten to `app_ttd_myapp` → matched |
| `IsAppResource` | 3 | `app_ttd_myapp` → true, org/app set; `regular-resource` → false; `app_ttd` (two-part) → true, org/app null |
| `CheckIfErrorShouldBePushedToErrorQueue` | 6 | `Resource 'X' not found`; FK-to constraint; FK-from constraint; audit fields; failed to find policy file; generic → false |

## Verification

```
Ran 63 test(s) — 63 Passed, 0 Failed, 0 Skipped
(38 DtoMapperConnectionQueryTest + 25 DelegationCheckHelperTest)
Build: successful
```

## Deferred Items (remaining after this step)

- `DtoMapper.ConvertResources(IEnumerable<AssignmentResource>)` — needs `Resource`
  model (Version7 UUID constructor) + `Assignment` with `From`/`To` populated;
  low marginal coverage value, deferred.
- `DtoMapper.Extract*` edge cases (null Package in ExtractRelationPackage* → not included)
  — low value, deferred.
- `AccessMgmt.Persistence` (32.51%) and `AccessManagement.Persistence` (44.94%) —
  dominated by Npgsql repository code; require a live DB.
- `DelegationCheckHelper.DecomposePolicy` with org/app resource rewriting (via
  `CalculateRightKeys` internal path) — covered indirectly by `CalculateRightKeys_OrgAppResource*`.
