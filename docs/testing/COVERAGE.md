# Code Coverage

The repo uses `dotnet-coverage` to produce Cobertura reports and a small
PowerShell wrapper to parse them and report per-assembly coverage against
targets. **Assemblies below their target are surfaced as warnings; coverage
never fails the build** — it is reported, not gated, in CI and locally alike.

## Files

| File | What it is |
|---|---|
| [`eng/testing/coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json) | Source of truth for per-assembly minimum line coverage |
| [`eng/testing/check-coverage-thresholds.ps1`](../../eng/testing/check-coverage-thresholds.ps1) | Parse-only script (reads a Cobertura file, pretty-prints a table, and reports below-target assemblies as warnings; never fails on coverage) |
| [`eng/testing/run-coverage.ps1`](../../eng/testing/run-coverage.ps1) | **Local-dev** helper: builds every `*.Tests` project, runs each under `dotnet-coverage collect` in parallel, then invokes the check script |
| [`eng/testing/run-accessmanagement-coverage.ps1`](../../eng/testing/run-accessmanagement-coverage.ps1) | Local-dev helper scoped to the AccessManagement vertical (faster feedback) |
| [`eng/testing/coverage.settings`](../../eng/testing/coverage.settings) | `dotnet-coverage` collection settings (passed via `--settings`) that exclude generated code / migrations / tooling from the line totals, so the floors measure the same denominator as SonarCloud |

## Thresholds

[`coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json) is the source of
truth for the current per-assembly numbers (don't duplicate them here). It has two sections:

- **`assemblies`**: the primary per-assembly targets; any below their floor are reported as a warning.
- **`warnings`**: a softer tier for assemblies approaching promotion to `assemblies`.

Neither tier fails the build — both are reported.

### `globalThreshold`

Currently `0`, so only the explicitly listed assemblies are tracked. We
deliberately do **not** set a coverage target on every assembly in the repo;
many are thin adapters, generated code, or host wiring where line coverage
isn't a meaningful target.

## Philosophy

Coverage percentage is a guardrail against regression, not a quality goal in
itself. New tests should be driven by a concrete risk: a named bug class
(mapping, translation, validation, an authorization boundary), a regression we
want pinned, or a branch whose failure would matter. Not by a number to hit.

Consequences:

- A mock-based test is worth writing when it exercises real branching,
  validation, or decision logic in the code under test. It is **not** worth
  writing when its only assertion is that a pass-through maps its input to its
  output (response-shape mapping over a thin adapter), which adds a percentage
  point and a maintenance burden without guarding behaviour.
- Prefer covering pure logic (validators, mappers with real rules, decision
  paths) over plumbing/I-O orchestration that can only be reached by mocking
  out everything it does.
- The floors exist to stop covered code from rotting, so raise a floor (or
  promote a `warnings` entry to `assemblies`) when real coverage lands, but do
  not lower a floor to make a thin test "count".

## Running locally

Full repo:

```
pwsh eng/testing/run-coverage.ps1
```

Just AccessManagement:

```
pwsh eng/testing/run-accessmanagement-coverage.ps1
```

Scope to specific projects:

```
pwsh eng/testing/run-coverage.ps1 -Projects @(
    "src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.Api.Tests/Altinn.AccessManagement.Api.Tests.csproj"
)
```

Outputs land in `TestResults/`:

- `*.cobertura.xml`: one per test project
- `*.coverage.log`: captured test stdout (last N lines are echoed on failure)

## How CI runs it (single pass)

CI does **not** run `run-coverage.ps1`. It runs the suite once under
`dotnet-coverage collect --settings eng/testing/coverage.settings` **on the nightly
analyze run only**, then invokes `check-coverage-thresholds.ps1` on the resulting
Cobertura file (report-only: below-target assemblies are surfaced as warnings, they do
not fail the build). PR runs execute the tests directly, without coverage instrumentation
— coverage is informational, so it does not need to run on every PR. The exact steps live in
[`.github/workflows/tpl-vertical-ci.yml`](../../.github/workflows/tpl-vertical-ci.yml).

## What is excluded from the denominator

Both coverage systems exclude the same non-product code so a floor reflects
coverage of real logic:

- **SonarCloud** via `sonar.coverage.exclusions` in
  [`SonarQube.Analysis.xml`](../../SonarQube.Analysis.xml).
- **CI floors** via [`eng/testing/coverage.settings`](../../eng/testing/coverage.settings),
  passed to `dotnet-coverage collect --settings`.

Excluded in both: test/mock assemblies, generated code (`*.g.cs`,
`*.Designer.cs`, `Properties/AssemblyInfo.cs`), EF migrations, the Aspire host
composition root (`*.AppHost`), developer tooling (`Altinn.Authorization.Cli`),
and the `Altinn.AccessMgmt.FFB` / `Altinn.AccessMgmt.WebComponents` front-end
modules. (`check-coverage-thresholds.ps1` independently scores only "Owned"
`Altinn.*` assemblies, so Tests/TestUtils/Mocks never count regardless.)

Deliberately **not** excluded:

- `Program.cs` / `Startup.cs`: in the controller APIs the minimal-hosting
  bootstrap is large and genuinely exercised by integration tests, and the
  floors are calibrated with it counted. Excluding it would drop the API hosts
  below their floors, so it stays in the denominator in both systems.
- EF entity-type configurations (`PersistenceEF/Configurations`): well-covered
  declarative schema, so dropping them would only lower the measured percentage
  and risk its floor.

Keep the two lists in sync: when you add an exclusion to one, mirror it in the
other. `coverage.settings` uses the VSTest `.runsettings` shape with a
`DataCollector` named `Code Coverage`, and its `ModulePaths` / `Attributes` /
`Sources` elements must appear in that order or `dotnet-coverage` rejects the
whole file.

## Adjusting a threshold

- **Raising** an existing threshold: open a PR that bumps the number in
  `coverage-thresholds.json`. No other change needed.
- **Adding** a new tracked assembly: add it to `assemblies` with a
  realistic target. If current coverage is close but not quite there, add
  it to `warnings` first and promote later.
- **Lowering** a threshold: discouraged. Include a short rationale in the PR
  description (e.g. "deleted an entire well-covered module"). Prefer adding
  tests.

## Next: [CI.md](CI.md)
