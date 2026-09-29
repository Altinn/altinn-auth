# AGENTS.md — Altinn.Authorization

The Policy Decision Point for Altinn 3. It answers authorization requests by evaluating XACML policies, and serves the access-list, party, policy and role endpoints that surround that decision. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md); this file covers only what is specific to this vertical.

## Commands

```bash
dotnet build src/apps/Altinn.Authorization/Altinn.Authorization.sln
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.sln -- --filter-trait "Category=Unit"
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.sln
dotnet run --project src/apps/Altinn.Authorization/src/Altinn.Authorization   # http://localhost:5030/api/v1
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
| Enforcement, used by consumers rather than here | `Altinn.Authorization.PEP`, published as `Altinn.Common.PEP` |

Outbound calls to other services go through wrappers in `Services/Implementation` (`AccessManagementWrapper`, `ProfileWrapper`, `OedRoleAssignmentWrapper`), not directly from controllers.

## Landmines

_Nothing recorded yet._ This section is the reason the file exists, and it can only be written by people who know why the code is the way it is. If you have ever said "don't change that, it looks wrong but it isn't" about this vertical, write it here. See [#4078](https://github.com/Altinn/altinn-auth/issues/4078).

## Test gotchas

- **Two fixtures, opposite purposes.** `AuthorizationApiFixture` boots the host with `PostgreSQLSettings:EnableDBConnection=false` and mock repositories, because otherwise every fixture runs EF migrations against the configured Postgres, which is unnecessary and racy when several test classes share one database. `AuthorizationDbFixture` is the one that manages its own container, for tests that need a real database. The reasoning is in the fixture itself, and both are documented in [`docs/testing/FIXTURES.md`](../../../docs/testing/FIXTURES.md).
- Integration tests skip rather than fail without a container runtime, so a green local run can mean very little. See the root file.
- Tests are split into `Unit/` and `Integration/`, and every test needs `[UnitTest]` or `[IntegrationTest]` or the run fails.
