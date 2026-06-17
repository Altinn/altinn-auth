# Test Architecture — Target Structure

Status: **largely complete** — Feature #3452's sub-tasks are implemented in PR #3456:
the project-local mock-catalog fixture, the no-DB tier, three cohort host collapses,
robust test-data paths, and a unified fixture convention.
Scope: the AccessManagement integration/web-app test suite. Authorization is
referenced as the pattern to copy — specifically its **project-local** fixture
(`AuthorizationApiFixture`), which bakes its own mock graph in rather than
centralising mocks in `TestUtils`.

## 1. Why

The suite's cost and flakiness are dominated by one number — **how many web-host
builds it does** (~71, at ~1.36 s each; DB clones are cheap at 230 ms, template is
a one-time ~9 s). The #3379 measurement proved this, and the incremental fixture
sharing there confirmed that ad-hoc cohort sharing plateaus, because the structure
fights it. This document is the structure those measurements point to.

## 2. Current state (measured)

| Fact | Value |
|---|---|
| `IClassFixture<ApiFixture>` classes (≈ host builds) | ~71 at baseline (now 65 after the cohort collapses) |
| Per-fixture host build (dominant cost) | ~1.36 s avg (12.9 s cold) |
| DB clone / template build | 230 ms / 9 s one-time |
| Classes calling `ConfigureServices` | 39 files |
| Distinct mock interfaces across *all* those calls (the catalog) | **15** (+ 4 `RemoveAll`) |
| API feature-flag axes | ~5 |
| Behavioral-override files (`RemoveAll<…>`, non-default behavior) | ~12 |
| Fixture files that never seed/query the DB | 19 (4 clearly mock the repo layer) |
| `ApiFixture` / `LegacyApiFixture` files | 52 / 8 |

## 3. Root cause

**The test class is the unit of everything** — host config, seed data, isolation,
and assertion scope are all bound to it. Every wall the incremental work hit is a
symptom:

1. **Under-provisioned base host → artificial DI divergence.** `ApiFixture`
   registers only auth stubs + a permit-PDP, so each class patches in whichever of
   the **same 15-mock catalog** it needs via `ConfigureServices`. The 39 calls are
   *subsets of one catalog*, not 39 configs — but each subset forces a unique host.
   **`AuthorizationApiFixture` already bakes its full mock graph in**, proving the
   divergence is avoidable.
2. **Grouping by API operation, not compatibility.** `Connections` is ~20 classes
   (`Get`/`Check`/`Add`/`Remove`/`Update`), each rebuilding the whole host.
3. **Class-coupled data + global assertions.** `EnsureSeedOnce<TSelf>` + exact /
   count assertions mean shared data breaks tests — sharing is hostile by
   construction.
4. **DB clone even when unused.** `InitializeAsync` always clones Postgres; ~19
   files mock the data layer.
5. **No single convention.** `ApiFixture`, `LegacyApiFixture` (8), scenario/E2E
   tests, and the mock-based Authorization fixture coexist.

**Headline (hypothesis): ~71 host builds looked like ~10–12 real profiles multiplied
out by per-class mock-patching and per-operation splitting. Measurement later showed
the per-class configs are more idiosyncratic than this — see the collapse outcome in §8.**

## 4. Principles

- **Decouple three axes:** host configuration, data, isolation.
- **Shared-by-default, divergence-by-exception.**
- **Own your data:** a test reads/writes only entities it uniquely owns and asserts
  only against them — so sharing is tolerant by construction.
- **Pay only for what you use:** no DB for tests that don't touch it.

## 5. Target architecture

**A. Host profiles (finite, named).** A **project-local** base fixture
(`AccessMgmtApiFixture : ApiFixture`, mirroring `AuthorizationApiFixture`) registers
the external-platform client catalog once — the mocks stay in the test project
beside their data, not centralised in `TestUtils`. Most classes then need zero
`ConfigureServices` and join one shared host. Genuine variation becomes a small
enumerated set of collection-fixture profiles:

| Profile | Driver | Approx. classes |
|---|---|---|
| `Default` (full mocks, permit PDP) | no flags, no override | ~40+ |
| `Altinn2RoleRevoke` (on/off) | feature flag | ~7 |
| `EnableRequestAssignmentResource` / `…Package` / `EnableEnduserMaskinportenAdminApi` | feature flags | ~3 each |
| PDP / resource-registry / http-context overrides | `RemoveAll<…>` behavior | ~12 spread over a few |

→ **target: ~71 host builds collapse to ~10–12** (the realised outcome was smaller — see the collapse outcome in §8).

**B. Data ownership.** Rich shared baseline template + each test creating entities
under **unique IDs** and asserting only against them. Additive seeds never collide;
exact assertions become safe; **read/write distinction dissolves** (a test that
mutates only its own rows can share too).

**C. Isolation by exception.** Shared host + owned data is the default. Only tests
that mutate *global* state or need a divergent host get a dedicated fixture.

**D. Two tiers.**
- **Web-app tier (no DB):** tests that mock the *entire* data layer — no Postgres
  clone, no DB connection (`ApiFixture.ProvisionsDatabase = false` via `NoDbApiFixture`).
  Measured caveat: "mocks the data layer" is necessary but **not sufficient** — most
  controller endpoints still reach Postgres via party / context resolution even with the
  named repositories mocked, so each candidate must be verified by running it. The
  genuinely DB-less set is small (so far only `Altinn2RightsControllerTest`).
- **DB-integration tier:** real `AppDbContext` + migrations.

**E. One convention.** Every web-app fixture descends from `ApiFixture` (the profile
fixtures `AccessMgmt` / `Legacy` / `Consent` / `Rights` / `NoDb` are subclasses), and the
scenario / E2E tests use it too — so this is already achieved. `LegacyApiFixture` is
*retained* as the full-schema (EF + Yuniql) profile, not a separate convention: it is
`ConsentApiFixture`'s base and is used by the Dapper-backed consent / resource tests, so
deleting it would unwind structure for no gain.

## 6. Before / after (author-facing)

```csharp
// BEFORE — own host, patches mocks, seeds + asserts globally
public class GetInstanceRights : IClassFixture<ApiFixture> {
    public GetInstanceRights(ApiFixture f) {
        f.ConfigureServices(s => {                 // forces a unique host
            s.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            s.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
            s.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
        });
        f.EnsureSeedOnce<GetInstanceRights>(db => /* shared entities */);
    }
}

// AFTER — shared host (catalog pre-registered), owns its data, scoped assert
[Collection(DefaultHost.Name)]                     // one host for the whole profile
public class GetInstanceRights(DefaultHost host) {
    [Fact] public async Task ... {
        var party = host.NewOwnedOrg();            // unique id; no collision possible
        // seed under `party`; assert only on `party`'s results
    }
}
```

## 7. Expected impact

- **Host builds ~71 → ~10–12** — a structural ~6× cut (vs ~9 recovered by cohort
  sharing).
- **Flakiness:** the seed-interference failure class disappears; #3376 pressure drops.
- **Parallelism:** with far fewer host builds, the single-Postgres-container plateau
  stops being the ceiling.
- **DB-less tier** removes the clone/provision for the few genuinely DB-less classes
  (measured ~1 — most endpoints still hit the DB).

## 8. Migration plan (sized → Tasks)

Each phase is independently shippable, kept green, and measured with `FixtureTiming`.
Sizing is files/classes touched in AccessManagement.

| # | Phase | Scope (sized) | Win | Risk | Task granularity |
|---|---|---|---|---|---|
| 1 | Enumerate profiles | done (this doc): ~10–12 | unblocks the rest | — | folded into the Feature |
| 2 | **Provision the `Default` host** — bake the external-client catalog into a project-local base fixture (`AccessMgmtApiFixture`; mocks stay in the test project, not `TestUtils`); delete now-redundant `ConfigureServices` | 39 files | collapses ~40+ classes → 1 host (largest single win) | low — additive registration, validate per project | 1 Task (1 PR) |
| 3 | **Owned-data + scoped assertions** — replace `EnsureSeedOnce<TSelf>` global seeds/asserts with per-test owned entities | 33 seeding files | removes condition-4 fragility; enables write-sharing | med — most careful; per-controller | 3–4 Tasks (per project/controller cluster) |
| 4 | **DB-less web-app tier** — no-DB fixture for mock-the-data-layer tests | ~1 verified (most candidates secretly hit the DB) | drops clone/provision for the few truly DB-less | low | done (#3458) |
| 5 | **Define profiles as collection fixtures; convert classes to `[Collection]`** — *supersedes #3449's per-controller cohorts* | all ~71 | realizes the 71 → ~12 collapse | low after 2–3 | 2 Tasks (per project) |
| 6 | **One convention (#3461)** — every fixture descends from `ApiFixture` and scenario/E2E use it; `LegacyApiFixture` retained as the full-schema profile (`ConsentApiFixture`'s base) rather than deleted | — | one convention | — | ✅ done |

Suggested Feature → ~8–9 Tasks. Order matters: **2 → 3 → 4/5 → 6** (provision the
host first; then make data shareable; then collapse onto profiles).

Status: Phase 1 and Phase 2a are done — Phase 2a is the project-local
`AccessMgmtApiFixture` catalog (#3454), shipped in PR #3456 (972 integration tests
green). Phase 2b (#3453) is in progress. Remaining: #3459 (owned data), #3458
(DB-less tier), #3460 (profiles), #3461 (retire `LegacyApiFixture`); plus #3462
(robust test-data paths).

## Profile axes (measured in AccessMgmt.Tests — input for #3460)

With the external-client catalog defaulted (#3454), the per-class `ConfigureServices`
that remain are either **data-layer mocks** or **profile-axis overrides**:

- **Data-layer mocks** (`PolicyRetrievalPointMock`, `DelegationMetadataRepositoryMock`,
  `PolicyFactoryMock`) **cannot be defaulted** — 4 DB-integration classes
  (`PartyControllerTests`, `PolicyInformationPointResourcesAndInstancesTest`,
  `PolicyInformationPointRolesAndAccessPackagesTest`, `MaskinportenSupplierServiceTests`)
  use the *real* repositories against the seeded Postgres. Registering a repo mock is the
  marker of a **mock-data-layer** test, which is also the DB-less-tier signal (#3458).
  Caveat: the Legacy/Dapper consent tests use the DB without `EnsureSeedOnce`, so the
  DB-less set needs per-class verification, not just the seed signal.
- **Profile axes** — the genuinely varying registrations, which become the small set of
  named profiles in #3460:
  - `IPDP`: `PermitPdpMock` (base default) / `PdpPermitMock` (9 classes) / `PepWithPDPAuthorizationMock` (2).
  - `IPublicSigningKeyProvider`: base `PublicSigningKeyProviderMock` / `SigningKeyResolverMock` (12 classes, issuer-cert tokens).
  - `IHttpContextAccessor`: default / `MutableHttpContextAccessor` (2 classes).

**Sequencing insight (updated after #3458):** in principle DB-less classes could collapse
without owned data, but the genuinely DB-less set turned out to be ~1 class — almost every
controller endpoint reaches Postgres via party / context resolution. So the host-build
collapse (#3460) depends on the owned-data work (#3459) for nearly all classes; #3459 is
the real prerequisite.

**Progress — first collapse (#3459 / #3460).** The PolicyInformationPoint DB cohort
(`…ResourcesAndInstancesTest` + `…RolesAndAccessPackagesTest`) now shares one host via
`PolicyInformationPointDbCollection` — 2 host builds → 1, suite green. Finding: both were
*already* owned-data-ready (scoped `Assert.Contains` / `Empty`, disjoint seed IDs), so no
data rewrite was needed — the only work was confirming host-config compatibility (moving
one class to `AccessMgmtApiFixture`; its endpoint does not use the now-mocked Parties /
Profile). So for well-written tests #3459 is often already satisfied, and the real
per-cohort blocker is shared host config (the profile axes), not data ownership. Classes
with global / exact-count assertions or colliding fixed IDs still need the owned-data
conversion.

**Remaining cohorts (config-cluster map, measured).** After the catalog default, each class's
residual `ConfigureServices` is data-layer mocks + a PDP / signing / http-context variant. The
clusters, and why each is a dedicated effort (not a quick win):
- *Consent* — ✅ **done.** The 2 `IClassFixture` classes (`ConsentControllerTestEnterprise`,
  Maskinporten `ConsentControllerTest`) now share `ConsentApiFixture` via `ConsentDbCollection`
  (2 hosts → 1). The shared `MockParyRepositoryPopulator` Moq is baked into the fixture
  (Setup-only, so a single instance is safe) and inserted request IDs were already disjoint
  (Enterprise random `Guid.CreateVersion7()` vs Maskinporten fixed) — no owned-data rewrite.
  The other 2 classes (BFF, FetchStatusChanges) build a fresh fixture per test for isolation
  and stay separate.
- *Rights / Delegation* — ✅ **done.** `AppsInstanceDelegationControllerTest` + the main
  `RightsInternalControllerTest` share `RightsApiFixture` via `RightsDbCollection` (2 hosts → 1).
  All baked services are stateless mocks; no owned-data rewrite (AppsInstance seeds additively
  under its own IDs, RightsInternal reads baseline). The `WithPdpMock` sibling (needs an extra
  `PepWithPDPAuthorizationMock`) keeps its own fixture.
- *Maskinporten* (`MaskinportenSchema` ×2 siblings): split by PDP (PepWith vs PdpPermit) +
  `MutableHttpContextAccessor` — two distinct profiles, not mergeable.
- *Singletons* (`PartyControllerTests`, `ResourceController`, `V2Resource`): each a unique
  minimal config — no peer to share with.

**Collapse outcome (measured).** Three cleanly-shareable cohorts collapsed — PolicyInfoPoint,
Consent, Rights/Delegation (6 classes → 3 hosts). The rest do not form shareable groups: the
remaining classes have idiosyncratic configs (unique PDP / data-layer / http-context
combinations) or require per-test isolation, so they stay on their own fixtures. **The optimistic
"71 → ~12" target does not materialise** — the per-class configs are genuinely diverse, not
incidental variations of a few profiles. The realised wins are the catalog default (#3454), the
no-DB tier (#3458, ~1 class), and these three cohort collapses; further reduction would require
aggressive superset-baking that risks changing per-class behaviour (low yield, rising risk), so
#3459 / #3460 stop here. The realised count is 65 hosts for `AccessMgmt.Tests`. There is no CI
guard on the host-build count: a per-assembly check existed briefly and was removed, since we do
not want CI to fail just because a test is inefficient. `FixtureTiming` still measures the counts
for anyone optimising locally.

## 9. Risks & open items

- **Scope:** touches most test classes — phase per project, stay green.
- **Discipline:** owned-data + no-per-class-DI must be conventions (review/lint) or
  it rots back.
- **Open numbers to confirm during Phase 1/2:** exact profile count (≤~12); the
  DB-less set (measured small — ~1; mock-data-layer is necessary-not-sufficient); any mock needing *per-test behavior*
  (vs one impl) — those stay isolated.
- **Relationship to PR #3456:** that PR's measurement, `FixtureTiming`, the two
  validated cohorts, and Phase 2a (the `AccessMgmtApiFixture` catalog) ship there.
  Phase 5 supersedes the per-controller rollout (#3449) with profile-based collections.
- **Project-local mocks, not `TestUtils`:** the base catalog lives in a project-local
  fixture (`AccessMgmtApiFixture`, the `AuthorizationApiFixture` pattern). Moving the
  AccessMgmt mocks up into `TestUtils` was rejected — shared test data (`Data/Parties`
  is also read by `TestDataUtil`), ~14 registration sites, and fragile cross-assembly
  `..` path navigation (see #3462) make centralisation costly for no benefit.

## Related

- [TEST_SETUP_TIMING.md](TEST_SETUP_TIMING.md) — the measurements this is built on.
- [CI.md](CI.md), [../SONARCLOUD.md](../SONARCLOUD.md).
