# Step 6.7d (Part 4) — Coverage: DtoMapper Classes

## Goal

Continue Phase **6.7d** by covering the `DtoMapper` partial-class family and the
`RequestMapper` partial — the largest remaining untested surface in `AccessMgmt.Core`.

---

## What Changed

### New test file — `Utils/Mappers/DtoMapperTest.cs` (56 tests)

Single file in `Altinn.AccessMgmt.Core.Tests` that covers every static and instance
method spread across the seven `DtoMapper` partial-class files:

| Source file | Methods tested |
|---|---|
| `DtoMapper.cs` | `Convert(Entity, bool)` — scalars, type/variant constant lookup, null guard, parent mapping, `isConvertingParent` flag; `ConvertCompactRole` (null/non-null); `ConvertCompactPackage(ConnectionQueryPackage)` (null/non-null) |
| `DtoMapper.Simplified.cs` | `ToSimplifiedParty` (null/non-null, personal identifier excluded); `ToSimplifiedConnection` (null/non-null); `ToSimplifiedConnections` (null/non-null) |
| `DtoMapperAssignmentDto.cs` | `Convert(Assignment)` |
| `DtoMapperAssignmentPackageDto.cs` | `Convert(AssignmentPackage)`; `Convert(AssignmentResource)` |
| `DtoMapperPermissionDto.cs` | `ConvertToPermission(Assignment)`; `ConvertToPermission(Connection)` |
| `DtoMapperDelegationDto.cs` | `ConvertToDelegationDto(Delegation, pkgId, roleId)`; `ConvertToDelegationDto(ConnectionQueryExtendedRecord, pkgId, roleId)` |
| `CreateDelegationResponseDtoMapper.cs` | `Convert(Delegation)`; `Convert(IEnumerable<Delegation>)` |
| `DtoMapperRolePackage.cs` | `Convert(Package)` null/non-null; `Convert(Package, Area, Resources)` (with/without nulls); `Convert(Role)` null/non-null; `Convert(RolePackage)` null/non-null; `Convert(Area)` null/non-null; `Convert(Area, packages)` with/without nulls; `Convert(AreaGroup)` null/non-null; `Convert(AreaGroup, areas)` with/without nulls; `Convert(EntityType)` null/non-null; `Convert(EntityVariant)` null/non-null; `ConvertFlat(EntityVariant)` null/non-null; `Convert(Resource)` null/non-null |
| `DtoMapperAccessPackageDto.cs` | `Convert(PackageDto)` |
| `DtoMapperAuthorizedPartyDto.cs` | `ConvertToAuthorizedPartyDto` (scalars, subunits); `ConvertToAuthorizedPartiesDto` |
| `RequestMapper.cs` | `Convert(RequestAssignmentPackage)`; `Convert(RequestAssignmentResource)`; `ConvertToPartyEntityDto` (known constants, unknown constants → null) |

All tests are pure in-memory — no database, no container, no DI.

---

## Verification

```
Test run completed. Ran 207 test(s). 207 Passed, 0 Failed, 0 Skipped
```

(151 pre-existing + 56 new DtoMapper tests)

Build: **0 errors**.

---

## Deferred

- `DtoMapper.Extract*` family (`ExtractRelationDtoToOthers`, `ExtractRelationPackageDtoToOthers`,
  `ExtractSubRelation*`, `ExtractRelationDtoFromOthers`, etc.) requires constructing
  `IEnumerable<Connection>` with nested navigation properties — non-trivial setup,
  deferred to a later step if coverage numbers warrant it.
- `DtoMapperConnectionQuery.ConvertToOthers`, `ConvertFromOthers`, `ConvertPackages`,
  `ConvertSubConnections*` — same issue; deferred.
- `DtoMapperEntityVariant` (separate class, not `DtoMapper`) — tested indirectly through
  `DtoMapper.Convert(EntityVariant)` in `DtoMapperRolePackage.cs`; the separate
  `DtoMapperEntityVariant.Convert(EntityVariant)` overload would need its own test if
  coverage tooling distinguishes them.
