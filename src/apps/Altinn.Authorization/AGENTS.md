# AGENTS.md — Altinn.Authorization

The Policy Decision Point for Altinn 3. It answers authorization requests by evaluating XACML policies, and serves the access-list, party, policy and role endpoints that surround that decision. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md); this file covers only what is specific to this vertical.

## Commands

```bash
dotnet build src/apps/Altinn.Authorization/Altinn.Authorization.sln
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.sln -- --filter-trait "Category=Unit"
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.sln
dotnet run --project src/apps/Altinn.Authorization/src/Altinn.Authorization   # http://localhost:5050/authorization/api/v1
```

## Where things live

One source project and one test project, which makes this the simplest vertical in the repository.

The XACML roles map onto files, and knowing that mapping is most of what it takes to find your way:

| Role | Where |
| --- | --- |
| Decision point, the endpoint that answers | `Controllers/DecisionController.cs` |
| Policy retrieval | `Services/Implementation/PolicyRetrievalPoint.cs` |
| Policy information, the attributes a decision needs | `Services/Implementation/PolicyInformationPoint.cs`, with `ContextHandler` and `DelegationContextHandler` building the request context |
| Policy administration | `Services/Implementation/PolicyAdministrationPoint.cs` |
| Evaluation engine | `Altinn.Authorization.ABAC`, referenced from `src/pkgs` as a project |
| Enforcement, published to consumers as `Altinn.Common.PEP`, and also guarding this app's own endpoints (`ClaimAccessHandler`, `ScopeAccessHandler` in `Program.cs`) | `Altinn.Authorization.PEP`, referenced from `src/pkgs` as a project |

Outbound calls to other services go through wrappers in `Services/Implementation` (`AccessManagementWrapper`, `ProfileWrapper`, `OedRoleAssignmentWrapper`), not directly from controllers.

## Landmines

_Nothing recorded yet._ This section is the reason the file exists, and it can only be written by people who know why the code is the way it is. If you have ever said "don't change that, it looks wrong but it isn't" about this vertical, write it here. See [#4078](https://github.com/Altinn/altinn-auth/issues/4078).

## Test gotchas

- **Two fixtures, opposite purposes.** `AuthorizationApiFixture` boots the host with `PostgreSQLSettings:EnableDBConnection=false` and mock repositories, so the host never runs migrations, even if `EnableDBConnection` is turned on in configuration. Both `appsettings.json` files already set it to `false`; the override is a guard. `AuthorizationDbFixture` is for tests that need a real database.
- **`AuthorizationDbFixture` does not start a container per test class.** It uses the shared `PostgresTestEngine`: migrations are applied once into a template database, and each test class gets a fast `CREATE DATABASE ... WITH TEMPLATE` clone. Treat it as cheap, and do not add container management around it.
- **Skipping without a container runtime is not automatic.** `PostgresTestEngine` records a `SkipReason` rather than throwing, and `AuthorizationDbFixture` exposes it so tests can call `Assert.SkipWhen(...)`. A test that needs the database and does not check it will fail rather than skip. Both fixtures are documented in [`docs/testing/FIXTURES.md`](../../../docs/testing/FIXTURES.md).
- Tests are split into `Unit/` and `Integration/`, and every test needs `[UnitTest]` or `[IntegrationTest]` or the run fails.
