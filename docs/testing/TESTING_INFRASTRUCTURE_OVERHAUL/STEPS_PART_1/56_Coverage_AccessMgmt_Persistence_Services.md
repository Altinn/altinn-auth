# Step 56 — Coverage: `AccessMgmt.Persistence` Services — `AMPartyService`, `EntityService`, `PartyService`

## Goal

Increase coverage of `Altinn.AccessMgmt.Persistence` (last measured: **32.51% line**) by
adding pure Moq-based unit tests for three public service classes whose business logic
is fully exercisable without a database or container.

All three services use constructor-injected repository interfaces
(`IEntityLookupRepository`, `IEntityRepository`, `IEntityTypeRepository`,
`IEntityVariantRepository`), making them straightforward to test by mocking the
`IDbBasicRepository<T>` / `IDbExtendedRepository<T, TExtended>` overloads.

---

## Key technical observations (pre-work)

| Finding | Detail |
|---|---|
| `GenericFilterBuilder<T> : IEnumerable<GenericFilter>` | When a service calls `repo.GetExtended(filter)` with a `GenericFilterBuilder<T>`, it resolves to the `GetExtended(IEnumerable<GenericFilter>)` overload — mockable with `It.IsAny<IEnumerable<GenericFilter>>()`. |
| Expression-based `GetExtended<TProperty>` | `GetByUuid` and `GetChildren` call `repo.GetExtended(t => t.Prop, value)`. These use the `Expression<Func<TExtended, TProperty>>` overload and require Moq to know the exact `TProperty` type. `Entity.ParentId` is `Guid?` (not `Guid`), so the mock must use `It.IsAny<Guid?>()`. |
| `OrganizationNumber` (Api.Contracts) uses a checksum | `937884117` and `923609016` are valid. `919272567` is **not** (fails `IsValidOrganizationIdentifier`). |
| `Result<T>.IsProblem` / `.Value` | `Result<T>` from `Altinn.Authorization.ProblemDetails` exposes `IsProblem` (bool) and `Value` (T) — confirmed from `PartyController`. |

---

## What changed

### New test files — `AccessMgmt.Tests/Services/`

#### `AMPartyServiceTest.cs` — 19 tests

Covers all 5 public methods of `AMPartyService` (single `IEntityLookupRepository` dep):

| Method | Scenarios |
|---|---|
| `GetByOrgNo` | empty → null; single → `MinimalParty` with `OrganizationId`; multiple → `InvalidOperationException` |
| `GetByPartyId` | empty → null; multiple → throws; person type → `PersonId` set; org type → `OrganizationId` set; unknown type → both null |
| `GetByPersonNo` | empty → null; multiple → throws; single → `PersonId` set |
| `GetByUuid` | empty → null; `OrganizationIdentifier` in dict → `OrganizationId`; `PersonIdentifier` in dict → `PersonId`; `Name` in dict overrides entity name |
| `GetByUserId` | empty → null; multiple → throws; person type → `PersonId`; other type → `PersonId` null |

#### `EntityServiceTest.cs` — 7 tests

Covers `EntityService` (`IEntityRepository` + `IEntityLookupRepository`):

| Method | Scenarios |
|---|---|
| `GetByOrgNo` | empty → null; multiple → `Exception`; single → returns `Entity` |
| `GetByPersNo` | always `NotImplementedException` |
| `GetByProfile` | always `NotImplementedException` |
| `GetChildren` | expression overload (`Guid?`) → returns `IEnumerable<Entity>` |
| `GetParent` | Guid overload → returns `Entity` |

#### `PartyServiceTest.cs` — 5 tests

Covers `PartyService` (`IEntityRepository` + `IEntityTypeRepository` + `IEntityVariantRepository`):

| Scenario | Expected |
|---|---|
| Entity already exists | `result.IsProblem = false`, `PartyCreated = false` |
| Entity not found + unsupported EntityType (`"Organisation"`) | `result.IsProblem = true` |
| Entity not found + type not found in DB | `result.IsProblem = true` |
| Entity not found + variant not found | `result.IsProblem = true` |
| Entity not found + all valid → `Create` returns 1 | `result.IsProblem = false`, `PartyCreated = true`, `PartyUuid` matches |

---

## Verification

```
Ran 31 test(s) — 31 Passed, 0 Failed, 0 Skipped
Build: successful
```

All new tests are deterministic and require no containers or external services.

---

## Coverage impact

The three service classes add previously-uncovered branches and statements in
`Altinn.AccessMgmt.Persistence`. Exact new percentage will be measured in the next
full coverage run; this step targets the 32.51% baseline.

---

## Deferred items

- **`AccessMgmt.Persistence` repositories** (`AssignmentRepository`, `ConnectionRepository`,
  etc.) — all Npgsql-based SQL; require a live Postgres container.  These dominate the
  remaining gap and are best addressed via DB-backed integration tests in a future step.
- **`AccessManagement.Persistence`** (44.94%) — `ConsentRepository` (Npgsql),
  `PolicyRepository` (Azure Blob), `DelegationMetadataEF` (EF Core + Postgres).
  All require real infrastructure.
- **`MaskinportenConsumersController` / `MaskinportenSuppliersController`** — requires
  PDP stubbing or resource seeding; deferred from Step 33.
- **`Sender_ConfirmsDraftRequest_ReturnsPending`** — still `[Skip]`ped, needs
  separate investigation.
