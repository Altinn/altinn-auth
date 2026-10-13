# Getting Started

## Prerequisites

| Tool | Version | Why |
|---|---|---|
| .NET SDK | 10.0.x (plus 9.0.x for a full build) | Test projects target `net10.0`; the `pkg` projects also multi-target `net9.0`, so building the whole solution needs the 9.0 SDK too |
| Docker **or** Podman | Any recent version | Integration tests spin up real PostgreSQL via [Testcontainers](https://dotnet.testcontainers.org/) |
| PowerShell | 7+ (`pwsh`) | Coverage scripts under `eng/testing/`, and the `just` recipes |
| RepoCtl | The version in `.github/actions/init/action.yml` | CI tests and checks the repository with it. Install with `dotnet tool install --global Altinn.Authorization.RepoCtl.Cli` |

### Verifying the container runtime

```
docker ps              # or: podman ps
```

If neither is available, integration tests that need a database fail: Access
Management's in fixture setup, Resource Registry's when PostgreSQL does not
start. Unit tests (the vast majority of the suite) don't need a container
runtime.

### Podman on Windows / macOS

Testcontainers requires a Docker-compatible socket. If you use Podman, make sure
`podman machine` is running and the `DOCKER_HOST` environment variable points at
Podman's socket. On Windows, Testcontainers falls back to the
`\\.\pipe\docker_engine` pipe when `DOCKER_HOST` is unset, and Podman serves
that pipe only if nothing else holds it. `just check-full` checks for one or the
other before it builds.

## Running the tests

### The gate before a pull request

```
just check src/apps/Altinn.Authorization
```

Builds the vertical with `-warnaserror`, as CI does, and runs its unit tests,
with no container runtime needed.
It is a fast early signal, not what CI runs. `just check-full <vertical>` is:
it builds, then runs every test in the vertical in one go through
`repoctl vertical test`, so it needs a container runtime. `just repo-check`
runs `repoctl check`, the repository-wide check CI also runs: `dotnet format`,
generated `.slnx` files and the release configuration.

A working tree checked out before #4103 (2026-10-09) can still hold CRLF
files, because Git applies the new `.gitattributes` only to files it writes
again; `git ls-files --eol` shows them as `w/crlf`, and `dotnet format` in
`repo-check` flags every line in them. A fresh clone or `git worktree add` is
LF already. In an existing clone with no uncommitted changes,
`git rm -r -q --cached .` followed by `git reset -q --hard` rewrites them. A few whitespace findings in
`Altinn.ResourceRegistry` remain on Windows, because its own `.editorconfig`
sets no `end_of_line`; CI on Linux does not report them.

`Altinn.ResourceRegistry` runs xUnit v2 without categories, so its unit tests
are all of its tests and start PostgreSQL. Recipes need PowerShell 7.

### All tests

```
dotnet test
```

From the repo root. This restores, builds, and runs every `*.Tests` project.

### A single project

```
dotnet test src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests
```

### A single test or class

These projects run on Microsoft Testing Platform (MTP) via xUnit v3, which does
**not** honour the VSTest `--filter "FullyQualifiedName~..."` syntax. To run a
single test or class, build the project and invoke the test assembly directly
with the xUnit v3 query filter — `/assembly/namespace/class/method`, with `*`
wildcards:

```
dotnet build src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests
dotnet src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/bin/Debug/<tfm>/Altinn.AccessManagement.Api.Tests.dll -filter "/*/*/*/MyTestMethod"
```

Replace `<tfm>` with the project's target framework (currently `net10.0`).

Pass `-filter` more than once to select several tests (the filters OR together).
Nested test classes appear as `Outer+Inner` in the class segment, so filtering
by a method-name wildcard (`/*/*/*/MyTestMethod*`) is usually the simplest
selector. The MTP option names (`--filter-method`, `--filter-class`) are not
accepted by the in-process assembly runner — use the `-filter` query form above.

### In Visual Studio / Rider / VS Code

Test Explorer (VS / Rider) and the C# Dev Kit test UI (VS Code) discover the
tests automatically via MTP. No additional configuration needed.

## Running coverage locally

The local helper builds every `*.Tests` project, runs each under
`dotnet-coverage collect` in parallel, writes a Cobertura XML per project to
`TestResults/`, and then checks all thresholds in
[`eng/testing/coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json).

```
pwsh eng/testing/run-coverage.ps1
```

If `dotnet-coverage` isn't installed, the script installs it as a global tool.

See [COVERAGE.md](COVERAGE.md) for the full flow.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `"Docker/Podman not available"` skips | Container runtime not running | Start Docker Desktop / `podman machine start` |
| `No test is available in <dll>` | MTP routing regressed for that project | Ensure the `.csproj` clears the inherited singular `<TargetFramework>` and sets `<TargetFrameworks>net10.0</TargetFrameworks>` (plural) — that is what routes it to MTP. |
| Tests hang on Postgres container start | Stale containers from a previous run | `docker ps -a` → remove any `testcontainers`-prefixed containers |
| `JsonReaderException: Unexpected character '<'` | Client's default `Accept` header is `application/xml`; server returned XML, test tried to parse it as JSON | Remove the default `Accept` header or override per-request. |

## Next: [TEST_PROJECTS.md](TEST_PROJECTS.md)
