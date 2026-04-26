# Sub-step 16.4 — Group B scenario-based consumers: blocker investigation

## Goal

Execute sub-step 16.4 of Step 16 (AccessMgmt.Tests WAF consolidation, Phase 2.2) —
migrate the remaining six `WebApplicationFixture` consumers onto the canonical
`ApiFixture`:

- `V2ResourceControllerTest`
- `ConsentControllerTestEnterprise` (Enterprise channel)
- `ConsentControllerTest` (MaskinPorten channel)
- `ConsentControllerTestBFF` (BFF channel)
- `V2MaskinportenSchemaControllerTest` (all `[Skip]`ped, uses `DelegationScenarios`)
- `V2RightsInternalControllerTest` (all `[Skip]`ped, uses `DelegationScenarios`)

Following the `16.2a/16.2b` precedent, the sub-step was scoped down to `16.4a` —
the three classes that do **not** use `DelegationScenarios` or per-test EF seeding:
`V2ResourceControllerTest`, `ConsentControllerTestEnterprise`,
`ConsentControllerTest` (MaskinPorten).

## What was attempted

Applied the canonical migration recipe (see
[AccessMgmt_WAF_Consolidation_Plan_and_POC.md](AccessMgmt_WAF_Consolidation_Plan_and_POC.md))
to the three classes:

- Swapped `IClassFixture<WebApplicationFixture>` → `IClassFixture<ApiFixture>`.
- Moved DI overrides from `fixture.WithWebHostBuilder(...).ConfigureTestServices(...)`
  into `fixture.ConfigureServices(...)` in the constructor.
- Added `services.RemoveAll<IPublicSigningKeyProvider>()` + `SigningKeyResolverMock`
  (required because V2/platform-token tests sign with `{issuer}-org.pfx`, not the
  static `TestTokenGenerator.SigningKey` used by ApiFixture's default
  `PublicSigningKeyProviderMock`).
- Added `services.RemoveAll<IPDP>()` + `PdpPermitMock` (legacy mock flavour).
- Replaced `WithWebHostBuilder` + `ConfigureTestServices` with
  `fixture.WithAppsettings(b => b.AddJsonFile("appsettings.test.json", ...))` +
  `fixture.ConfigureServices(...)`.

Code compiled cleanly. Tests then ran under Podman.

## Blocker

All three migrated tests failed at runtime with database schema errors:

1. **`V2ResourceControllerTest.POST_UpsertResource`** →
   `relation "accessmanagement.resource" does not exist` (SqlState 42P01)
2. **`ConsentControllerTestEnterprise.*`** →
   `System.InvalidCastException: Writing values of ...ConsentRequestStatusType is
   not supported...  ---> ArgumentException: A PostgreSQL type with the name
   'consent.status_type' was not found in the current database info`
3. **`ConsentControllerTest` (MaskinPorten)** — same `consent.status_type`
   failure as Enterprise.

### Root cause

`ApiFixture.InitializeAsync` uses `EFPostgresFactory`, which provisions the test
PostgreSQL database by running **only** the EF Core migrations
(`AppDbContext.Database.MigrateAsync()` + `TestDataSeeds.Exec`). The EF
migrations cover the new `dbo` / `dbo_history` schemas but **not** the legacy
Yuniql schemas (`accessmanagement.*`, `consent.*`, `delegation.*`) that the
still-extant Dapper-based repositories (`ResourceMetadataRepo`,
`ConsentRepository`, `DelegationMetadataRepo`) and the `MapEnum` bindings in
`PersistenceDependencyInjectionExtensions.AddDatabase` depend on.

The legacy `WebApplicationFixture` avoided this by (a) using
`PostgresServer.NewEFDatabase()` (which only creates `dbo` schemas but leaves
Yuniql disabled), and (b) setting
`PostgreSQLSettings:EnableDBConnection=true` + `RunIntegrationTests=true` in
its in-memory config, which nudges the Yuniql pipeline — but the three
consent/resource tests under `WebApplicationFixture` actually rely on Yuniql
schema + enum mappings being **provisioned at container startup**, not just
enabled. In the legacy setup this was achieved through the shared container's
lifecycle plus the application's own migration-on-start behaviour when
`MigrationDbEf=true` is left off — i.e. the production code path ran both
Yuniql and EF migrations against the per-test database.

The mechanical WAF → ApiFixture swap therefore cannot work in isolation for
any of the six Group B scenario-based consumers; they all exercise code paths
that require the Yuniql schema + enum types to exist, which ApiFixture
currently does not provision.

## Decision

**Revert** the three in-progress migrations and keep `WebApplicationFixture`
as the host for these six classes until the underlying schema-provisioning
gap is closed.

This means sub-step 16.4 is **blocked on infrastructure work**, not on
mechanical migration effort. The correct next actionable item is a new
sub-step (labelled `16.4-prep` below) that teaches `ApiFixture`
(or a sibling fixture) to stand up the full production schema — Yuniql +
EF — so the mechanical migration recipe becomes viable for Group B.

## Proposed follow-up: Sub-step 16.4-prep — provision full production schema in ApiFixture

Options, in rough order of preference:

1. **Run Yuniql + EnumMap during `EFPostgresFactory.Create()` template
   bootstrap.** Reuse the same `AddAccessManagementPersistence` /
   `AddAltinnPostgresDataSource` pipeline that production uses, so the
   template database has both the EF `dbo` schema and the Yuniql
   `accessmanagement.*` / `consent.*` / `delegation.*` schemas + enum types.
   Every cloned per-test database then has everything it needs.
2. **Introduce a `LegacyApiFixture : ApiFixture`** that overlays Yuniql
   provisioning on top of `ApiFixture`'s EF-only setup. Migrate Group B
   scenario-based consumers onto this variant; keep `ApiFixture` lean for
   EF-only tests. This keeps the pure path fast while unblocking the legacy
   path.
3. **Port the three repositories (`ResourceMetadataRepo`, `ConsentRepository`,
   `DelegationMetadataRepo`) to EF.** Largest scope — not suitable for this
   sub-step; tracked separately under the broader legacy-retirement plan.

Option 1 is cheapest per migrated test class but slows every `ApiFixture`
consumer (~all the Group A tests). Option 2 is the recommended path: it
isolates the cost to the tests that need it.

## Files touched

None — investigation only. The three experimental migrations were reverted:

```
git checkout -- \
  src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/V2ResourceControllerTest.cs \
  src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/Enterprise/ConsentControllerTestEnterprise.cs \
  src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/MaskinPorten/ConsentControllerTest.cs
```

## Verification

- `dotnet build` on `AccessMgmt.Tests.csproj` before revert: **0 errors**
  (migrations compiled cleanly).
- `AccessMgmt.Tests.exe -class "*V2ResourceControllerTest"` under Podman:
  **1 Failed** with `relation "accessmanagement.resource" does not exist`.
- `AccessMgmt.Tests.exe -method "*ConsentControllerTestEnterprise.*"` under
  Podman: **18 Failed** with
  `A PostgreSQL type with the name 'consent.status_type' was not found`.
- Post-revert `git status --short`: clean (all three files reverted).

## Follow-ups

- **16.4-prep (new priority 1):** Implement Option 2 above —
  `LegacyApiFixture` or equivalent — so `ApiFixture`'s successors can host
  tests that require the Yuniql schema. Owner: whoever picks up the next
  priority item.
- **16.4a/b (unblocked by 16.4-prep):** Resume the mechanical migration of the
  six remaining `WebApplicationFixture` consumers once the full-schema
  fixture is available.
- **16.5 (depends on 16.4):** Retire `WebApplicationFixture`, `PostgresFixture`,
  `PostgresServer`, `AcceptanceCriteriaComposer`, and `Scenarios/*` once no
  consumers remain.
