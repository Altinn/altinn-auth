# Sub-step 16.4b-continued — `ConsentControllerTestBFF` → `LegacyApiFixture`

**Status:** ✅ Complete. All 23 BFF tests migrated off `WebApplicationFixture`
and passing against `LegacyApiFixture`.

## Goal

Finish the priority-1 item flagged in Step 24
([AccessMgmt_WAF_Group_B_16_4b_Final_Consumers.md](AccessMgmt_WAF_Group_B_16_4b_Final_Consumers.md)):
migrate `ConsentControllerTestBFF` off the legacy `WebApplicationFixture` so
`WebApplicationFixture` has **no remaining test consumers** and can be retired
in 16.5.

## Approach — Option 1: per-test DB isolation

Of the two options identified in Step 24, Option 1 was chosen because it
mirrors the legacy `WebApplicationFixture` semantics exactly
(`PostgresServer.NewEFDatabase()` per `WithWebHostBuilder` invocation) and does
not require rewriting test bodies.

Implementation: the test class was converted from
`IClassFixture<WebApplicationFixture>` to `IAsyncLifetime`. Each xUnit test
instance (one per `[Fact]`) news up its own `LegacyApiFixture` in
`InitializeAsync`. Because `ApiFixture.InitializeAsync` calls
`EFPostgresFactory.Create()` — which clones a fresh database from the
`test_primary` template — every test gets its own clean DB cloned from the
shared migrated+seeded template. This reuses existing infrastructure; **no
new fixture type was introduced**.

## What changed

### `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/Bff/ConsentControllerTestBFF.cs`

- `: IClassFixture<WebApplicationFixture>` → `: IAsyncLifetime`.
- `_fixture` field retyped from `WebApplicationFactory<Program>` to
  `LegacyApiFixture` and marked `null!` (initialized in `InitializeAsync`).
- Constructor reduced to capturing `ITestOutputHelper`. DI overrides moved to
  `InitializeAsync`, applied via `_fixture.ConfigureServices(...)`:
  - `RemoveAll<IPublicSigningKeyProvider>()` + `SigningKeyResolverMock`
    (certificate-based flavour — matches the 16.4a recipe).
  - `RemoveAll<IPDP>()` + `PdpPermitMock` (legacy flavour — the
    `ApiFixture` default `PermitPdpMock` is the wrong shape for these tests).
  - All pre-existing per-class mocks: `PartiesClientMock`,
    `JwtCookiePostConfigureOptionsStub`, `ResourceRegistryClientMock`,
    `PolicyRetrievalPointMock`, `AltinnRolesClientMock`, `ProfileClientMock`,
    `Altinn2ConsentClientMock`.
- `InitializeAsync` awaits `_fixture.InitializeAsync()` then runs
  `SeedResources()`. `DisposeAsync` awaits `_fixture.DisposeAsync()`.

### `ElenaFjaerEntity.UserId` — the seed collision fix

The first green-build test run failed 23/23 with
`Npgsql.PostgresException: 23505: duplicate key value violates unique
constraint "ix_entity_userid"`, `Key (userid)=(20001337)`. Root cause:
`EFPostgresFactory`'s template DB applies `TestDataSeeds.Exec`, which seeds
`TestEntities.PersonOrjan` with `UserId = 20001337` —
the same UserId `ElenaFjaerEntity` claimed. `SeedResources()`'s
Id-based idempotency guard (`Any(e => e.Id == entity.Id)`) couldn't catch
the column collision. Under the legacy `WebApplicationFixture` this never
surfaced because that fixture's DB was not seeded with `TestEntities`.

Fix: `ElenaFjaerEntity.UserId` is now `null`. The BFF tests identify Elena
Fjær by her entity `Id` / party UUID
(`d5b861c8-8e3b-44cd-9952-5315e5990cf5`) through `PrincipalUtil.GetToken`'s
`userUuid` parameter — they never read the `UserId` column — so nulling it
is safe. A code comment documents the constraint.

No other BFF-defined entity collides with the template seed (verified by
`Select-String` over `TestEntities`/`TestData` for every
PersonIdentifier/OrganizationIdentifier/PartyId/UserId used by the BFF
test file).

## Verification

Branch: `feature/2842_Optimize_Test_Infrastructure_and_Performance`,
Podman Desktop 5.2.5, `postgres:16.1-alpine`.

| Class | Result | Time |
|---|---|---|
| `AccessMgmt.Tests.Controllers.Bff.ConsentControllerTestBFF` | **23 / 23 PASS** | 1.4 min |

Build: 0 errors (pre-existing StyleCop warnings unchanged).

## Impact on `WebApplicationFixture`

`ConsentControllerTestBFF` was the **last** test consumer of
`WebApplicationFixture`. The remaining references are:

- `src/.../Fixtures/WebApplicationFixture.cs` — the fixture definition itself.
- `src/.../Templates/ControllerTestTemplate.cs` — example/template file, not
  a real test.

→ **16.5 (retire `WebApplicationFixture`, `PostgresServer`,
`AcceptanceCriteriaComposer`, `Scenarios/*`) is now unblocked.**

Note: `PostgresFixture` is **out of scope for 16.5** — it still has four
active consumers (`ConnectionQueryTests`, `RequestServiceTests`,
`TranslationServiceTests`, `DeepTranslationExtensionsTests` and
`DatabaseTestTemplate`). Retiring it is a separate follow-up.

## Per-test-DB cost note

Each `[Fact]` now clones a fresh template DB. Observed ~3.5 s per test
amortized across the 23 tests (1.4 min total). If future multi-test-per-class
`LegacyApiFixture` consumers don't need per-test isolation they should stay
with the default class-scoped `IClassFixture<LegacyApiFixture>` pattern (used
by the 16.4a migrations) — per-test isolation should be applied only when
tests reuse hard-coded IDs or mutate shared state.
