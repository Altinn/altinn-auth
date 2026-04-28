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
| `Altinn.Authorization.Host.Lease.Tests` | Distributed lease primitive | ⚠️ Needs Azurite — skipped in CI, see [steps/INDEX.md#blocked-items](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md#blocked-items) |
| `Altinn.Authorization.Host.Pipeline.Tests` | Pipeline hosted services, builders, segment/sink/source services | 🌱 Scaffold only — one smoke test against `PipelineMessage<T>`. Real coverage to follow under Phase D.1 of the [Part 2 plan](TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#4-improvement-plan--phases). |

### `pkg: ABAC`

No dedicated test project. `Altinn.Authorization.ABAC` is exercised
indirectly by `Altinn.Authorization.Tests` (PEP → ABAC paths during
the end-to-end XACML decision tests). Coverage typically lands around
63 % line / 61 % branch — see the
[`COVERAGE.md`](COVERAGE.md) ratchet (60 % enforced).

If a direct ABAC unit-test suite is wanted later, recreate
`src/pkgs/Altinn.Authorization.ABAC/test/` with the standard
`Directory.Build.props` (`<XUnitVersion>v3</XUnitVersion>`,
`<IsTestProject>true</IsTestProject>`) and a single test csproj. The
prior empty shell was deleted in the Part 2 testing-infrastructure
overhaul (audit ID `C1'`).

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
