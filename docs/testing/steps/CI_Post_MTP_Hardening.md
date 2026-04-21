# Step 36 — Post‑MTP CI hardening: test‑library FluentAssertions + cross‑platform coverage exe detection

## Goal

Two follow-up CI failures surfaced after step 35 enabled Microsoft Testing
Platform routing. Both are small fixes in shared build/test infrastructure.

## Issue A — `Altinn.AccessManagement.TestUtils` fails to build

```text
error CS0400: The type or namespace name 'FluentAssertions' could not be found
in the global namespace (are you missing an assembly reference?)
[.../Altinn.AccessManagement.TestUtils.csproj::TargetFramework=net9.0]
```

### Root cause

`src/Directory.Build.targets` emitted a global `<Using Include="FluentAssertions" />`
for anything where `IsTestProject=true` OR `IsTestLibrary=true`, but added the
`FluentAssertions` `PackageReference` only under the `IsTestProject=true` branch.

Before step 35, `Altinn.AccessManagement.TestUtils` had `IsTestLibrary=true` and
also inherited `IsTestProject=true` from `test/Directory.Build.props`, so it
hit the "test project" branch and picked up the package. Step 35 set
`<IsTestProject>false</IsTestProject>` on that csproj so `dotnet test` would
skip it — which also stopped the `FluentAssertions` package from being added,
while the global using remained, producing the CS0400.

### Change

`src/Directory.Build.targets` — moved the `FluentAssertions` `PackageReference`
out of the `IsTestProject=true` group and into a shared `ItemGroup` that
applies whenever the outer `IsTestProject OR IsTestLibrary` `When` matches.
Comment documents why. The existing `<Using Include="FluentAssertions" />`
global-using now resolves in both runnable test projects and shared test
helper libraries.

### Verification

`dotnet build src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/Altinn.AccessManagement.TestUtils.csproj -c Release`
→ `Build succeeded. 0 Error(s)` (warnings unchanged).

## Issue B — `Coverage threshold check` step fails with "No coverage files generated"

Seen in `ci (pkg: PEP) / Coverage threshold check`:

```text
dotnet test (vstest fallback)
Passed! - Failed: 0, Passed: 92, Skipped: 0, Total: 92 ... (net9.0|x64)
No coverage files generated.
Error: Process completed with exit code 1.
```

### Root cause

`docs/testing/run-coverage.ps1` detected xUnit v3 test executables by
probing `$projName.exe`, which only exists on Windows. CI runs on Linux
where the apphost has no `.exe` suffix, so the script always fell through
to the `dotnet test (vstest fallback)` branch. That branch relies on
VSTest's `XPlat Code Coverage` collector — which is not invoked for xUnit v3
projects routed through MTP (step 35), so no `coverage.cobertura.xml` is
produced. The script then exits 1 with "No coverage files generated".

### Change

`docs/testing/run-coverage.ps1`:

- Detect v3 MTP projects cross‑platform by checking for both `$projName.dll`
  and `$projName.runtimeconfig.json` (the runtimeconfig is always emitted
  for `OutputType=Exe` and is OS-independent).
- Run the collector as `dotnet-coverage collect -- dotnet <dll>` so the
  managed entry point is invoked the same way on Windows and Linux.
- Keep the VSTest fallback path untouched for any future non-v3 projects.

### Verification

Local smoke test on Windows: `dotnet build` of all verticals still succeeds;
detection now keys off files present on every platform. Full CI
re‑run will confirm Linux behavior.

## Not in scope

- Flaky `PermitWithActionFilterMatch` (still deferred — see step 34/35 docs).
- Any real test failures surfaced by MTP enablement in
  `AccessManagement.ServiceOwner.Api.Tests` / `Enduser.Api.Tests`.
