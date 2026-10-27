# AGENTS.md — Altinn.AccessManagement

Administration of rights and delegations: who may act on behalf of whom, for which resource. Thirteen source projects and seven runnable test projects, so orienting yourself costs more here than anywhere else in the repository. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md).

## Commands

```bash
dotnet build src/apps/Altinn.AccessManagement/Altinn.AccessManagement.slnx
dotnet test src/apps/Altinn.AccessManagement/Altinn.AccessManagement.slnx -- --filter-trait "Category=Unit"
dotnet run --project src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement -- --RunInitOnly=true   # once per fresh database: EF migrations and static data
dotnet run --project src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
```

Uses the `authorizationdb` database with the roles `platform_authorization` and `platform_authorization_admin`. Local setup is in the [repository README](../../../README.md#local-development-environment).

## Where things live

Six API surfaces, each a project of its own, so the first question for a change is which audience it serves:

| Surface | Audience |
| --- | --- |
| `Api.Enduser` | The citizen and business user, seven controllers, the largest surface |
| `Api.ServiceOwner` | Service owners |
| `Api.Maskinporten` | Machine-to-machine clients |
| `Api.Enterprise` | Enterprise integrations |
| `Api.Internal` | Internal platform callers |
| `Api.Metadata` | Metadata and lookups |

Behind them: `Core` and `AccessMgmt.Core` for domain logic, `Persistence` and `AccessMgmt.PersistenceEF` for storage, `Integration` for outbound calls, and `AppHost` in the solution.

Decisions for this vertical are in [`docs/adr/`](docs/adr/README.md). Read [ADR-0001](docs/adr/0001-ef-core-persistence.md) before adding persistence, a migration or Core-layer code, and [ADR-0002](docs/adr/0002-init-run-under-lease.md) before changing what runs at startup or in the init run, migrations and seed data included.

## Landmines

- **There are two naming families, both live, and `Altinn.AccessMgmt.*` is the new one.** `Altinn.AccessManagement.Core` sits beside `Altinn.AccessMgmt.Core`, and `Altinn.AccessManagement.Persistence` beside `Altinn.AccessMgmt.PersistenceEF`; the host references both persistence projects (`Altinn.AccessManagement.csproj`, lines 45 and 46). New persistence goes in `Altinn.AccessMgmt.PersistenceEF`, and new Core-layer code in `Altinn.AccessMgmt.Core`. Code still in the `Altinn.AccessManagement.*` projects is maintained there until it is migrated in large chunks. Recorded as decision 6 in [ADR-0001](docs/adr/0001-ef-core-persistence.md).
- **One test project's folder and project names differ.** The folder is `test/AccessMgmt.Tests` and the project inside it is `Altinn.AccessManagement.Tests.csproj`. The seven other test projects match. So `dotnet test .../test/Altinn.AccessManagement.Tests` fails and `dotnet test .../test/AccessMgmt.Tests` works.

_More belongs here._ This is the section that pays for itself, and it can only come from people who know why the code is the way it is.

## Test gotchas

- **A missing container runtime fails this vertical's integration tests, it does not skip them.** `PostgresTestEngine` records a `SkipReason`, but `ApiFixture` and `EFPostgresFactory` surface it during fixture initialisation, where xUnit v3 reports a skip as a fixture-init *failure*. So a red integration lane here may mean nothing worse than Docker being off. Converting these to the per-test-skip pattern is tracked separately; see [`docs/testing/FIXTURES.md`](../../../docs/testing/FIXTURES.md).
- Shared helpers and test data live in `test/Altinn.AccessManagement.TestUtils`, which sets `IsTestProject=false` and is a library rather than a runnable test project.
- `test/AccessMgmt.Tests` is by far the largest test project; the API-specific projects cover their own surface.
- `test/Bruno` holds API tests and `test/K6` load tests; neither runs as part of `dotnet test`. Save `.bru` files without a BOM: the Bruno CLI cannot parse one that has a BOM, and a single such file stops the whole post-deployment run in Azure DevOps.
- Every test needs `[UnitTest]` or `[IntegrationTest]` or the run fails. See the root file.

## Known tech debt

Moving the remaining code out of the older `Altinn.AccessManagement.*` projects, starting with delegations ([#3654](https://github.com/Altinn/altinn-auth/issues/3654)). Whether `AppHost` is in use, and how it relates to the Aspire work in [#4051](https://github.com/Altinn/altinn-auth/issues/4051), is also unresolved.
