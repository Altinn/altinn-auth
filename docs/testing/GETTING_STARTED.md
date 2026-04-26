# Getting Started

## Prerequisites

| Tool | Version | Why |
|---|---|---|
| .NET SDK | 9.0.x | All projects target `net9.0` |
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
Podman's socket. See [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/12_AccessManagement_Coverage_Baseline_Success.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/12_AccessManagement_Coverage_Baseline_Success.md)
for the exact configuration that's known to work.

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

xUnit v3 uses Microsoft Testing Platform (MTP). Filter with `--filter`:

```
dotnet test --filter "FullyQualifiedName~MaskinportenConsumersController"
dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"
```

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
| `No test is available in <dll>` | MTP routing regressed for that project | Ensure the `.csproj` has `<TargetFramework>net9.0</TargetFramework>` (singular). See [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/37_CI_MTP_Routing_TargetFramework_Clear.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/37_CI_MTP_Routing_TargetFramework_Clear.md). |
| Tests hang on Postgres container start | Stale containers from a previous run | `docker ps -a` → remove any `testcontainers`-prefixed containers |
| `JsonReaderException: Unexpected character '<'` | Client's default `Accept` header is `application/xml`; server returned XML, test tried to parse it as JSON | Remove the default `Accept` header or override per-request. See [`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/40_CI_First_Green_Run_Hardening.md). |

## Next: [TEST_PROJECTS.md](TEST_PROJECTS.md)
