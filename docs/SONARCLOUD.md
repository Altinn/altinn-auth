# SonarCloud

SonarCloud runs static analysis on every push and PR — code smells, bugs,
security hotspots, duplication, and ingested test coverage. Each vertical
(`apps/*`, `libs/*`, `pkgs/*`) is its own SonarCloud project under the shared
[`altinn`](https://sonarcloud.io/organizations/altinn) organization, scanned
in **monorepo mode** so projects are isolated despite sharing a repository.

## Where the config lives

| File | What it controls |
|---|---|
| [`SonarQube.Analysis.xml`](../SonarQube.Analysis.xml) | Shared analysis settings: host URL, exclusions, coverage report paths, duplication exclusions, monorepo flag. Referenced from CI via `/s:`. |
| [`.github/workflows/tpl-vertical-ci.yml`](../.github/workflows/tpl-vertical-ci.yml) | The `build-test-analyze` job. Passes the four CLI-only properties (key / name / org / token) inline; everything else comes from the XML. |
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

Current state:

| Vertical | Project key |
|---|---|
| `apps/Altinn.AccessManagement` | `Authorization_AccessManagement` |
| `apps/Altinn.Authorization` | `Authorization_Authorization` |
| `apps/Altinn.Register` | *disabled* (`sonarcloud: false`) |

To onboard a new vertical: create the SonarCloud project under the `altinn`
organization (Sonar UI → New project → GitHub → pick the repo → set monorepo
mode), then add `"sonarcloud": { "projectKey": "..." }` to its `conf.json`.

### Automatic skips

Even when a vertical opts in, the `Detect SonarCloud config` step skips
Sonar when:

- the PR is from a fork (no `SONAR_TOKEN` access)
- the PR was opened by `renovate[bot]` or `dependabot[bot]` — bot PRs only
  touch dependency manifests and produce no useful Sonar findings, so
  analysing them just burns Sonar minutes per vertical

Pushes to `main` (including the merge commit of a bot-authored PR) always
run Sonar — the bot author check uses `pull_request.user.login`, which is
null on push events.

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

| Output | Consumer |
|---|---|
| `TestResults/coverage.cobertura.xml` | `eng/testing/check-coverage-thresholds.ps1` |
| `TestResults/coverage.xml` *(VSCoverage XML)* | SonarCloud (via `sonar.cs.vscoveragexml.reportsPaths` in the XML config) |

Sonar's C# scanner does not accept cobertura, which is why both formats are
materialized. For local coverage workflow see
[testing/COVERAGE.md](testing/COVERAGE.md).

## Quality gate

The default SonarCloud quality gate applies. PR decoration posts inline
comments for issues introduced in the PR's "new code" period; the gate
result is reported as a check but **does not block merging**
(`sonar.qualitygate.wait=false` in the XML). To make it blocking, flip that
property to `true` — expect ~30–90 s added to PR check time.

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
