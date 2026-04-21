# Sub-step 16.5 — Retire `WebApplicationFixture` & legacy scenario harness

**Status:** ✅ Complete.

**Plan Phase:** 2.2 (final).

## Goal

Now that Step 25 moved the last consumer (`ConsentControllerTestBFF`) onto
`LegacyApiFixture`, delete the obsolete legacy test harness:

- `AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs`
- `AccessMgmt.Tests/Scenarios/*` (`Scenario.cs`, `DelegationScenarios.cs`,
  `TokenScenario.cs`)
- `AccessMgmt.Tests/AcceptanceCriteriaComposer.cs`
- `AccessMgmt.Tests/Templates/ControllerTestTemplate.cs`

## What changed

### Deleted files

| File | Reason |
|---|---|
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs` | Last consumer migrated in Step 25. |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Scenarios/Scenario.cs` | Only referenced by `AcceptanceCriteriaComposer` / legacy scenario types. |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Scenarios/DelegationScenarios.cs` | Legacy delegation seed composer; no remaining callers. |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Scenarios/TokenScenario.cs` | Legacy token seed composer; no remaining callers. |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AcceptanceCriteriaComposer.cs` | Paired with `WebApplicationFixture`; no consumers. |
| `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Templates/ControllerTestTemplate.cs` | Example template for the retired harness; not a real test. |

The now-empty `Scenarios/` directory was removed. `Templates/` was kept
because `DatabaseTestTemplate.cs` (tied to the out-of-scope `PostgresFixture`)
still lives there.

The `.csproj` required no edits — it's SDK-style with no explicit `Compile`
items for these files.

### Deliberately retained: `PostgresServer`

The `INDEX.md` blurb for this step suggested deleting "the `PostgresServer`
helper that backs them". **`PostgresServer` was kept**: it is a `static class`
declared inside `src/.../Fixtures/PostgresFixture.cs` and is still the
backing store for `PostgresFixture`, which has four out-of-scope consumers
(`ConnectionQueryTests`, `RequestServiceTests`, `TranslationServiceTests`,
`DeepTranslationExtensionsTests`, plus `DatabaseTestTemplate`). Retiring it
must wait until `PostgresFixture` itself is retired — explicitly called
out as a separate follow-up in the Step 25 doc and in `INDEX.md`.

## Residual references (all benign)

Remaining mentions of `WebApplicationFixture` / `AcceptanceCriteriaComposer`
in the codebase are all comments or XML doc cross-references documenting the
migration history:

- `Controllers/HealthCheckTests.cs`, `Controllers/PartyControllerTests.cs`,
  `Controllers/V2ResourceControllerTest.cs`,
  `Controllers/Bff/ConsentControllerTestBFF.cs`,
  `Controllers/ConsentControllerTestEnterprise.cs`,
  `Controllers/MaskinPorten/ConsentControllerTest.cs` — "Migrated from
  WebApplicationFixture…" comments.
- `Fixtures/LegacyApiFixture.cs`, `Fixtures/LegacyApiFixtureSmokeTest.cs` —
  XML doc comments explaining what the legacy harness provided.

These were left in place; they document intent and cost nothing.

## Verification

```
dotnet build src\apps\Altinn.AccessManagement\test\AccessMgmt.Tests\AccessMgmt.Tests.csproj -c Debug
```

Result: **0 errors**, 957 warnings (all pre-existing StyleCop / xUnit1051
noise unchanged by this step).

No source changes to test logic were made, so no test runs are required for
this step — Step 25 already validated the last migrated consumer end-to-end.

## Follow-up

- Retiring `PostgresFixture` + `PostgresServer` is the natural next cleanup.
  It is **not** on the current priority list; add it as a dedicated item if
  and when the four remaining consumers are migrated or rewritten against
  `LegacyApiFixture` / `ApiFixture`.
