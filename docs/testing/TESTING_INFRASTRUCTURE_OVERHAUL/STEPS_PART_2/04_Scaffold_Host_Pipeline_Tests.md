---
step: 4
title: Scaffold Altinn.Authorization.Host.Pipeline.Tests project
phase: A
status: complete
linkedIssues:
  feature: 2946
  task: 2947
coverageDelta:
  # Production assembly coverage unchanged — populating real Pipeline
  # tests is Phase D.1, deferred. The new test project itself has one
  # smoke test that exercises a single `PipelineMessage<T>` ctor.
  Altinn.Authorization.Host.Pipeline: { line: 0.0, branch: 0.0, note: scaffold_only_phase_d1_pending }
verifiedTests: 1
touchedFiles: 6
auditIds: [C3']
---

# Step 4 — Scaffold Altinn.Authorization.Host.Pipeline.Tests project

## Goal

Resolve audit finding **C3'**'s scaffold half (plan item **A.5**):
create a new `Altinn.Authorization.Host.Pipeline.Tests` test project so
that Phase D.1 (populating it with real coverage tests) has a wired
project to add files to. The production assembly
`Altinn.Authorization.Host.Pipeline` is ~1.4 k LOC at 0 % coverage and
is the largest untested production library in the repo.

Per the audit decision, this step is **scaffold only** — no real
Pipeline coverage tests yet. To avoid recreating the **C1'**
empty-test-project antipattern that Step 3 just fixed, the scaffold
includes one trivial smoke test against the public
`PipelineMessage<T>` record so the xUnit v3 runner discovers ≥ 1 test
in the assembly.

## What changed

### New files

- `src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/Altinn.Authorization.Host.Pipeline.Tests.csproj`
  — mirrors the existing `Lease.Tests` csproj exactly: empty
  `<TargetFramework></TargetFramework>` (Part 1 Step 37 MTP-routing
  trick) + `<TargetFrameworks>net9.0</TargetFrameworks>` +
  ProjectReference to `..\..\src\Altinn.Authorization.Host.Pipeline\Altinn.Authorization.Host.Pipeline.csproj`
  + `xunit.runner.json` content. All other wiring (xUnit v3,
  FluentAssertions, coverlet, MTP `OutputType=Exe`,
  `TestingPlatformDotnetTestSupport=true`) is inherited from the
  parent `test/Directory.Build.props` and the central
  `src/Directory.Build.targets`.
- `src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/xunit.runner.json`
  — copied verbatim from `Lease.Tests` (parallelism + method-display
  options matching the rest of the host vertical).
- `src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/PipelineMessageSmokeTest.cs`
  — single `[Fact]` constructing a `PipelineMessage<int>(42, ActivityContext, CTS) { Sequence = 7 }`
  and asserting that the record exposes the four init values it was
  given. Exercises one of the simplest public types in the Pipeline
  assembly with no external dependencies. Doc-comment explicitly
  flags the smoke-test intent and points at C1'/Phase D.1 so future
  readers don't mistake it for the start of a real coverage suite.

### Solution files

```
dotnet sln Altinn.Authorization.sln add \
  src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/Altinn.Authorization.Host.Pipeline.Tests.csproj
dotnet sln src/libs/Altinn.Authorization.Host/Altinn.Authorization.Host.sln add \
  src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/Altinn.Authorization.Host.Pipeline.Tests.csproj
```

Both root + per-package solution files updated. Same convention as
the existing `Lease.Tests` entry.

### Documentation

- [`docs/testing/TEST_PROJECTS.md`](../../TEST_PROJECTS.md) `### lib: Host`
  table — added a `Altinn.Authorization.Host.Pipeline.Tests` row
  describing what it covers (Pipeline hosted services, builders,
  segment/sink/source services) and flagging it as scaffold-only with
  a pointer to Phase D.1. Also fixed the stale `STEPS_PART_1` link on
  the Lease row → now points to `STEPS_PART_2/INDEX.md#blocked-items`.
- Audit doc [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
  §2 — C3' scaffold half marked done (production-assembly coverage
  gap remains for Phase D.1); §4 Phase A — A.5 marked done.
- Step log row + Recommended Next Steps refresh in
  [`INDEX.md`](INDEX.md).

## Verification

### Build clean

```
dotnet build src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/Altinn.Authorization.Host.Pipeline.Tests.csproj \
  --configuration Release --verbosity minimal
```

Result: 0 warnings, 0 errors. The new project compiles for `net9.0`
with the production `Altinn.Authorization.Host.Pipeline` assembly
restored as a transitive dependency. Same build pattern that
`run-coverage.ps1` exercises in CI.

### Smoke test runs and passes

```
dotnet src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/bin/Release/net9.0/Altinn.Authorization.Host.Pipeline.Tests.dll
```

Output:

```
xUnit.net v3 In-Process Runner v2.0.3+216a74a292 (64-bit .NET 9.0.15)
  Discovering: Altinn.Authorization.Host.Pipeline.Tests
  Discovered:  Altinn.Authorization.Host.Pipeline.Tests
  Starting:    Altinn.Authorization.Host.Pipeline.Tests
  Finished:    Altinn.Authorization.Host.Pipeline.Tests
=== TEST EXECUTION SUMMARY ===
   Altinn.Authorization.Host.Pipeline.Tests  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0, Time: 1.488s
```

Confirms:

- xUnit v3 + MTP routing inherited correctly from the Host vertical's
  `test/Directory.Build.props`.
- `OutputType=Exe` + `runtimeconfig.json` emission from the central
  `Directory.Build.targets` (production assembly DLL is loadable
  in-process).
- Discovery finds exactly the one smoke test (no zero-discovery
  recurrence of C1').

### Future `run-coverage.ps1` impact

When `eng/testing/run-coverage.ps1` next runs, it will discover and
execute this project alongside the other 10 (test-project count
10 → 11). The smoke test contributes ~negligible coverage to the
production `Altinn.Authorization.Host.Pipeline` assembly (just a few
lines of the `PipelineMessage<T>` record auto-generated equality
plumbing). The 0 % → ε bump is expected and not yet meaningful;
real Phase D.1 coverage will move it materially.

## Deferred / follow-up

- **Phase D.1** — populate the project with real coverage tests for
  `PipelineHostedService` (345 LOC), the three `Pipeline*Service`
  classes (~492 LOC combined), the three `Pipeline*Builder` classes
  (~126 LOC combined), `PipelineDescriptor`, `PipelineGroup`,
  `PipelineRegistry`, `PipelineTelemetry`. Largest untested unit of
  production code in the repo; biggest single coverage win available.
- **Test-project count** in
  [`PART_2.md` §1.1](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#11-test-project-inventory)
  is now stale (audit recorded 11; Step 3 took us to 10; this step
  takes us back to 11 with a *different* set of projects). Will be
  refreshed alongside the §1.4 baseline at T1 closing.

## Blocked-items sweep

No items unblocked. Refresh `Last re-checked = step 4` on the three
Blocked Items rows in [`INDEX.md`](INDEX.md#blocked-items).

## Obsolete-docs sweep

None. No prior step doc covered this work.
