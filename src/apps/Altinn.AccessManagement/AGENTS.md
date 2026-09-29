# AGENTS.md — Altinn.AccessManagement

Administration of rights and delegations: who may act on behalf of whom, for which resource. Thirteen source projects and eight test projects, so orienting yourself costs more here than anywhere else in the repository. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md).

## Commands

```bash
dotnet build src/apps/Altinn.AccessManagement/Altinn.AccessManagement.sln
dotnet test src/apps/Altinn.AccessManagement/Altinn.AccessManagement.sln -- --filter-trait "Category=Unit"
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

## Landmines

- **There are two naming families, and both are live.** `Altinn.AccessManagement.Core` alongside `Altinn.AccessMgmt.Core`, and `Altinn.AccessManagement.Persistence` alongside `Altinn.AccessMgmt.PersistenceEF`. The host project references **both** persistence projects (`Altinn.AccessManagement.csproj`, lines 46 and 47). Before adding a repository, a query or an entity, find out which family the surrounding code belongs to rather than assuming the one you found first is the current one. **Which family new code belongs in is not yet written down; ask the team and record the answer here.** See [#4078](https://github.com/Altinn/altinn-auth/issues/4078).
- **One test project's folder and project names differ.** The folder is `test/AccessMgmt.Tests` and the project inside it is `Altinn.AccessManagement.Tests.csproj`. The seven other test projects match. So `dotnet test .../test/Altinn.AccessManagement.Tests` fails and `dotnet test .../test/AccessMgmt.Tests` works.

_More belongs here._ This is the section that pays for itself, and it can only come from people who know why the code is the way it is.

## Test gotchas

- Shared helpers and test data live in `test/Altinn.AccessManagement.TestUtils`, not in each test project.
- `test/AccessMgmt.Tests` is by far the largest test project; the API-specific projects cover their own surface.
- `test/Bruno` holds API tests and `test/K6` load tests; neither runs as part of `dotnet test`.
- Every test needs `[UnitTest]` or `[IntegrationTest]` or the run fails, and integration tests skip without a container runtime. See the root file.

## Known tech debt

The two naming families above are the open architectural question. Whether `AppHost` is in use, and how it relates to the Aspire work in [#4051](https://github.com/Altinn/altinn-auth/issues/4051), is also unresolved.
