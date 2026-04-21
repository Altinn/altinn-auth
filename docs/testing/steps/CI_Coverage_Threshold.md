# Sub-step 6.6 — CI Coverage Threshold

## Goal

Prevent coverage regressions by enforcing per-assembly minimum line-coverage
thresholds in CI. PRs that drop below the floor fail the build.

## Changes

### 1. `docs/testing/coverage-thresholds.json` (new)

Per-assembly threshold configuration:

| Assembly | Floor |
|---|---|
| `Altinn.Authorization` | 60% |
| `Altinn.Authorization.ABAC` | 60% |
| `Altinn.Authorization.PEP` | 75% |
| *(all others)* | 50% (global) |

Thresholds are set ~3% below current baselines to allow minor fluctuations
without blocking PRs, while still catching significant regressions.

### 2. `docs/testing/run-coverage.ps1` (updated)

- New `-ThresholdsFile` parameter accepts a JSON config path.
- Auto-discovers `coverage-thresholds.json` in the script's directory if no
  explicit file is provided.
- Per-assembly thresholds override the global threshold.
- Exit code 1 if any assembly falls below its floor.

### 3. `.github/workflows/tpl-vertical-ci.yml` (updated)

Added a **Coverage threshold check** step after the Test step:
- Installs `dotnet-coverage` tool.
- Discovers test projects in the vertical.
- Runs `run-coverage.ps1 -NoBuild -ThresholdsFile ...` to collect coverage
  and enforce thresholds.
- Skipped if no test projects exist in the vertical.

## Usage

```powershell
# Local: run with per-assembly thresholds (auto-discovered)
docs/testing/run-coverage.ps1

# Local: explicit thresholds file
docs/testing/run-coverage.ps1 -ThresholdsFile docs/testing/coverage-thresholds.json

# Local: override with a flat threshold
docs/testing/run-coverage.ps1 -Threshold 60
```

## Verification

- [x] `run-coverage.ps1` accepts `-ThresholdsFile` parameter
- [x] Per-assembly thresholds loaded from JSON config
- [x] CI workflow includes coverage threshold step
- [ ] CI pipeline passes on next push (verify after merge)

---

**Note:** For current recommendations and next steps, see [INDEX.md](INDEX.md).
