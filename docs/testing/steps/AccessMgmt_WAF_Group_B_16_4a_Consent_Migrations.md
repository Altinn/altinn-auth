# Sub-step 16.4a — Migrate three `WebApplicationFixture` consumers to `LegacyApiFixture`

## Goal

Execute the priority-1 item from `docs/testing/steps/INDEX.md`: migrate the three
`WebApplicationFixture` consumers that the [16.4 investigation](AccessMgmt_WAF_Group_B_Scenarios_16_4_Investigation.md)
attempted and had to revert, now that
[Step 22](AccessMgmt_WAF_Group_B_16_4_Prep_LegacyApiFixture.md) has provided
`LegacyApiFixture` — the full-schema fixture they need.

Targets:

1. `V2ResourceControllerTest`
2. `ConsentControllerTestEnterprise`
3. `ConsentControllerTest` (MaskinPorten)

## What changed

### `V2ResourceControllerTest.cs`

- `IClassFixture<WebApplicationFixture>` → `IClassFixture<LegacyApiFixture>`.
- `AcceptanceCriteriaComposer`/`TheoryData` rewritten as a single direct HTTP
  `[Fact] POST_UpsertResource` since the sole row was a no-op DB assertion.
- DI overrides now applied via `_fixture.ConfigureServices(...)` (sealed-once
  on first `CreateClient()`):
  - `RemoveAll<IPublicSigningKeyProvider>()` + `SigningKeyResolverMock`.

### `ConsentControllerTestEnterprise.cs` and `MaskinPorten/ConsentControllerTest.cs`

- `IClassFixture<WebApplicationFixture>` → `IClassFixture<LegacyApiFixture>`;
  `_fixture` field retyped as `ApiFixture` (base type).
- `fixture.WithWebHostBuilder(b => b.ConfigureTestServices(...))` replaced
  with `fixture.ConfigureServices(...)`.
- DI override matrix re-applied from the reverted 16.4 investigation attempt:
  - `RemoveAll<IPublicSigningKeyProvider>()` + `SigningKeyResolverMock`
    (certificate-based flavour the legacy tests require — the ApiFixture
    default `PublicSigningKeyProviderMock` is the wrong shape).
  - `RemoveAll<IPDP>()` + `PdpPermitMock` (legacy flavour — the ApiFixture
    default `PermitPdpMock` is the wrong shape).
  - Plus the existing per-class mocks (parties, roles, registry, PRP, A2
    consent, profile, JWT cookie post-configure stub).

## Duplicate-requestId fix in `MaskinPorten/ConsentControllerTest.cs`

Two tests in the class both seeded `consent.consentrequest` with the same
`requestId e2071c55-6adf-487b-af05-9198a230ed44`:

- `GetConsent_CreatedExpired_BadRequest`
- `GetConsent_Created_BadRequest`

`ConsentRepository.CreateRequest` is a plain `INSERT INTO consent.consentrequest`
with no `ON CONFLICT` (see `ConsentRepository.cs` line 100). With the per-class
`LegacyApiFixture` DB cloned from a shared template and tests running
sequentially within the class, the second insert failed with a PK violation →
`500` instead of the expected `400 BadRequest`. The WAF-based baseline happened
to mask the collision, but the collision itself is a latent bug in the test
file.

Fix: switched `GetConsent_Created_BadRequest` to the already-existing data file
`Data/Consent/consent_request_e2071c55-6adf-487b-af05-9198a230ed46.json` (same
payload, `portalViewMode` differs only). No production code changes; no new
data file needed.

## Verification

All runs on branch `feature/2842_Optimize_Test_Infrastructure_and_Performance`,
under Podman Desktop 5.2.5 (postgres:16.1-alpine):

| Class | Result | Time |
|---|---|---|
| `V2ResourceControllerTest` | 1 / 1 PASS | 16.7s |
| `ConsentControllerTestEnterprise` | 18 / 18 PASS | 13.3s |
| `MaskinPorten.ConsentControllerTest` | 7 / 7 PASS | 30.8s |

Build: 0 errors (706 pre-existing StyleCop warnings, unchanged).

## Follow-ups

- **16.4b:** `ConsentControllerTestBFF` (needs `EnsureSeedOnce<T>` for
  `SeedResources()`), `V2MaskinportenSchemaControllerTest`,
  `V2RightsInternalControllerTest`. All three are currently `[Skip]`ped —
  confirm skip-state is still desired before porting.
- **16.5:** retire `WebApplicationFixture`, `PostgresFixture`,
  `PostgresServer`, `AcceptanceCriteriaComposer`, `Scenarios/*` once no
  consumers remain.
