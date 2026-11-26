# Bruno API Test Collections

[Bruno](https://www.usebruno.com/) collections live alongside the code and exercise the
**running** APIs against the shared Altinn test environments (AT / TT02 / YT01). They are
**not** part of GitHub CI (not wired into `dotnet test` or GitHub Actions). Instead, two of
them run in **Azure DevOps as a post-deployment backend smoke test** after each release to
AT22 and TT02 (see [Running](#running)), and all of them can be run manually (the Bruno app
or the `bru` CLI) for exploratory verification. They also double as the **behavioral
spec** for the C# integration tests: endpoint inventory, request/response shapes,
field-level assertions, and known-good seed-data IDs.

## Where they live

| Collection | Path | Azure DevOps |
|---|---|---|
| AccessManagement | `src/apps/Altinn.AccessManagement/test/Bruno/AccessMgmt/` | `access-management-bruno` (definition 475), `test/` only |
| Authorization | `src/apps/Altinn.Authorization/test/Bruno/Altinn.Authorization/` | `authorization-bruno` (definition 480), `test/` only |
| Authentication | `src/apps/Altinn.Authentication/test/Bruno/Altinn.Authentication/` | not run; see its [AGENTS.md](../../src/apps/Altinn.Authentication/AGENTS.md#test-gotchas) |
| ResourceRegistry | `src/apps/Altinn.ResourceRegistry/test/Bruno/Altinn.ResourceRegistry/` | not run |

`AccessMgmt/shared/Consent/Consent/` has a `bruno.json` of its own, so the Bruno app can also
open it as a separate collection.

## Layout (AccessMgmt collection)

- `bruno.json` / `collection.bru`: collection config and collection-level scripts.
- `environments/`: one `.bru` per Altinn environment (`AT22`, `AT23`, `AT24`, `TT02`,
  `YT01`, `DEV`, `PROD`); selects `baseUrl`, `tokenEnv`, and other per-env variables.
- `test/`: the requests Azure DevOps runs, grouped by API area: `EnduserAPI`,
  `InternalAPI`, `ServiceOwnerAPI`, `AuthorizedParties`, `Consent`, `DelegationRequest`,
  `SystemUserClientDelegation`, `Maskinporten`, `KlientDelegation`,
  `Virksomhetsdelegering`, ….
- `shared/`: requests that no pipeline runs: exploratory requests, and `Prep_*` folders
  that someone runs by hand to create seed data in an environment (`Enduser`, `Lookup`,
  `PolicyInformationPoint`, …). `old_SBL_Bridge_tests/` is legacy; ignore it.
- `TestToolsTokenGenerator.js` + per-area `testdata/*.js`: generate platform / Maskinporten
  tokens and supply seed-data IDs (org / app / party / instance) at request time.

## Anatomy of a `.bru` request

```
meta      { name, type: http, seq }
get|post  { url: {{baseUrl}}/accessmanagement/api/v1/…, body, auth }
params:path / params:query / headers     # e.g. PlatformAccessToken: {{platformAccessToken}}
script:pre-request { … generate a token, set bru vars from testdata … }
tests     { test("…", () => { expect(res.status).to.equal(200); … }) }
```

The `tests { … }` block is the behavioral assertion (status code, body fields, error
codes). That is the contract worth mirroring in a C# integration test.

## BDD scenario suites

Newer suites are structured as BDD scenarios instead of flat per-endpoint request lists.
The reference is `test/EnduserAPI/ClientDelegationsV2/` (v2 client delegation):

- One folder per scenario. `folder.bru` holds the Gherkin text in a `docs` block, and the
  step files are named `NN_GIVEN|WHEN|THEN|AND|CLEANUP_...` with `seq` matching the step
  order.
- Every scenario starts from a known state: the GIVEN steps are idempotent reset requests
  (deletes that return 204 whether or not anything exists), so leftovers from earlier or
  crashed runs never break a scenario. Fixtures are shared across suites, so a scenario
  must not assume the absence of state it did not itself remove.
- Every scenario removes what it created (CLEANUP steps), so the whole suite can run
  repeatedly against a shared environment and scenarios can run standalone.
- Setup that crosses APIs is expressed as explicit GIVEN steps. The client delegation
  scenarios establish rightholder relations, package grants, and single-resource grants
  through the v1 connections API before exercising the v2 endpoints, which also documents
  how the APIs compose.
- Personas map to testdata entries, and each step mints its own token through a suite
  `helpers.js` (`loginAs(persona, scopes?)`), so a scenario can switch between the
  facilitator, the client, and the agent per step. Validation negatives (400) assert the
  full problem body: status, `code`, `validationErrors[].code`, and the parameter the
  error points at. Authorization negatives (401/403) assert the status only, since those
  responses carry no problem body.
- A suite whose seed data exists only in some environments skips itself elsewhere: each
  step calls a `helpers.js` guard that runs `bru.runner.skipRequest()` unless `tokenEnv` is
  supported. `test/EnduserAPI/BankruptcyEstateAdministrator/` runs in AT22 and TT02.
- Leave out negatives that would leave lasting state in the shared environment, such as
  failed person lookups, which lock the caller out of lookups for an hour. Cover those in
  unit or integration tests instead.

## Using them as a spec for C# tests

- **Endpoint inventory**: which endpoints exist, with their routes and verbs.
- **Assertion style**: the exact status codes and response fields an endpoint must
  return, including error / negative cases.
- **Seed data**: the environment `.bru` files and `testdata/*.js` hold known-good
  org / app / party / resource IDs. Those IDs target the live AT / TT02 environments and
  are **not** reusable in the in-process EF test host; use `TestData.*` / the `*Constants`
  in the C# fixtures instead, and treat the Bruno IDs only as a guide to which entities a
  scenario needs.

## Running

- **Automatically:** each release to AT22 or TT02 queues the Azure DevOps pipeline for
  that app, which runs `bru run "test" -r --env <ENV> --sandbox=developer` from the
  collection root, publishes the JUnit results and posts a summary to Slack. They are not
  part of GitHub CI (`dotnet test` / GitHub Actions). The pipelines install
  `@usebruno/cli` without a version, so CI runs the latest release, which may differ
  from your local `bru`.
- **Manually:** open the collection in the Bruno app, or run the same `bru run` command
  from the collection root; pick an environment and supply secrets via `.env` (see
  `.env.sample`).

Tests under `test/` may depend on seed data that a `shared/.../Prep_*` folder created in
that environment. Nothing re-creates it, so a request that deletes it breaks those tests
in every run until someone runs the prep request again
([#4853](https://github.com/Altinn/altinn-auth/issues/4853)).

The CLI parses the whole collection before it sends a request, so one `.bru` file it
cannot parse stops the entire run. A UTF-8 BOM fails with `Expected end of input` at
line 1, col 1 ([#4595](https://github.com/Altinn/altinn-auth/pull/4595)), and any other
syntax error, such as text pasted into a block, stops it too. `.bru` files must therefore be UTF-8 **without** a BOM,
which is the `.editorconfig` default for every file type except `*.cs`, `*.csproj`,
`*.props` and `*.targets`.
