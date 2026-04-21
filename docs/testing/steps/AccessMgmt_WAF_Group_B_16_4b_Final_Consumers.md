# Sub-step 16.4b — Final `WebApplicationFixture` consumers

> **Update (Step 25):** The `ConsentControllerTestBFF` migration deferred here
> has landed. See
> [AccessMgmt_WAF_16_4b_Continued_BFF_Migration.md](AccessMgmt_WAF_16_4b_Continued_BFF_Migration.md).

**Status:** Partially delivered. Two skipped consumers deleted;
`ConsentControllerTestBFF` migration **blocked** on per-test DB isolation gap in
`LegacyApiFixture` and deferred to a dedicated follow-up.

## Goal

Finish migrating the last three `WebApplicationFixture` consumers to
`LegacyApiFixture` so `WebApplicationFixture`, `PostgresFixture`,
`PostgresServer`, `AcceptanceCriteriaComposer`, and `Scenarios/*` can be retired
in 16.5.

Targets:

- `ConsentControllerTestBFF`
- `V2MaskinportenSchemaControllerTest` (100% `[Skip]`ped)
- `V2RightsInternalControllerTest` (100% `[Skip]`ped)

## What changed

### Deleted

- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/V2MaskinportenSchemaControllerTest.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Controllers/V2RightsInternalControllerTest.cs`

Both files had every test marked `[Fact(Skip = "...")]` and leaned on
`AcceptanceCriteriaComposer` + `DelegationScenarios`, which are slated for
retirement in 16.5. They added no coverage and were actively blocking WAF
retirement. If owners want them revived, the code is preserved in git history
and should land on `LegacyApiFixture` using the 16.4a recipe.

### Not changed (deferred)

- `ConsentControllerTestBFF` — still on `WebApplicationFixture`.
  A full migration was attempted on this branch, built clean, but **20 of 23
  tests failed** at runtime (baseline on `main`: 23/23 pass). The migration
  was reverted so the branch stays green.

## Why the BFF migration was reverted

Root cause is structural, not a DI-parity issue:

- `WebApplicationFixture.ConfigureWebHost` calls
  `PostgresServer.NewEFDatabase()` **on every `WithWebHostBuilder` invocation**.
  Each test's constructor re-derives the factory (`_fixture = fixture.WithWebHostBuilder(...)`),
  so every test runs against a **fresh, empty EF database**. The `IClassFixture`
  only keeps the shared PostgreSQL container process alive.
- `LegacyApiFixture` is a single `ApiFixture` per test class. The host (and its
  DB) is built once and reused for all 23 tests in the class. `EnsureSeedOnce<T>`
  is explicitly keyed to run once per class.
- `ConsentControllerTestBFF` tests assume a clean DB per test: they reuse
  hard-coded `requestId` GUIDs (e.g. `e2071c55-6adf-487b-af05-9198a230ed44`
  appears in 5+ tests) and re-insert the same entity/resource rows.
  Under `LegacyApiFixture` this produces:
  - `duplicate key value violates unique constraint "ix_entity_userid"` during
    seed after the first run,
  - `InvalidOperationException: Consent request with ID ... not found or
    already updated` on accept/reject chains (state from a prior test already
    flipped the row to `Accepted`/`Rejected`),
  - `Forbidden` on flows whose policy evaluation depends on prior state,
  - `ListRequests_One_*` assertions failing because the list contains
    accumulated rows from earlier tests (e.g. 6 found, 1 expected).

Fixing this requires either:

1. **Per-test DB isolation in `LegacyApiFixture`** (e.g. `IAsyncLifetime` per
   test class combined with a transaction-per-test wrapper, or adopting the
   `NewEFDatabase()` pattern via `WithWebHostBuilder` — with the caveat that
   `ApiFixture.ConfigureServices` seals after first `Services` access), **or**
2. **Rewriting all 23 BFF tests** to use unique request/entity IDs and clean
   up inserted state.

Both options exceed the scope of 16.4a's "mechanical port" recipe and deserve
their own step.

## Verification

- Build: ✅ `run_build` green after the revert + deletions.
- Baseline test run on `main` (WAF retained):
  `AccessMgmt.Tests.Controllers.Bff.ConsentControllerTestBFF` → 23/23 passed.
- Post-revert branch state: working tree has only the two deletions staged;
  `ConsentControllerTestBFF.cs` is unchanged from `HEAD`.

## Follow-up: new Recommended Next Step

A new priority item is added to `INDEX.md`:

> **16.4b-continued** — Migrate `ConsentControllerTestBFF` to a fixture that
> provides per-test DB isolation. Two viable approaches:
> 1. Extend `LegacyApiFixture` (or introduce a `LegacyPerTestApiFixture`) with
>    a per-test DB allocation/reset hook, wired as an `IAsyncLifetime`
>    collection fixture or via a transaction-rollback TestScope.
> 2. Rewrite `ConsentControllerTestBFF` tests to generate unique requestIds
>    per test (e.g. `Guid.CreateVersion7()`) and scope shared entity inserts
>    to avoid collisions across runs.
>
> Option 1 is the more reusable investment and is likely what any future
> multi-test-per-class `LegacyApiFixture` consumer will need.

Until 16.4b-continued lands, `WebApplicationFixture`, `PostgresFixture`,
`PostgresServer`, `AcceptanceCriteriaComposer`, and `Scenarios/*` **cannot be
retired** (blocker for 16.5).
