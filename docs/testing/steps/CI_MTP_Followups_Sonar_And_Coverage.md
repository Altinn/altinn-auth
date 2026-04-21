# Step 38 — MTP follow-ups: forward correct flags in the Sonar `Analyze` job

## Goal

Now that step 37 has restored real xUnit v3 / Microsoft Testing Platform (MTP)
routing across all verticals, two latent issues in the `analyze` job of
`.github/workflows/tpl-vertical-ci.yml` were about to surface. Address both in
a single small change, plus tighten one comment in `docs/testing/run-coverage.ps1`.

## Audit findings

1. **`analyze` job would fail on all-skipped assemblies.**
   `dotnet coverage collect 'dotnet test --no-build --results-directory TestResults/'`
   passed no flags through to MTP. Under MTP, `Host.Lease` (all tests `[Skip]`
   without Azurite) exits with code 8 (ZeroTestsRan), which propagates up and
   fails the whole Sonar step for the `lib: Altinn.Authorization.Host` vertical.

2. **Sonar would stop seeing test results.**
   Before step 35 the inner `dotnet test` ran under VSTest and produced `.trx`
   files that Sonar picks up via `sonar.cs.vstest.reportsPaths="**/*.trx"`. With
   MTP, no `.trx` is emitted unless you opt in. That's a silent Sonar regression
   (coverage would still be reported; test outcomes would not).

3. **`--results-directory` was being passed to VSTest, not MTP.**
   Without `--` it went to the `dotnet test` CLI (still VSTest contract).
   Harmless while routing to VSTest; meaningless/ignored under MTP.

4. **Initial (aborted) concern about `run-coverage.ps1`.**
   I first suspected that `dotnet-coverage collect -- dotnet <dll>` would hit
   MTP's ZeroTestsRan=8 for all-skipped Host.Lease. Verified locally that
   `dotnet <dll>` invokes xUnit v3's **native in-process runner** (not MTP);
   that runner returns 0 when every test is `[Skip]`ped, so no handling is
   required. Documented that in a comment.

## MTP CLI surface (verified locally)

Reading the crash log from a deliberately-wrong invocation
(`dotnet test -- --report-trx`) confirmed the correct flags:

- `--ignore-exit-code 8` — MTP built-in
- `--results-directory <path>` — MTP built-in
- `--report-xunit-trx` — **xUnit v3's built-in TRX reporter**. The generic MTP
  `--report-trx` requires the separate `Microsoft.Testing.Extensions.TrxReport`
  package, which xunit.v3 2.0.3 does not bring in (only `…TrxReport.Abstractions`).
  `--report-xunit-trx` produces `.trx` under `<test-project>/TestResults/`,
  which `**/*.trx` glob-matches for Sonar's `sonar.cs.vstest.reportsPaths`.

## Changes

### 1. `.github/workflows/tpl-vertical-ci.yml` — `analyze` job

```diff
           dotnet build --no-incremental
-          dotnet coverage collect 'dotnet test --no-build --results-directory TestResults/' \
+          # xUnit v3 routes `dotnet test` through Microsoft Testing Platform (MTP).
+          # Everything after `--` is forwarded to each per-project MTP runner:
+          #   --results-directory   emit artifacts (including the TRX below) under TestResults/
+          #   --report-xunit-trx    generate .trx files so Sonar's sonar.cs.vstest.reportsPaths picks them up
+          #                         (xUnit v3's built-in TRX reporter; the generic MTP
+          #                         --report-trx would need the separate
+          #                         Microsoft.Testing.Extensions.TrxReport package)
+          #   --ignore-exit-code 8  treat "all tests [Skip]ped" (e.g. Host.Lease without Azurite) as success
+          dotnet coverage collect 'dotnet test --no-build -- --results-directory TestResults/ --report-xunit-trx --ignore-exit-code 8' \
             -f xml -o 'TestResults/coverage.xml'
```

### 2. `docs/testing/run-coverage.ps1` — document the (deliberate) lack of `--ignore-exit-code`

Added a comment clarifying that `dotnet-coverage collect -- dotnet <dll>`
invokes xUnit v3's native runner (which exits 0 on all-skipped), not MTP, so no
`--ignore-exit-code 8` forwarding is needed there. No functional change.

## Verification

End-to-end smoke test of the new Sonar invocation (against PEP):

```powershell
dotnet test …/Altinn.Authorization.PEP.Tests.csproj --no-build -- \
  --results-directory TestResults/ --report-xunit-trx --ignore-exit-code 8
```

- Output: MTP style (`Run tests: …dll` / `Passed! - Failed: 0, Passed: 92, …`).
- Exit code: `0`.
- TRX produced: `…/Altinn.Authorization.PEP.Tests/TestResults/HAVAND_<host>_<timestamp>.trx`
  → matches `**/*.trx` glob used by `sonar.cs.vstest.reportsPaths`.

Ran `dotnet <Host.Lease.Tests.dll>` directly to confirm all-skipped exit 0:

```text
=== TEST EXECUTION SUMMARY ===
   Altinn.Authorization.Host.Lease.Tests  Total: 2, Errors: 0, Failed: 0, Skipped: 2, Not Run: 0, Time: 0.060s
EXITCODE=0
```

confirming the `run-coverage.ps1` path needs no additional flag.

Full CI run is expected to keep the `build-and-test` job green (from step 37)
**and** restore Sonar's test-result reporting + keep `analyze` green on the
Host.Lease vertical.

## Not in scope (deferred audit items from the same review)

All five are cheap but not blocking CI today; captured as future hygiene:

- **Net9.0 hardcoded in `run-coverage.ps1`.** `$binDir = Join-Path … "net9.0"`
  would silently fall back to the VSTest branch on net10 bump / any
  multi-targeted test project. Cheap fix: glob `bin/$Configuration/net*` and
  take the first directory.
- **`--ignore-exit-code 8` masking routing regressions.** If MTP routing
  breaks again, zero tests will run and CI stays green. Consider
  `--minimum-expected-tests N` per project, or a single always-runs smoke test.
- **`dotnet workload restore` in every vertical** is a noisy no-op and an
  occasional flake vector. Candidate for removal or `continue-on-error: true`.
- **Sonar analyze rebuilds from scratch.** Unavoidable across separate jobs,
  but a single production build failure takes the entire Sonar step with it.
- **Binlog upload `if-no-files-found: error`** will fail the workflow if
  the test step is ever conditionally skipped. Minor.
