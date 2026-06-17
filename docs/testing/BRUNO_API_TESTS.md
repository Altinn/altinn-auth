# Bruno API Test Collections

[Bruno](https://www.usebruno.com/) collections live alongside the code and exercise the
**running** APIs against the shared Altinn test environments (AT / TT02 / YT01). They are
**not** part of GitHub CI (not wired into `dotnet test` or GitHub Actions). Instead they
run automatically in **Azure DevOps, as a post-deployment backend smoke test** against the
deployed environment, and can also be run manually (the Bruno app or the `bru` CLI) for
exploratory verification. They also double as the **behavioral spec** for the C#
integration tests: endpoint inventory, request/response shapes, field-level assertions,
and known-good seed-data IDs.

## Where they live

| Collection | Path |
|---|---|
| AccessManagement | `src/apps/Altinn.AccessManagement/test/Bruno/AccessMgmt/` |
| Authorization | `src/apps/Altinn.Authorization/test/Bruno/Altinn.Authorization/` |

## Layout (AccessMgmt collection)

- `bruno.json` / `collection.bru`: collection config and collection-level scripts.
- `environments/`: one `.bru` per Altinn environment (`AT22`, `AT23`, `AT24`, `TT02`,
  `YT01`, `DEV`, `PROD`); selects `baseUrl`, `tokenEnv`, and other per-env variables.
- `shared/` and `test/`: requests grouped by API area: `Consent`, `Delegations`,
  `Enduser`, `InternalAPI`, `Lookup`, `Maskinporten`, `MaskinportenSchema`,
  `PolicyInformationPoint`, `Resource`, `RightsInternal`, `SystemUserClientDelegation`,
  `AuthorizedParties`, `AppsInstanceDelegation`, … (`old_SBL_Bridge_tests/` is legacy,
  ignore it).
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

- **Automatically:** in Azure DevOps, as a post-deployment backend smoke test against the
  deployed environment. They are not part of GitHub CI (`dotnet test` / GitHub Actions).
- **Manually:** open the collection in the Bruno app, or `bru run` via the CLI; pick an
  environment and supply secrets via `.env` (see `.env.sample`).
