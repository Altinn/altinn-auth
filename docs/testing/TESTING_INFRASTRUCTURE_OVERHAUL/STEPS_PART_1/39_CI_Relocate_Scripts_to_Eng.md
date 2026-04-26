# Step 39 — Relocate build tooling from `docs/testing/` to `eng/testing/`

## Goal

Move executable build/test tooling out of the documentation tree and into a
dedicated engineering tooling folder that follows common .NET repo
conventions (`eng/` is the standard location for build/CI scripts in
dotnet/runtime, dotnet/aspnetcore, and many Microsoft OSS repos).

## Rationale

`docs/` is the documentation tree. The coverage scripts and thresholds JSON
are consumed by the CI pipeline (not end-user docs), so colocating them with
other per-vertical/per-repo engineering assets under `eng/testing/` makes the
intent clearer and keeps `docs/` focused on human-readable material.

No behavior change — this is pure relocation.

## What moved

Three files moved via `git mv` (history preserved):

| From | To |
|------|----|
| `docs/testing/run-coverage.ps1` | `eng/testing/run-coverage.ps1` |
| `docs/testing/run-accessmanagement-coverage.ps1` | `eng/testing/run-accessmanagement-coverage.ps1` |
| `docs/testing/coverage-thresholds.json` | `eng/testing/coverage-thresholds.json` |

## Ripple effects

### 1. `.github/workflows/tpl-vertical-ci.yml`

Updated the two path strings and one comment in the "Coverage threshold
check" step:

- `$globalThresholds = Join-Path $env:GITHUB_WORKSPACE 'docs/testing/coverage-thresholds.json'`
  → `'eng/testing/coverage-thresholds.json'`
- `& (Join-Path $env:GITHUB_WORKSPACE 'docs/testing/run-coverage.ps1')`
  → `'eng/testing/run-coverage.ps1'`
- Comment `# fall back to the global one under docs/testing/.`
  → `# fall back to the global one under eng/testing/.`

### 2. Scripts themselves — no change required

- `run-coverage.ps1` computes `$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent`
  (2 levels up). From `eng/testing/` that still lands at the repo root.
- `run-coverage.ps1` default thresholds fallback:
  `Join-Path $PSScriptRoot 'coverage-thresholds.json'` — the JSON moves
  alongside the script.
- `run-accessmanagement-coverage.ps1` delegates via
  `& "$PSScriptRoot\run-coverage.ps1"` — moves cleanly together.

### 3. Historical step docs under `docs/testing/steps/`

Older step docs still reference the previous `docs/testing/...` paths in
example command lines. These are archival and intentionally left as-is —
rewriting historical narratives would rewrite history. Going forward, new
docs should reference `eng/testing/`.

## Verification

- `git status` shows renames (`R` / `RM`), not add+delete — history preserved.
- Grep confirms only archival step docs still reference the old paths; no
  active script, workflow, or csproj does.
- Workflow YAML still parses as the two path literals + comment were the
  only edits.

## Follow-ups (deferred)

- Optionally move any future CI/test tooling (e.g., the Docker/Podman
  bootstrap snippets currently in `docs/testing/`) into `eng/testing/` over
  time.
- Sweep archival step docs to add a "paths updated in Step 39" note if the
  historical commands are ever re-run in support work.
