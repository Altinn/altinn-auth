 This document provides an overview of how to conduct performance testing using K6 in the Dialogporten Frontend project.

  ## Prerequisites
  * Either
    * [Grafana K6](https://k6.io/) must be installed and `k6` available in `PATH` 
    * or Docker (available as `docker` in `PATH`)
  * Powershell or Bash (should work on any platform supported by K6)

  ## Tests
  ### Authorized Parties
  Contains the following tests for the `api/v1/resourceowner/authorizedparties` endpoint:
  * `getAuthorizedPartiesForOrganization.js`
  * `getAuthorizedPartiesForParty.js`
  * `getAuthorizedPartiesForSystemUsers.js`
  * `getAuthorizedPartiesForUserPartyId.js`

  The body for the POST request has two attributes: `type` and `value`. Each test uses a unique `type` and varies the `value` based on the test data files located at `./testData/orgsIn-<env>-WithPartyUuid.csv`. Currently, test data files exist for the `yt01` and `staging` environments. 

  ### Client Delegations
  A test for the `api/v1/internal/systemuserclientdelegation/clients` endpoint:
  * `getClientDelegations.js`

  Query parameters `party` and `role` are fetched from the file `./testData/orgsIn-yt01-WithPartyUuid.csv`. Query parameter `packages` varies based on `role`.

  ### PDP Authorize
  Contains the following tests for `api/v1/authorize`:
  * `pdpAuthorizeClientDelegations.js`
  * `pdpAuthorizeRoleDagl.js`
  * `pdpAuthorizeRoleDaglDeny.js`
  * `pdpAuthorizeRolePriv.js`

  In the PDP authorize client delegations test, three resources are created: `ttd-performance-clientdelegation`, `ttd-performance-clientdelegation-ffor`, and `ttd-performance-clientdelegation-revisor`. Request bodies are created that yield both `Permit` and `Not applicable`. Other test data is fetched from `./testData/customers.csv`.

  The other tests use the resource `ttd-dialogporten-performance-test-02` and retrieve test data from `./testData/OrgsDagl.csv`.

  ### Consent
  A test for the `api/v1/consent/request/` and the `api/v1/bff/consentrequests/` endpoints:
  * `postConsent.js`

  A request is made from an organization or a person to another person. The response is used to approve the request, as the person to whom the request was made. One resource with the necessary policy is used: `samtykke-performance-test`. Test data is fetched from `./testData/orgsIn-yt01-WithPartyUuid.csv`.

  ## Running Tests
  ### From CLI
  1. Navigate to the following directory:
  ```shell
  cd src/apps/Altinn.Authorization/test/K6/performance
  ```
  2. Run the tests using the following command. Replace the values inside `<>` with appropriate values:
  ```shell
  TOKEN_GENERATOR_USERNAME=<username> TOKEN_GENERATOR_PASSWORD=<passwd> \
  k6 run <test> \
  -e VUS=<vus> \
  -e DURATION=<duration> \
  -e ENVIRONMENT=<environment> \
  -e subscription_key=<subscription_key>
  ```
  * **VUS**: Number of browser VUs (Virtual Users) to run. Default is `1`.
  * **DURATION**: Duration of the test, e.g., `2m` (2 minutes), `30s` (30 seconds). Default is `1m`.
  * **ENVIRONMENT**: Test environment to run the test in, `yt01`, `staging` or `test`
  * **subscription_key**: The subscription key for the environment.

  ### From GitHub Actions
  To run the performance test using GitHub Actions, follow these steps:
  1. Go to the [GitHub Actions](https://github.com/altinn/dialogporten-frontend/actions/workflows/run-performance-tests.yml) page.
  2. Select "Run workflow" and fill in the required parameters. See above for details.
  3. Tag the performance test with a descriptive name.

  ## Reporting
  Test results can be found in the GitHub Action run log and in [Grafana](https://altinn-grafana-test-b2b8dpdkcvfuhfd3.eno.grafana.azure.com/d/ccbb2351-2ae2-462f-ae0e-f2c893ad1028/k6-prometheus?orgId=1&from=now-30m&to=now&timezone=browser&var-DS_PROMETHEUS=k6tests-amw&var-namespace=&var-testid=$__all&var-quantile_stat=p99&var-adhoc_filter=&refresh=30s). 