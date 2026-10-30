# AGENTS.md

Guidance for AI agents, and a useful first page for anyone new to this repository. Verticals have their own `AGENTS.md`, and the guidance closest to the code wins. The contract behind this file is [ADR-0002](docs/adr/0002-tool-neutral-agent-contract.md).

## What this is

Authorization and access management for Altinn 3: the Policy Decision Point and Policy Enforcement Point, the Access Management services that administer rights and delegations, and the Resource Registry. This is becoming the monorepo for the whole domain; register and the Access Management frontend are still being moved in ([#4049](https://github.com/Altinn/altinn-auth/issues/4049)).

## Commands

```bash
dotnet build Altinn.Authorization.slnx                 # the whole repository
dotnet build src/apps/Altinn.Authorization/Altinn.Authorization.slnx   # one vertical
dotnet test                                            # every test project
dotnet test src/apps/Altinn.Authorization/Altinn.Authorization.slnx -- --filter-trait "Category=Unit"
dotnet test src/apps/Altinn.ResourceRegistry/Altinn.ResourceRegistry.slnx   # xUnit v2, see Test gotchas
repoctl vertical test app:Altinn.Authorization          # what CI runs: every test in the vertical
repoctl check                                          # dotnet tool install -g Altinn.Authorization.RepoCtl.Cli
pwsh eng/testing/run-coverage.ps1                      # coverage, installs dotnet-coverage if missing
```

`.justfile` holds the quality gates and local-development helpers: `just check <vertical>` is the fast gate, `just check-full <vertical>` runs what CI runs. Recipes need PowerShell 7.

## Critical workflow rules

- **Branch per change, never commit to `main`.** Name it `type/<issue>_<slug>`.
- **Commits and PR titles use Conventional Commits**, with a lowercase kebab-case scope for a component where there is one, e.g. `fix(authorized-parties): correct delegation lookup`. Issues go in the body or trailers (`Refs: #4044`), never the title. `main` is squash-only: preserve all commit trailers. See [conventions](CONTRIBUTING.md#branches-commits-and-pull-requests).
- **CI runs `repoctl check`:** formatting, generated `.slnx` files and release configuration must be current (`just update-sln-files`). [Promotion](.github/actions/deploy-apps/README.md) serves CI and releases.
- **A green local build or `dotnet test` is not proof, and a red one is not always a defect.** CI builds with `-warnaserror`, and without a container runtime, integration tests that need a database fail in setup. CI is the gate.
- **Update affected documentation and agent guidance in the same PR.** Require ADRs only for significant architectural decisions or lasting trade-offs; see [the threshold](CONTRIBUTING.md#when-an-adr-is-required). Validate the [guidance contract](docs/adr/0002-tool-neutral-agent-contract.md) with `node .github/scripts/agents-docs-validate.mjs`.
- **Issues: a type and one or more `area/*`** ([guide](CONTRIBUTING.md#issues-and-labels)).
- **Issues, comments, docs and PR descriptions start with a TL;DR** of at most five bullets (bugs: what happened). Text drafted with a tool says so near the top, and says the reader is unspecified until a person confirms they have read it.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) is the working agreement, including the rules for AI assistance, and [`docs/ai/data-policy.md`](docs/ai/data-policy.md) says what may go into a tool at all. Read both before using an assistant here.

**Write a PR description by these rules, with or without a GitHub template.**

- The TL;DR states the problem, change and purpose. Close issues primarily through the PR description: `Resolves: #<issue>`, only when the PR completes it.
- State what you verified: the commands or checks, their results, and what you did not run or could not test locally, with the reason. Distinguish skipped tests from passed ones. Never claim a check you did not run.
- Say what an assistant did and how you verified its output.
- Keep it proportional, with longer background in a `<details>` block. Check the description against the final diff before publishing, and correct it if the scope moved.

## Where things live

Code sits in verticals, each with its own `.slnx`, `src/` and `conf.json`. CI finds them in the four folders below with `repoctl ci find-verticals`, so adding one needs no workflow change.

| Path | Contents |
| --- | --- |
| `src/apps` | Deployable services: [`Altinn.Authorization`](src/apps/Altinn.Authorization/AGENTS.md) (PDP), [`Altinn.AccessManagement`](src/apps/Altinn.AccessManagement/AGENTS.md), [`Altinn.ResourceRegistry`](src/apps/Altinn.ResourceRegistry/AGENTS.md), [`Altinn.Authentication`](src/apps/Altinn.Authentication/AGENTS.md), [`Altinn.Mockporten`](src/apps/Altinn.Mockporten/AGENTS.md), `Altinn.Register` |
| `src/libs` | Shared libraries: `Api.Contracts`, `Host`, `Integration`, `Testing` |
| `src/pkgs` | Published NuGet packages: `Altinn.Authorization.ABAC`, `Altinn.Authorization.PEP` (ships as `Altinn.Common.PEP`) and `JWTCookieAuthentication` |
| `src/tools` | `Altinn.Authorization.Cli` and the [`Altinn.AccessMgmt.FFB`](src/tools/Altinn.AccessMgmt.FFB/AGENTS.md) admin tool, which has its own guidance |
| `docs/testing` | How the test suite is organised, fixtures, mocks, naming |
| `docs/adr` | Architecture decision records, cross-cutting; each vertical keeps its own in its `docs/adr/` |
| `docs/ai` | The [data policy](docs/ai/data-policy.md) for AI tools, and the background for the working agreement |
| `eng/testing` | Coverage scripts, thresholds, the test-category guard |
| `infra` | Infrastructure as code |
| `.agents/skills` | [Task skills](.agents/skills/README.md); `.claude/skills` points here |

Before changing a vertical, read its `AGENTS.md`.

## Landmines

Do not "fix" these without understanding them.

- **`src/apps/Altinn.Register` is a placeholder**, a single `Program.cs`. The real Register arrives with [#4056](https://github.com/Altinn/altinn-auth/issues/4056). Do not build on it or wire anything to it.
- **Imported verticals are deliberately islands** with their own `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props` and `stylecop.json`. That let them be imported with their history intact. Do not merge them into the shared build files.
- **Markdown is LF**, enforced by `.gitattributes`. Do not reformat line endings.
- **Kustomize's `envs` generator reads manifest `.env` files literally.** Write `KEY=value` with no spaces around `=` and no quotes, `true` and `false` included. Spaces and `=` inside a value, as in a connection string, are kept.

## Test gotchas

- **In an xUnit v3 vertical, every test needs a category.** Mark the class or method `[UnitTest]` or `[IntegrationTest]` from `Altinn.Authorization.Testing`. The unit lane selects by that trait, and `TestCategoryGuard`, linked into those test assemblies, fails the run on an uncategorised test. CI runs every test in a vertical in one go.
- **Resource Registry is the exception.** It is an xUnit v2 island whose tests carry no category, so a repository-wide `dotnet test -- --filter-trait "Category=Unit"` also runs its integration tests, which start a real PostgreSQL. Run the unit lane per vertical, and that vertical through its own solution; [its `AGENTS.md`](src/apps/Altinn.ResourceRegistry/AGENTS.md) explains why.
- **Without Docker or Podman**, Access Management's integration tests fail in fixture setup and Resource Registry's fail starting PostgreSQL. Authorization's need no container.
- Start from [`docs/testing/README.md`](docs/testing/README.md) for fixtures, mocks and the naming convention.

## Known tech debt

Landmines per vertical [#4078](https://github.com/Altinn/altinn-auth/issues/4078), and `just` build and test recipes [#4086](https://github.com/Altinn/altinn-auth/issues/4086).
