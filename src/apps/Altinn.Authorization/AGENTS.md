# AGENTS.md — Altinn.Authorization

The Policy Decision Point for Altinn 3. It answers authorization requests by evaluating XACML policies, and serves the access-list, party, policy and role endpoints that surround that decision. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md); this file covers only what is specific to this vertical.

## Commands

```bash
dotnet build src/apps/Altinn.Authorization/Altinn.Authorization.slnx
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.slnx -- --filter-trait "Category=Unit"
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.slnx
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

Deployment overlays live in `manifests/deploy/<environment>` and must match the paths selected by `syncroot/<environment>`, including `tt02` and `prod`. Production's Maskinporten client ID and signing key must come from external configuration. Validate overlays with `bash .github/scripts/validate-manifests.sh`.

Decisions for this vertical are in [`docs/adr/`](docs/adr/README.md). Read [ADR-0001](docs/adr/0001-access-management-as-pip.md) before changing what the PDP fetches from Access Management, its cache, or what it trusts in the answer, and [ADR-0002](docs/adr/0002-audit-events-through-a-queue.md) before changing which decisions are audit-logged or how.

## Landmines

_Nothing recorded yet._ This section is the reason the file exists, and it can only be written by people who know why the code is the way it is. If you have ever said "don't change that, it looks wrong but it isn't" about this vertical, write it here. See [#4078](https://github.com/Altinn/altinn-auth/issues/4078).

## Test gotchas

- **No test here needs a container runtime.** `AuthorizationApiFixture` boots the host with mock repositories and clients; [#4311](https://github.com/Altinn/altinn-auth/pull/4311) removed the PDP's database and the `AuthorizationDbFixture` that tested it. The fixture is documented in [`docs/testing/FIXTURES.md`](../../../docs/testing/FIXTURES.md).
- Tests are split into `Unit/` and `Integration/`, and every test needs `[UnitTest]` or `[IntegrationTest]` or the run fails.
