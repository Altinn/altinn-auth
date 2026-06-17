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
| 10 | [BRUNO_API_TESTS.md](BRUNO_API_TESTS.md) | The Bruno API collections — manual/exploratory API tests that double as a behavioral spec for the C# integration tests |
| 11 | [../SONARCLOUD.md](../SONARCLOUD.md) | Static analysis: exclusions, per-vertical setup, quality gate, debugging |

## TL;DR for newcomers

- **Test framework:** xUnit v3, all test projects target `net10.0`.
- **Unit vs integration:** every test class is tagged `[UnitTest]` or
  `[IntegrationTest]` (a `Category` trait) and lives under a `Unit/` or
  `Integration/` folder with a matching namespace segment. CI runs them as
  two lanes; filter locally with `dotnet test -- --filter-trait "Category=Unit"`.
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
