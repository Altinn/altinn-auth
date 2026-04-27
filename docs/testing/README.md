# Testing

Welcome. This folder is the entry point for everything you need to know about
the test suite in this repository — how it's organised, how to run it locally,
what fixtures and mocks are available, the conventions we follow, and how
coverage is enforced in CI.

If you're new to the codebase, read these in order. Each doc is short and
self-contained; none of them is a wall of text.

## Contents

| # | Doc | What it covers |
|---|---|---|
| 1 | [GETTING_STARTED.md](GETTING_STARTED.md) | Prerequisites, how to run the tests, Docker/Podman, IDE setup |
| 2 | [TEST_PROJECTS.md](TEST_PROJECTS.md) | Inventory of every test project, what it tests, how it maps to a production assembly |
| 3 | [FIXTURES.md](FIXTURES.md) | `ApiFixture`, `AuthorizationApiFixture`, `LegacyApiFixture`, `EFPostgresFactory` — when to use which |
| 4 | [MOCKS_AND_TESTUTILS.md](MOCKS_AND_TESTUTILS.md) | The shared `TestUtils` library, canonical mocks, test certificates |
| 5 | [WRITING_TESTS.md](WRITING_TESTS.md) | Patterns, xUnit v3 specifics, when to unit-test vs integration-test |
| 6 | [TEST_NAMING_CONVENTION.md](TEST_NAMING_CONVENTION.md) | `MethodUnderTest_Scenario_ExpectedResult` |
| 7 | [FLUENT_ASSERTIONS_GUIDELINES.md](FLUENT_ASSERTIONS_GUIDELINES.md) | When and how to use FluentAssertions |
| 8 | [COVERAGE.md](COVERAGE.md) | Running coverage locally, per-assembly thresholds, ratcheting |
| 9 | [CI.md](CI.md) | How tests run in the pipeline, Microsoft Testing Platform (MTP), artifacts |
| 10 | [../SONARCLOUD.md](../SONARCLOUD.md) | Static analysis: exclusions, per-vertical setup, quality gate, debugging |

## Historical / reference

| Doc | Purpose |
|---|---|
| [TESTING_INFRASTRUCTURE_OVERHAUL.md](TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md) | The 2025 audit and the phased plan that produced the current setup. Marked complete; retained as a ledger of the issues (C1–C5, M1–M8, L1–L3) and the decisions taken. |
| [steps/INDEX.md](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md) | Chronological step log. Every individual change has a numbered step doc; this index is where follow-up work and blocked items are tracked. |

## TL;DR for newcomers

- **Test framework:** xUnit v3, all projects target `net9.0`.
- **Assertion library:** [FluentAssertions](FLUENT_ASSERTIONS_GUIDELINES.md)
  (globally imported — no `using` needed).
- **Integration tests** use a real PostgreSQL via Testcontainers. You need a
  **working Docker or Podman** on your machine. See
  [GETTING_STARTED.md](GETTING_STARTED.md).
- **Shared test code** lives in `Altinn.AccessManagement.TestUtils`
  (fixtures, mocks, test certificates, token generator).
- **Run everything:** `dotnet test` from the repo root.
- **Run with coverage locally:** `pwsh eng/testing/run-coverage.ps1`.
- **CI gates:** per-assembly line-coverage thresholds in
  [`eng/testing/coverage-thresholds.json`](../../eng/testing/coverage-thresholds.json).
