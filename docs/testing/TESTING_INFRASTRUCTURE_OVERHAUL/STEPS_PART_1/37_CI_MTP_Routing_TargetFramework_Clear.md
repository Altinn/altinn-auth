# Step 37 — Restore MTP routing by clearing inherited singular `<TargetFramework>` in test csprojs

## Goal

Fix CI `Build and Test` failing with **"No test is available … Make sure that
test discoverer & executors are registered"** across every AccessManagement,
Authorization, and lib test assembly after commit `20ae747b`
("ci(test): fix CI Build-and-Test + VS Test Explorer discovery", step 35
follow-up) silently regressed Microsoft Testing Platform (MTP) routing.

## Root cause

Step 35 discovered that for xUnit v3 test projects, `dotnet test` only routes to
MTP when `TargetFramework` (singular) is **empty** and `TargetFrameworks`
(plural) is set. If the singular is non-empty, `dotnet test` silently falls
back to VSTest — which has no discoverer for xUnit v3, hence "No test is
available".

Step 35 handled this in each `test/Directory.Build.props` by setting both
`<TargetFramework></TargetFramework>` and `<TargetFrameworks>net9.0</TargetFrameworks>`.

Commit `20ae747b` then moved `<TargetFrameworks>net9.0</TargetFrameworks>`
inline into each leaf test csproj (to make VS Test Explorer happy) and removed
the Directory.Build.props entries entirely — but it did **not** also copy the
`<TargetFramework></TargetFramework>` clearer inline. So every apps/libs test
project re-inherited `<TargetFramework>net9.0</TargetFramework>` from
`src/Directory.Build.props`, and MTP routing stopped working.

The pkgs tests (PEP, ABAC) kept working because `src/pkgs/Directory.Build.props`
clears `<TargetFramework></TargetFramework>` for its entire subtree
(production packages multi-target `net8.0;net9.0`). `src/apps/Directory.Build.props`
and `src/libs/Directory.Build.props` don't do the equivalent, because their
production projects want the singular `net9.0`.

Repro (local, before fix):

```text
> dotnet msbuild …/AccessMgmt.Tests.csproj -getProperty:TargetFramework,TargetFrameworks,TestingPlatformDotnetTestSupport
{
  "TargetFramework": "net9.0",         ← non-empty: kills MTP routing
  "TargetFrameworks": "net9.0",
  "TestingPlatformDotnetTestSupport": "true"
}

> dotnet test …/AccessMgmt.Tests.csproj -c Release --no-build
Test run for …/AccessMgmt.Tests.dll (.NETCoreApp,Version=v9.0)
VSTest version 17.14.1 (x64)
…
No test is available in …/AccessMgmt.Tests.dll. Make sure that test discoverer
& executors are registered and platform & framework version settings are
appropriate and try again.
```

## Change

Added `<TargetFramework></TargetFramework>` next to the existing
`<TargetFrameworks>net9.0</TargetFrameworks>` in the 9 affected leaf test
csprojs (the pkgs tests, PEP + ABAC, are already fine and don't need editing):

- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/Altinn.AccessManagement.Api.Tests.csproj`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Enduser.Api.Tests/Altinn.AccessManagement.Enduser.Api.Tests.csproj`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.ServiceOwner.Api.Tests/Altinn.AccessManagement.ServiceOwner.Api.Tests.csproj`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.Core.Tests/Altinn.AccessMgmt.Core.Tests.csproj`
- `src/apps/Altinn.AccessManagement/test/Altinn.AccessMgmt.PersistenceEF.Tests/Altinn.AccessMgmt.PersistenceEF.Tests.csproj`
- `src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Altinn.Authorization.Tests.csproj`
- `src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Lease.Tests/Altinn.Authorization.Host.Lease.Tests.csproj`
- `src/libs/Altinn.Authorization.Integration/test/Altinn.Authorization.Integration.Tests/Altinn.Authorization.Integration.Tests.csproj`

A short comment next to the added line documents **why** the explicit clear is
required (so the next person poking at the csproj doesn't "clean it up" and
regress MTP again).

No changes to `.github/workflows/*`, `Directory.Build.props`,
`Directory.Build.targets`, `Directory.Packages.props`, `run-coverage.ps1`, or
any test source files.

## Verification

`dotnet msbuild` now reports an empty singular `TargetFramework` on every
previously-broken project, matching the PEP/ABAC shape:

```text
> dotnet msbuild …/AccessMgmt.Tests.csproj -getProperty:TargetFramework,TargetFrameworks,TestingPlatformDotnetTestSupport
{
  "TargetFramework": "",
  "TargetFrameworks": "net9.0",
  "TestingPlatformDotnetTestSupport": "true"
}
```

`dotnet test --no-build` now uses the MTP output style (`Run tests: …dll` +
`Passed! - Failed: 0, Passed: N …`) instead of the VSTest banner, e.g.:

```text
  Run tests: '…/AccessMgmt.Tests.dll' [net9.0|x64]
  Passed! - Failed: 0, Passed: 1050, Skipped: 0, Total: 1050, Duration: 2m 23s - AccessMgmt.Tests.dll (net9.0|x64)
  Tests succeeded: '…/AccessMgmt.Tests.dll' [net9.0|x64]
```

Full CI run is expected to restore the green state that existed after step 36.

## Not in scope

- Flaky/latent failures tracked under 6.7e / 6.7f in INDEX.md.
- Any further Directory.Build.props refactor to push the `TargetFramework`
  clearer back up to a shared file (intentionally left inline so it stays next
  to the `<TargetFrameworks>` it pairs with, matching the step-20ae747b
  rationale that VS Test Explorer is happier when TFM declarations live in the
  csproj itself).
