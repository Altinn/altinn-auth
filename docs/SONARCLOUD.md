# SonarCloud

SonarCloud runs static analysis — code smells, bugs, security hotspots,
duplication, and ingested test coverage — **once a day against `main`**, not
on every push/PR. The per-push model contended for consistently busy runners
while adding little per-change value: the quality gate is non-blocking,
security is covered by CodeQL (default setup, on PRs + main), and code smells
are enforced in-build by Roslyn + StyleCop, with per-assembly coverage
reported (not gated) by `check-coverage-thresholds.ps1`. Sonar's unique value (duplication,
the maintainability / tech-debt dashboard, coverage trend) is a property of
`main`, so a daily off-peak scan is sufficient.

Each vertical (`apps/*`, `libs/*`, `pkgs/*`) is its own SonarCloud project
under the shared [`altinn`](https://sonarcloud.io/organizations/altinn)
organization, scanned in **monorepo mode** so projects are isolated despite
sharing a repository.

## Where the config lives

| File | What it controls |
|---|---|
| [`SonarQube.Analysis.xml`](../SonarQube.Analysis.xml) | Shared analysis settings: host URL, exclusions, coverage report paths, duplication exclusions, monorepo flag. Referenced from CI via `/s:`. |
| [`.github/workflows/sonar-nightly.yml`](../.github/workflows/sonar-nightly.yml) | The scheduled trigger. Runs daily against `main`, selects the Sonar-enabled verticals, and calls the template below with `analyze: true`. |
| [`.github/workflows/tpl-vertical-ci.yml`](../.github/workflows/tpl-vertical-ci.yml) | The `build-test-analyze` job. The Sonar steps are gated on the `analyze` input (only the nightly caller sets it). Passes the four CLI-only properties (key / name / org / token) inline; everything else comes from the XML. |
| `src/apps/<vertical>/conf.json` | Per-vertical opt-in / project key (see below). |

The CI invocation is intentionally minimal:

```bash
dotnet-sonarscanner begin \
  /key:"$SONAR_KEY" \
  /name:"$SONAR_NAME" \
  /o:"altinn" \
  /d:sonar.token="$SONAR_TOKEN" \
  /s:"$GITHUB_WORKSPACE/SonarQube.Analysis.xml"
```

The scanner restricts four properties to CLI flags only — they cannot be
moved into `SonarQube.Analysis.xml`:

| Property | CLI flag |
|---|---|
| `sonar.projectKey` | `/k:` |
| `sonar.projectName` | `/n:` |
| `sonar.projectVersion` | `/v:` |
| `sonar.organization` | `/o:` |

Everything else (host URL, exclusions, coverage paths, monorepo flag,
quality-gate-wait, etc.) lives in the XML. If you need to change one of
those, edit the XML — not the workflow.

## Per-vertical setup

Each vertical's `conf.json` declares whether SonarCloud runs and under which
project key. Two shapes:

```jsonc
// Enabled
{ "sonarcloud": { "projectKey": "Authorization_AccessManagement" } }

// Disabled (Sonar steps in build-test-analyze are skipped; build/test still run)
{ "sonarcloud": false }
```

> **Default is enabled — set `false` explicitly to opt out.** When `conf.json`
> omits `sonarcloud` entirely (or the vertical has no `conf.json`),
> [`_meta.mts`](../.github/scripts/_meta.mts) defaults it to
> `{ enabled: true }` with a *derived* key `authorization-<slug>`. If no
> SonarCloud project exists for that derived key, the scanner still runs
> begin/end and "succeeds", but the upload lands nowhere ("Project not found")
> — so the steps burn ~78s per scan for no dashboard. A vertical that should
> **not** be scanned must say so explicitly with `"sonarcloud": false`; this is
> why every non-scanned vertical carries that line.

Current state — the verticals that are scanned:

| Vertical | Project key |
|---|---|
| `apps/Altinn.AccessManagement` | `Authorization_AccessManagement` |
| `apps/Altinn.Authorization` | `Authorization_Authorization` |

Every other vertical sets `"sonarcloud": false` and is not scanned. Notably
`apps/Altinn.Register` is a copy of a codebase that lives in its own
repository (`Altinn/altinn-register`), which owns its SonarCloud analysis —
running it (or surfacing its scores) here would be duplicate and misleading.

To onboard a new vertical: create the SonarCloud project under the `altinn`
organization (Sonar UI → New project → GitHub → pick the repo → set monorepo
mode), then add `"sonarcloud": { "projectKey": "..." }` to its `conf.json`.

### When it runs

The `Detect SonarCloud config` step enables Sonar only when **both** hold:

- the caller passed `analyze: true` — only [`sonar-nightly.yml`](../.github/workflows/sonar-nightly.yml)
  does, so PR and push-to-`main` CI never run Sonar
- the vertical opted in (`sonarProjectKey != 'false'`)

The nightly scan always runs against `main` HEAD, so the fork / bot-author
guards the per-push model needed are gone — there is no PR context to skip.
Verticals with `sonarcloud: false` are filtered out by the nightly workflow
before the matrix fans out, so they are not even built for a scan that would
be skipped.

## Exclusions

`SonarQube.Analysis.xml` defines four orthogonal exclusion lists:

| Property | Effect | Used for |
|---|---|---|
| `sonar.exclusions` | File is invisible to Sonar — no issues, metrics, or coverage | Migrations, generated `.g.cs` / `.Designer.cs`, the FFB / WebComponents folders |
| `sonar.test.exclusions` | File is not analyzed *as test code* | Everything under `**/test/**`, `*.Tests`, `*.TestUtils`, `*.Mocks` |
| `sonar.coverage.exclusions` | File is analyzed for issues but excluded from line-coverage % | Test code, `Program.cs` / `Startup.cs`, generated files, migrations |
| `sonar.cpd.exclusions` | File is exempt from copy/paste detection | Property-bag files: `Models/`, `Dto(s)/`, `Entities/`, `Contracts/`, `*Dto.cs`, `*Request.cs`, `*Response.cs` |

**Pick the narrowest one that solves your problem.** Most additions belong in
`coverage.exclusions` or `cpd.exclusions`; reach for `exclusions` only when
the file genuinely shouldn't be analyzed at all.

## Coverage import

Tests run **once** under `dotnet-coverage collect`, producing a native
`.coverage` binary. Two `dotnet-coverage merge` steps then convert it to
the formats consumed downstream:

| Output | Consumer | Produced |
|---|---|---|
| `TestResults/coverage.cobertura.xml` | `eng/testing/check-coverage-thresholds.ps1` | every run |
| `TestResults/coverage.xml` *(VSCoverage XML)* | SonarCloud (via `sonar.cs.vscoveragexml.reportsPaths` in the XML config) | analyze (nightly) run only |

Sonar's C# scanner does not accept cobertura, which is why a second format is
materialized — but only on the nightly analyze run, the only run that uploads
to Sonar (the VSCoverage conversion is gated on `SonarCloud begin` having
run). PR/main CI produces just the cobertura report for the threshold gate.
For local coverage workflow see [testing/COVERAGE.md](testing/COVERAGE.md).

## Quality gate

The default SonarCloud quality gate applies, evaluated against `main` on each
nightly scan. Because analysis no longer runs on PRs, there is **no PR
decoration** — findings surface on the project's SonarCloud dashboard, not as
inline PR comments. A maintainability / duplication regression introduced by a
PR is therefore visible on the next nightly scan of `main` (≤24 h later), not
at PR time; security regressions are still caught at PR time by CodeQL.

The gate is non-blocking (`sonar.qualitygate.wait=false` in the XML) and the
nightly job does not fail on a red gate — the dashboard is the signal. To be
alerted on a gate breach, configure SonarCloud's own notifications for the
project rather than failing the cron.

### New Code definition

Because "new code" can no longer mean "this PR's diff", the New Code period is
a rolling time window instead. Both scanned projects
(`Authorization_AccessManagement`, `Authorization_Authorization`) are set to
**Number of days = 30** in the SonarCloud UI — the gate's new-code conditions
evaluate against issues introduced on `main` in the last 30 days.

This is project-scoped UI state, not source-controlled. To change it: project →
**Administration → New Code** (or the org-level default under the `altinn`
organization). Avoid "Previous version" here — it keys off `sonar.projectVersion`
release tags, which this pipeline doesn't set.

## Common operations

**Add an exclusion.** Edit `SonarQube.Analysis.xml` and the change applies to
every vertical on the next CI run. No per-project config needed.

**Disable Sonar for a vertical.** Set `"sonarcloud": false` in the vertical's
`conf.json`. The `Detect SonarCloud config` step in the workflow then sets
`enabled=false`, and every Sonar-specific step downstream is skipped — no
project deletion required on Sonar's side. Build, test, and the coverage
threshold check still run as normal.

**Bump the scanner version.** Update the pinned version in two places that
must agree: the `--version` flag in the *Install coverage and Sonar tools*
step in `tpl-vertical-ci.yml`, and the cache key prefix on the same job
(otherwise the cache hands the old binary to the new pinned install).

**Mark something as "won't fix" or "false positive".** Do it in the
SonarCloud UI for the affected vertical's project — these decisions are
project-scoped state, not source-controlled.

## Debugging a failed Sonar run

1. Check the `SonarCloud end` step log in the failing job. The scanner
   prints a summary URL near the end (`Quality gate status:` line) — open
   it for the per-issue breakdown.
2. If the failure is in `SonarCloud begin`, the cause is usually an invalid
   `SONAR_TOKEN`, an `xmlns` typo in `SonarQube.Analysis.xml`, or a project
   key mismatch. Confirm the key in `conf.json` matches a project that
   exists in the `altinn` org.
3. If `Build` fails but local builds pass, the issue is almost always the
   `--no-incremental` rebuild hitting an analyzer that's tolerated by the
   incremental build. Reproduce locally with `dotnet build --no-incremental`.
4. If coverage is reported as 0 % despite passing tests, check that the
   `Convert coverage to VSCoverage XML` step ran and produced
   `TestResults/coverage.xml` — the path is relative to the vertical's
   `working-directory`, not the repo root.

## Related

- [testing/CI.md](testing/CI.md) — overall pipeline shape.
- [testing/COVERAGE.md](testing/COVERAGE.md) — local coverage workflow and
  threshold enforcement.
- [Tracking issue #2936](https://github.com/Altinn/altinn-authorization-tmp/issues/2936)
  — open follow-ups for the SonarCloud setup.
