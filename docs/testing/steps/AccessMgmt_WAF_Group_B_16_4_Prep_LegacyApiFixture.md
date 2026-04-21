# Sub-step 16.4-prep — `LegacyApiFixture` plumbing for the full production schema

## Goal

Unblock sub-steps 16.4a/b by providing a fixture that stands up the **full**
production database schema — both the EF `dbo`/`dbo_history` schemas
provisioned by `EFPostgresFactory` and the Yuniql `accessmanagement.*`,
`consent.*`, `delegation.*` schemas (plus enum types) provisioned by the
production migration pipeline. This was the priority-1 item in
`docs/testing/steps/INDEX.md`, recommended as **Option 2** in the
[16.4 investigation doc](AccessMgmt_WAF_Group_B_Scenarios_16_4_Investigation.md).

Scope was strictly plumbing-only — no test migration. The six remaining
`WebApplicationFixture` consumers (16.4a/b) will move to `LegacyApiFixture`
in follow-up sub-steps.

## What changed

### New file — `LegacyApiFixture` (in `AccessMgmt.Tests/Fixtures/`)

`LegacyApiFixture : ApiFixture` is a thin subclass that:

- Loads `appsettings.test.json` from the test bin directory (required by the
  legacy tests for `AzureStorageConfiguration`, `GeneralSettings`, etc.).
- Sets `PostgreSQLSettings:EnableDBConnection=true` and
  `RunIntegrationTests=true` via `WithInMemoryAppsettings`.

Those two flags are the exact combination the legacy `WebApplicationFixture`
used to nudge the production `AccessManagementHost.ConfigurePostgreSqlConfiguration`
path into flipping on `Altinn:Npgsql:<service>:Migrate:Enabled=true`, which in
turn triggers the production Yuniql migration pipeline (`AddYuniqlMigrations`
from `Altinn.Authorization.ServiceDefaults.Npgsql.Yuniql`) during host
startup.

### New file — `LegacyApiFixtureSmokeTest` (in `AccessMgmt.Tests/Fixtures/`)

A single `IClassFixture<LegacyApiFixture>` test that:

1. Resolves `IResourceMetadataRepository` (the Dapper-backed
   `ResourceMetadataRepo`) from the test host's DI container.
2. Calls `InsertAccessManagementResource` — a `INSERT ... RETURNING` against
   `accessmanagement.resource`, the exact table whose absence caused
   `V2ResourceControllerTest` to fail in the 16.4 investigation
   (`relation "accessmanagement.resource" does not exist`).
3. Asserts the insert round-trips (non-null row, matching registry id, non-zero
   generated `ResourceId`).

The assertion deliberately does **not** check `ResourceType`: the repo writes
`.ToString().ToLower()` but reads back with case-sensitive `Enum.TryParse`, so
the round-tripped value is always `Default`. That is pre-existing repo
behaviour and unrelated to the fixture plumbing under test.

## Design notes

### Why Option 2 (per-test Yuniql) rather than Option 1 (template-level Yuniql)

The 16.4 investigation's preferred option was "run Yuniql at the template
level inside `EFPostgresFactory`". This sub-step delivers the **recommended
Option 2** from the same doc — `LegacyApiFixture : ApiFixture` overlaying
Yuniql on top of EF — because:

- It keeps `ApiFixture` lean for the large Group A consumer base: those tests
  do **not** pay the Yuniql migration cost.
- It requires zero changes to `Altinn.AccessManagement.TestUtils`
  (`EFPostgresFactory` is untouched).
- It reuses the production Yuniql pipeline (`AddYuniqlMigrations` via
  `ManifestEmbeddedFileProvider`) — no need to copy the `Migration` folder to
  the test output directory or stand up a standalone Yuniql invocation.
- Migration SQL evolves with production code automatically — no drift risk
  between a bespoke test-side Yuniql invocation and the real one.

If the per-test Yuniql cost ever becomes a bottleneck (today it's ~15 s for
the full legacy startup on the smoke test run, dominated by the first-time
container+template bootstrap), the template-level alternative can be adopted
later without changing this fixture's public surface.

### What `LegacyApiFixture` deliberately does NOT do

- It does **not** register any of the legacy scenario-based mocks
  (`MockContext`, `PartiesClientMock`, etc.) that
  `WebApplicationFixture.ConfigureHostBuilderWithScenarios` wires up. The
  migrating test classes in 16.4a/b will continue to set up their own DI
  overrides via `ConfigureServices(...)` — that is the canonical pattern for
  `ApiFixture` consumers and preserves per-class isolation.
- It does **not** set `FeatureManagement:AccessManagement.MigrationDbEf=true`
  (which `WebApplicationFixture` used). EF migrations are already applied by
  `EFPostgresFactory` on the template, so re-running them per test would be a
  no-op; leaving the flag unset keeps behaviour aligned with the default
  `ApiFixture` path.

## Verification

- `dotnet build` on the full solution: **Build successful** (0 errors, 0
  warnings introduced).
- `LegacyApiFixtureSmokeTest.InsertAccessManagementResource_Succeeds_WhenYuniqlSchemaIsProvisioned`
  under Podman: **Passed** in ~15.8 s (first run; includes one-time
  container + EF template bootstrap).
- Pre-plumbing sanity check (from the 16.4 investigation): running the same
  repo insertion under a plain `ApiFixture` fails with
  `relation "accessmanagement.resource" does not exist`. The smoke test
  passing confirms the Yuniql schema is now provisioned in the per-test
  database.

## Files touched

- **Added:**
  - `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/LegacyApiFixture.cs`
  - `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/LegacyApiFixtureSmokeTest.cs`

No production code changed. `ApiFixture` and `EFPostgresFactory` are
untouched.

## Coverage impact

None — this sub-step adds test infrastructure and a trivial smoke test, not
code exercising production paths. Coverage numbers in `INDEX.md` are
unchanged.

## Follow-ups (unblocked)

- **16.4a:** Migrate `V2ResourceControllerTest`, `ConsentControllerTestEnterprise`,
  and MaskinPorten `ConsentControllerTest` from `WebApplicationFixture` →
  `LegacyApiFixture`. These were the three classes that tripped the
  investigation's runtime failures.
- **16.4b:** Migrate `ConsentControllerTestBFF` (needs `EnsureSeedOnce<T>` for
  `SeedResources()`), `V2MaskinportenSchemaControllerTest`, and
  `V2RightsInternalControllerTest` (all `[Skip]`ped today — confirm skip-state
  is still desired before porting).
- **16.5:** Retire `WebApplicationFixture`, `PostgresFixture`,
  `PostgresServer`, `AcceptanceCriteriaComposer`, and `Scenarios/*` once no
  consumers remain.
