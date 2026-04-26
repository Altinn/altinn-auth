# Test Projects

All test projects live under `src/**/test/`. They target **`net9.0`** and use
**xUnit v3** with Microsoft Testing Platform (MTP). Common test infrastructure
(fixtures, mocks, certificates) is published from a single shared library —
`Altinn.AccessManagement.TestUtils` — referenced by the AccessManagement test
projects.

## Inventory

Grouped by the production vertical they cover.

### `app: AccessManagement`

| Test project | Covers | Fixture | Needs container |
|---|---|---|---|
| `AccessMgmt.Tests` | Legacy controller/integration tests being migrated off Yuniql. Uses `LegacyApiFixture` for the small tail of tests that still need the Yuniql schema. | `LegacyApiFixture`, `PostgresFixture` | ✅ |
| `Altinn.AccessManagement.Api.Tests` | The modernised cross-cutting API tests. | `ApiFixture` | ✅ |
| `Altinn.AccessManagement.Enduser.Api.Tests` | Enduser API controllers (`Connections`, `Consent`, `MaskinportenConsumers/Suppliers`, …). | `ApiFixture` + direct Moq unit tests | Partial |
| `Altinn.AccessManagement.ServiceOwner.Api.Tests` | ServiceOwner API controllers (`Request`, …). | `ApiFixture` + direct Moq unit tests | Partial |
| `Altinn.AccessMgmt.Core.Tests` | Business-logic unit tests for `AccessMgmt.Core`. | — | ❌ |
| `Altinn.AccessMgmt.PersistenceEF.Tests` | Repository / EF Core tests. | `EFPostgresFactory` | ✅ |

### `app: Authorization`

| Test project | Covers | Fixture | Needs container |
|---|---|---|---|
| `Altinn.Authorization.Tests` | Authorization controllers (`PolicyInformationPoint`, `DecisionController`, …). | `AuthorizationApiFixture` | ❌ |
| `Altinn.Authorization.Integration.Tests` | Cross-service integration scenarios. | `PlatformFixture` | ❌ |

### `lib: Integration`

| Test project | Covers |
|---|---|
| `Altinn.Authorization.Integration.Platform.Tests` | `RequestComposer`, `ResponseComposer`, other in-process integration primitives |

### `lib: Host`

| Test project | Covers | Blocked? |
|---|---|---|
| `Altinn.Authorization.Host.Lease.Tests` | Distributed lease primitive | ⚠️ Needs Azurite — skipped in CI, see [steps/INDEX.md#blocked-items](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md#blocked-items) |

### `pkg: ABAC`

| Test project | Covers |
|---|---|
| `Altinn.Authorization.ABAC.Tests` | XACML parsing, evaluation, combining algorithms |

### `pkg: PEP`

| Test project | Covers |
|---|---|
| `Altinn.Authorization.PEP.Tests` | Policy Enforcement Point helpers, ASP.NET Core handlers |

## Conventions

### Each `test/` folder has a `Directory.Build.props`

It sets `<IsTestProject>true</IsTestProject>` (or `<IsTestLibrary>true</IsTestLibrary>`
for `TestUtils`) and the xUnit version. The shared `Directory.Build.targets`
at the root adds `xunit.v3`, `coverlet.collector`, and `FluentAssertions`
automatically based on those flags — individual `.csproj` files stay tiny.

### `InternalsVisibleTo` is automatic

`src/Directory.Build.targets` emits
`[InternalsVisibleTo("<AssemblyName>.Tests")]` for every project. You do **not**
need to add the attribute manually. `DynamicProxyGenAssembly2` is added where
Moq needs to proxy `internal` interfaces (see
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/58_Coverage_AccessMgmt_Persistence_Services_StatusService_AuditService.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/58_Coverage_AccessMgmt_Persistence_Services_StatusService_AuditService.md)).

### Shared test library

| Library | Purpose |
|---|---|
| `Altinn.AccessManagement.TestUtils` | Canonical fixtures (`ApiFixture`), factories (`EFPostgresFactory`), mocks, test data, test certificates, token generator. Referenced by every AccessManagement test project. |

## Next: [FIXTURES.md](FIXTURES.md)
