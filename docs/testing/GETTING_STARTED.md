# Getting Started

## Prerequisites

| Tool | Version | Why |
|---|---|---|
| .NET SDK | 10.0.x (plus 9.0.x for a full build) | Test projects target `net10.0`; the `pkg` projects also multi-target `net9.0`, so building the whole solution needs the 9.0 SDK too |
| Docker **or** Podman | Any recent version | Integration tests spin up real PostgreSQL via [Testcontainers](https://dotnet.testcontainers.org/) |
| PowerShell | 7+ (`pwsh`) | Coverage scripts under `eng/testing/` |

### Verifying the container runtime

```
docker ps              # or: podman ps
```

If neither is available, integration tests will **skip gracefully** with
`Assert.Skip("Docker/Podman not available")` rather than failing. Unit tests
(the vast majority of the suite) don't need a container runtime.

### Podman on Windows / macOS

Testcontainers requires a Docker-compatible socket. If you use Podman, make sure
`podman machine` is running and the `DOCKER_HOST` environment variable points at
Podman's socket.

## Running the tests

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
