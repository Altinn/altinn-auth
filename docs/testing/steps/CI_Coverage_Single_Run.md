# Step 41 — CI perf: single test run + parse-only threshold check

**Phase:** 6.6 follow-up
**Branch:** `feature/2842_Optimize_Test_Infrastructure_and_Performance`
**Status:** ✅ Completed

## Goal

Remove duplicate test execution in the vertical CI pipeline. Before this step the
"Test" step (~2m14s) and the "Coverage threshold check" step (~4m32s) each ran
the full test suite — once for pass/fail, once under `dotnet-coverage collect`
— making Coverage consistently ~2× longer than Test despite doing the same
work. Combined test time was ~6m46s per vertical.

Constraints:

1. Preserve step-level failure diagnosability in GitHub Checks UI — a
   **test failure** must still show up on the Test step (not buried in the
   coverage step), and a **coverage regression** must still be its own red
   checkmark independent of test outcome.
2. Keep `eng/testing/run-coverage.ps1` usable for local-dev (developer typing
   `./run-coverage.ps1` on their workstation).
3. No drift between local and CI threshold enforcement output.

## What changed

### New file: `eng/testing/check-coverage-thresholds.ps1` (~150 lines)

Parse-only script. Reads one or more Cobertura XMLs, loads the per-assembly
thresholds JSON, enforces thresholds **only** for assemblies owned by
`-OwnedRoot` (matches the Step 34 "CI Coverage Threshold Scoping" behaviour
that was previously buried inside `run-coverage.ps1`), and emits the color-
coded per-assembly line/branch summary. Completes in seconds because no tests
run.

Key parameters:

- `-CoverageFiles` (mandatory) — glob-resolved list of cobertura XMLs.
- `-ThresholdsFile` — defaults to `eng/testing/coverage-thresholds.json`.
- `-Threshold` — optional global-floor override.
- `-OwnedRoot` — directory prefix; assemblies whose source files don't start
  with this prefix are shown with `(ref)` and excluded from enforcement.

Exit codes: `0` on success, `1` if any owned assembly is below its threshold
or if no coverage files matched.

### Modified: `eng/testing/run-coverage.ps1` (213 → 148 lines)

Still the developer-workstation helper. Continues to run per-project
`dotnet-coverage collect` in parallel (`ForEach-Object -Parallel`) so the
dominant project sets the wall-clock instead of the sum of all projects.
After collecting, it now delegates to `check-coverage-thresholds.ps1` rather
than re-implementing the threshold-enforcement + pretty-print logic inline.
This removes ~70 lines of duplicated parsing/formatting code and guarantees
local-dev output matches CI.

Removed from this script: per-assembly threshold parsing, `Test-IsOwnedByVertical`
helper, manual color output, warnings-below-target list. All moved into the
new shared script.

### Modified: `.github/workflows/tpl-vertical-ci.yml`

1. **New "Install dotnet-coverage" step** (before Test). Kept separate so
   tool-install failures get their own red signal and don't pollute the Test
   step log.
2. **"Test" step rewritten** — from:

   ```yaml
   run: dotnet test -c Release --no-build -bl:binlog/test.binlog -- --ignore-exit-code 8
   ```

   to:

   ```yaml
   run: dotnet-coverage collect --nologo --output TestResults/coverage.cobertura.xml --output-format cobertura -- dotnet test -c Release --no-build -bl:binlog/test.binlog -- --ignore-exit-code 8
   ```

   `dotnet-coverage collect` is transparent to the wrapped command's exit
   code, so:
   - Exit 0 → tests passed, coverage XML produced.
   - Exit 2 → real test failures → this step goes red (correct signal).
   - Exit 8 → no tests ran (e.g. every test `[Skip]`ped because required
     infra is absent in CI) → swallowed by `--ignore-exit-code 8`.

3. **"Coverage threshold check" step rewritten** — no longer runs tests. Just
   invokes `check-coverage-thresholds.ps1` against the cobertura XML the Test
   step already produced. Guarded by `hashFiles(...)/coverage.cobertura.xml !=
   ''` so it skips cleanly when the Test step produced nothing (e.g. the
   all-skipped Host.Lease vertical).

4. **"Upload test results" path glob updated** — the stale
   `TestResults/*.coverage.log` entry (which only existed in the local-dev
   path under the repo root) was removed. Per-project MTP logs at
   `bin/.../TestResults/*.log` and TRX reports continue to be uploaded on
   failure.

## Verification

- **PowerShell parse**: both scripts `[System.Management.Automation.PSParser]::Tokenize`
  cleanly.
- **YAML parse**: `ConvertFrom-Yaml` on `tpl-vertical-ci.yml` succeeds.
- **Line count**: `run-coverage.ps1` is 148 lines (down from 213); no duplicate
  content after the truncation fix.
- **CI run**: to be validated on the next push.

## Expected CI timing change

Before: Test 2m14s + Coverage 4m32s = **6m46s** per vertical.
After: Test ~2m30s (small overhead from `dotnet-coverage collect` wrapping MTP)
+ Coverage ~5-10s (parse-only) = **~2m40s** per vertical.

Net savings: ~4m per vertical per CI run. With ~8 verticals building
concurrently the wall-clock impact depends on which vertical is on the
critical path, but compute-time savings are ~30m+ per run.

## Why not collapse into a single step?

Considered but rejected. Collapsing would merge two distinct failure signals
(test failure vs coverage regression) into one red checkmark. PR authors
would have to read step logs to know which actually broke. Keeping Step B
as a separate ~5-second parse-only step preserves the one-glance signal
separation in the Checks UI at near-zero cost.

## Deferred / follow-ups

- None. The Sonar `analyze` job already uses the unified
  `dotnet coverage collect 'dotnet test'` pattern (see Step 38), so this
  step brings the `Build and Test` job in line with it.
