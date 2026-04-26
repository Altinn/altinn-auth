# Step 61 — Docs Restructure + Guided-Tour Suite

**Phase:** Post-overhaul housekeeping (follows Step 60, the final coverage step).

**Goal:** Make `docs/testing/` pleasant to walk into for any developer who has not
been part of the overhaul — while preserving the full historical record of the
60-step overhaul itself.

## What changed

### 1. New guided-tour docs at `docs/testing/` top level

Eight new docs, each ending with a `Next:` link to form a reading chain:

1. `README.md` — entry point / index.
2. `GETTING_STARTED.md` — 5-minute on-ramp (clone → build → run one test).
3. `TEST_PROJECTS.md` — map of the test project layout (apps/libs/pkgs + TestUtils).
4. `FIXTURES.md` — canonical fixtures (`ApiFixture`, `AuthorizationApiFixture`,
   `LegacyApiFixture`, `EFPostgresFactory`, `PostgresFixture`).
5. `MOCKS_AND_TESTUTILS.md` — shared `Altinn.AccessManagement.TestUtils` library
   layout (Fixtures / Factories / Mocks / Models / Data / TestCertificates).
6. `WRITING_TESTS.md` — how to add a new test, naming convention, FluentAssertions
   usage, when to reach for a fixture vs a pure unit.
7. `COVERAGE.md` — `eng/testing/run-coverage.ps1`, `coverage-thresholds.json`,
   per-assembly ratchet.
8. `CI.md` — single-pass `dotnet-coverage collect -- dotnet test` model,
   `--ignore-exit-code 8`, parse-only threshold check.

Existing sibling docs untouched: `FLUENT_ASSERTIONS_GUIDELINES.md`,
`TEST_NAMING_CONVENTION.md`.

### 2. Folder restructure — overhaul record isolated

```
docs/testing/
├── README.md                                  (new entry point)
├── GETTING_STARTED.md
├── TEST_PROJECTS.md
├── FIXTURES.md
├── MOCKS_AND_TESTUTILS.md
├── WRITING_TESTS.md
├── COVERAGE.md
├── CI.md
├── FLUENT_ASSERTIONS_GUIDELINES.md
├── TEST_NAMING_CONVENTION.md
└── TESTING_INFRASTRUCTURE_OVERHAUL/
    ├── TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md   (renamed from root .md)
    └── steps/                                       (moved from root)
        ├── INDEX.md
        └── 1_* ... 60_*.md
```

Rationale: top level is now "how the test infrastructure works today"; the
`TESTING_INFRASTRUCTURE_OVERHAUL/` subfolder is "how we got here" and is safe to
skip for day-to-day contributors. History preserved via `git mv`.

### 3. Reference updates

All intra-doc links + C# source-comment paths re-pointed to the new folder
structure:

- 4 files inside the moved folder: parent link `../TESTING_INFRASTRUCTURE_OVERHAUL.md` → `../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`.
- 9 sibling docs in `docs/testing/`: prefix `steps/` → `TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/`; rename of the plan doc.
- 7 C# source files (controller test comments in AccessMgmt.Tests + one in
  Altinn.AccessManagement.Enduser.Api.Tests): `docs/testing/steps/` →
  `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/`.
- `INDEX.md` self-references updated (parent-link + description of its own path).

### 4. INDEX.md cleanup (performed in this step)

Two pre-existing structural issues fixed while the file was open:

- **Duplicate Step 51 row removed.** The ResourceRegistryMock cache-hit fix was
  listed twice in the step log table (immediately adjacent rows); kept the more
  detailed version, dropped the duplicate.
- **Missing `### Recommended Next Steps (priority order)` heading added**
  before the numbered list. The workflow section and the internal anchor
  `#recommended-next-steps-priority-order` at the bottom of the file were both
  referencing a heading that had been lost during an earlier edit.

## Verification

- `git grep` across the repo confirmed **zero** stale references outside the
  moved folder. Queries:
  `](steps/`, `` `steps/ ``, `](TESTING_INFRASTRUCTURE_OVERHAUL.md`, `docs/testing/steps/`.
- Structure listing confirms 10 top-level `.md` files + 1 subfolder in
  `docs/testing/`.
- No code changes → no test runs required. Coverage numbers unchanged.
- `run_build` not run (docs + comment-string changes only).

## Follow-up / recommended next steps

With the overhaul archived and a developer-facing doc suite in place, the next
highest-value work is the **fresh infrastructure audit** already queued under
priority 2 in `INDEX.md`:

- Re-measure assembly-level coverage — many numbers have shifted materially
  since the Step 12 baseline.
- Identify any remaining pure-logic targets missed.
- Assess whether `Host.Pipeline` / `Host.Database` / `Host.MassTransit` warrant
  dedicated test projects.
- Refresh the coverage-threshold enforcement list accordingly.
- Produce an updated audit doc + refreshed recommended-next-steps list.

## Files touched

- **New:** `docs/testing/{README,GETTING_STARTED,TEST_PROJECTS,FIXTURES,MOCKS_AND_TESTUTILS,WRITING_TESTS,COVERAGE,CI}.md` (8 files).
- **Renamed:** `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL.md` → `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md` (via `git mv`).
- **Moved:** `docs/testing/steps/` → `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/` (60 step docs + `INDEX.md`, via `git mv`).
- **Edited (links only):** 4 files inside moved folder, 9 sibling docs, 7 C# test files, `INDEX.md` (self-refs + cleanup described above).
- **New step doc:** this file.
