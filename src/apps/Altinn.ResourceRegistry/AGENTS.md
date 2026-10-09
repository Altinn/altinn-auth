# AGENTS.md — Altinn.ResourceRegistry

The registry of resources and access lists that authorization decisions are made against: service owners register a resource, attach a policy, and maintain the access lists that go with it. Imported into this repository with its full history on 25 September 2026 ([#4196](https://github.com/Altinn/altinn-auth/pull/4196)) and deliberately kept as a build island. Repo-wide rules are in the root [AGENTS.md](../../../AGENTS.md).

**Read the landmines before running anything here.** This vertical differs from the rest of the repository in ways that will mislead you otherwise.

## Commands

```bash
dotnet build src/apps/Altinn.ResourceRegistry/Altinn.ResourceRegistry.sln
dotnet test src/apps/Altinn.ResourceRegistry/Altinn.ResourceRegistry.sln
dotnet run --project src/apps/Altinn.ResourceRegistry/src/Altinn.ResourceRegistry   # http://localhost:5100, opens Swagger
```

Routes are under `resourceregistry/api/v1/`, except `ResourceV2Controller` at `resourceregistry/api/v2/resource`. Use this vertical's own solution rather than the repository-wide commands, for the reasons below.

## Where things live

Four source projects: `Altinn.ResourceRegistry` (the host and its controllers for resources, resource owners, access lists and access-list memberships, plus a v2 resource controller), `.Core`, `.Persistence` and `.Integration`. Tests are `Altinn.ResourceRegistry.Tests` and `Altinn.ResourceRegistry.Persistence.Tests`, with shared helpers in `Altinn.ResourceRegistry.TestUtils` and API tests under `test/Bruno`.

## Landmines

- **This is an intentional build island.** It carries its own `.editorconfig`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` and `stylecop.json`, rather than inheriting the repository's. That islanding is what made it possible to import the repository with its history intact. Do not merge these into the shared build files without a deliberate decision.
- **It is on xUnit v2 while the rest of the repository is on v3** (`xunit` 2.9.3, run through `xunit.runner.visualstudio` and VSTest rather than the Microsoft Testing Platform). Three consequences follow, and all three have already caused confusion:
  - Its tests carry no `[UnitTest]` or `[IntegrationTest]` marker, because this island does not import `src/Directory.Build.targets`, which is where the markers are linked in. Setting `XUnitVersion` to `v3` here does nothing; xUnit 2 comes from this folder's own `Directory.Build.targets`.
  - `TestCategoryGuard` is not compiled here, so nothing fails when a test has no category.
  - **The repository-wide unit lane does not exclude these tests.** `dotnet test -- --filter-trait "Category=Unit"` run from the root also runs this vertical's integration tests. That was verified: a PostgreSQL test here runs and passes under that filter.
- **Its integration tests start a real database rather than skipping.** `DbFixture` builds and starts a PostgreSQL container through Testcontainers, with no skip path when no runtime is available. Elsewhere in the repository a missing runtime either skips or fails in fixture setup; here it will try to start a container.
- There is no `Version.props`, and `conf.json` carries only a Sonar key, unlike the other app verticals.

## Test gotchas

Run this vertical through its own solution. A repository-wide `dotnet test` with the unit-lane filter will pull these tests in and start a database, which is slow and looks like the unit lane misbehaving when it is really this vertical having no lanes at all.

## Known tech debt

Whether to bring this vertical onto xUnit v3 and the shared category markers, and when to dissolve the build island now that the import is done, are both open. Until they are decided, everything above stays true.
