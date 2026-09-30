# AGENTS.md

Guidance for AI agents, and a useful first page for anyone new to this repository. It is the top of a hierarchy: each vertical gets its own `AGENTS.md` with the detail, and the guidance closest to the code wins. The contract behind this file is [ADR-0002](docs/adr/0002-tool-neutral-agent-contract.md).

## What this is

Authorization and access management for Altinn 3: the Policy Decision Point and Policy Enforcement Point, the Access Management services that administer rights and delegations, and the Resource Registry. This is becoming the monorepo for the whole domain; register, authentication and the Access Management frontend are still being moved in ([#4049](https://github.com/Altinn/altinn-auth/issues/4049)).

## Commands

```bash
dotnet build Altinn.Authorization.sln                 # the whole repository
dotnet build src/apps/Altinn.Authorization/Altinn.Authorization.sln   # one vertical
dotnet test                                            # every test project
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.sln -- --filter-trait "Category=Unit"
dotnet test src/apps/Altinn.ResourceRegistry/Altinn.ResourceRegistry.sln   # xUnit v2, see Test gotchas
dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests
pwsh eng/testing/run-coverage.ps1                      # coverage, installs dotnet-coverage if missing
```

`.justfile` holds local-development helpers, not build targets: `just dev` starts the containers (Podman on Windows, Docker elsewhere) and `just db-cred` prints database credentials. Build and test recipes are [#4086](https://github.com/Altinn/altinn-auth/issues/4086).

## Critical workflow rules

- **Branch per change, never commit to `main`.** Name it `type/<issue>_<slug>`.
- **The PR title is a Conventional Commit with the issue number**, for example `fix(#4044): ...`. `main` is squash-only with linear history, and the release automation reads the squash title. A single-commit PR currently squashes under the *commit* title, so check the title in the merge dialog ([#4095](https://github.com/Altinn/altinn-auth/issues/4095)).
- **A green local `dotnet test` is not proof.** Integration tests skip when no container runtime is running, so the suite can pass having run almost nothing. CI is the gate.
- **Documentation, `AGENTS.md` and ADRs change in the same PR** as the behaviour they describe. CI checks them against [ADR-0002](docs/adr/0002-tool-neutral-agent-contract.md); run it locally with `node .github/scripts/agents-docs-validate.mjs`.
- **Issues, comments, docs and PR descriptions start with a TL;DR** of at most five bullets. Text drafted with a tool says so near the top, and says the reader is unspecified until a person confirms they have read it.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) is the working agreement, including the rules for AI assistance, and [`docs/ai/data-policy.md`](docs/ai/data-policy.md) says what may go into a tool at all. Read both before using an assistant here.

**Write a PR description by these rules, with or without a GitHub template.** This repository keeps no local issue templates, so the organisation's shared ones stay in the picker.

- The TL;DR gives the problem, the change, and why it matters. Reference the issue, and use `Closes #<issue>` only when the PR completes it.
- State what you verified: the commands or checks, their results, and what you did not run or could not test locally, with the reason. Distinguish skipped tests from passed ones. Never claim a check you did not run.
- Say what an assistant did and how you verified its output.
- Keep it proportional, with longer background in a `<details>` block. Check the description against the final diff before publishing, and correct it if the scope moved.

## Where things live

Code sits in verticals. A vertical always has its own `.sln`, `src/` and `conf.json`; `test/`, `Version.props`, `Dockerfile` and `infra/` appear where they are needed, and the contents of `conf.json` differ per vertical (dependencies, Sonar key, database, infrastructure). CI discovers verticals by globbing the four folders below and reading each `conf.json`, so adding one needs no workflow change.

| Path | Contents |
| --- | --- |
| `src/apps` | Deployable services: `Altinn.Authorization` (PDP/PEP), `Altinn.AccessManagement`, `Altinn.ResourceRegistry`, `Altinn.Register` |
| `src/libs` | Shared libraries: `Api.Contracts`, `Host`, `Integration`, `Testing` |
| `src/pkgs` | Published NuGet packages: `Altinn.Authorization.ABAC`, and `Altinn.Authorization.PEP`, which ships as `Altinn.Common.PEP` |
| `src/tools` | `Altinn.Authorization.Cli` and the [`Altinn.AccessMgmt.FFB`](src/tools/Altinn.AccessMgmt.FFB/AGENTS.md) admin tool, which has its own guidance |
| `docs/testing` | How the test suite is organised, fixtures, mocks, naming |
| `docs/adr` | Architecture decision records, cross-cutting |
| `docs/ai` | The [data policy](docs/ai/data-policy.md) for AI tools, and the background for the working agreement |
| `eng/testing` | Coverage scripts, thresholds, the test-category guard |
| `infra` | Infrastructure as code |

Per-vertical `AGENTS.md` files arrive with [#4078](https://github.com/Altinn/altinn-auth/issues/4078); until then a vertical's `README.md` is the best starting point where one exists.

## Landmines

Do not "fix" these without understanding them.

- **`src/apps/Altinn.Register` is a placeholder**, a single `Program.cs`. The real Register arrives with [#4056](https://github.com/Altinn/altinn-auth/issues/4056). Do not build on it or wire anything to it.
- **`src/apps/Altinn.ResourceRegistry` is deliberately an island** with its own `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props` and `stylecop.json`. That is what let it be imported with its history intact. Do not merge it into the shared build files.
- **Markdown is LF**, enforced by `.gitattributes`. Do not reformat line endings.

## Test gotchas

- **In an xUnit v3 vertical, every test needs a category.** Mark the class or method `[UnitTest]` or `[IntegrationTest]` from `Altinn.Authorization.Testing`. CI selects its lanes by that trait, so an uncategorised test would run in neither; `TestCategoryGuard` is linked into those test assemblies and fails the run instead of letting it disappear. The markers and the guard are compiled only when `XUnitVersion` is `v3` (see `src/Directory.Build.targets`).
- **Resource Registry is the exception, and it is not a small one.** It is an xUnit v2 island: it does not link the category markers or the guard, its tests carry no category, and the trait filter does not exclude them. A repository-wide `dotnet test -- --filter-trait "Category=Unit"` therefore also runs its integration tests, which start a real PostgreSQL rather than skipping. Run the unit lane per vertical instead, and run that vertical through its own solution.
- Outside that vertical, integration tests skip rather than fail when Docker or Podman is unavailable. Start a container runtime before trusting a green run.
- Start from [`docs/testing/README.md`](docs/testing/README.md) for fixtures, mocks and the naming convention.

## Known tech debt

Per-vertical guidance [#4078](https://github.com/Altinn/altinn-auth/issues/4078), `just` build and test recipes [#4086](https://github.com/Altinn/altinn-auth/issues/4086), and the AI review bot we answer to [#4085](https://github.com/Altinn/altinn-auth/issues/4085).
