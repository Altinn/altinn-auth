// Shared pre-request helpers for the BankruptcyEstateAdministrator scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = {
  ...sharedtestdata.auth_scopes,
  bankruptcyEstateRead: "altinn:bankruptcyestatedelegations.read",
  bankruptcyEstateWrite: "altinn:bankruptcyestatedelegations.write",
};

// RoleConstants.BankruptcyEstateAdministrator
const administratorRole = {
  id: "e8f2e3bc-41e8-4ea6-87d3-3308934d471d",
  code: "innbygger-konkursbo-administrator",
};

// The packages an administrator gets from every estate of the bostyrer.
const estatePackageUrns = [
  "urn:altinn:accesspackage:hovedadministrator",
  "urn:altinn:accesspackage:konkursbo-lesetilgang",
  "urn:altinn:accesspackage:konkursbo-skrivetilgang",
];

// A party uuid that does not exist in any environment.
const unknownPartyUuid = "5b1e0f3c-8d2a-4c7e-9f41-0a6b2d9e7c13";

// Environments whose seed data has been verified for this suite.
const supportedEnvironments = ["at22"];

// Skips the current request outside the supported environments. Returns true when skipped,
// so the caller can stop before it loads seed data that does not exist there.
function skipOutsideSupportedEnvironments() {
  if (supportedEnvironments.includes(bru.getEnvVar("tokenEnv"))) {
    return false;
  }

  bru.runner.skipRequest();
  return true;
}

// Like skipOutsideSupportedEnvironments, and also skips unless the person lookup negatives are
// switched on with the runPersonLookupNegatives environment variable. See 08_PersonLookupFailures.
function skipUnlessPersonLookupNegativesEnabled() {
  if (skipOutsideSupportedEnvironments()) {
    return true;
  }

  if (String(bru.getEnvVar("runPersonLookupNegatives")).toLowerCase() === "true") {
    return false;
  }

  bru.runner.skipRequest();
  return true;
}

function testdata() {
  return require(`../../../testdata/bankruptcyestate-delegation/${bru.getEnvVar("tokenEnv")}.js`);
}

// Mints a personal token for a testdata persona and stores it as the collection bearer token.
// Tolerates the key spellings used in the testdata file (userId/userid).
async function loginAs(person, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: person.userId || person.userid,
    auth_partyId: person.partyId || person.partyid,
    auth_partyUuid: person.partyUuid || person.partyuuid,
    auth_ssn: person.pid,
    auth_tokenType: sharedtestdata.authTokenType.personal,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

// Finds the entry for an administrator in a GET administrators response body. Throws when the
// body has no data array, so an absence check cannot pass on a malformed response.
function findAdministrator(body, partyUuid) {
  if (!body || !Array.isArray(body.data)) {
    throw new Error(`expected a body with a data array, got ${JSON.stringify(body)}`);
  }

  return body.data.find(
    entry => entry.administrator && entry.administrator.id.toLowerCase() === partyUuid.toLowerCase());
}

// Lists the administrators of a party with the current bearer token, outside the step's own
// request. Used to check state before or after a write in the same step.
async function listAdministrators(party) {
  const axios = require("axios");
  const response = await axios.get(
    `${bru.getEnvVar("baseUrl")}/accessmanagement/api/v1/enduser/bankruptcyestate/administrators?party=${party}`,
    { headers: { Authorization: `Bearer ${bru.getVar("bearerToken")}` } });
  return response.data;
}

// Adds an administrator with the current bearer token, outside the step's own request, until a
// response has the wanted status or maxAttempts requests have been sent. Returns every response
// as { status, code }. The failed-lookup counter is per pod, so the 08 scenario repeats requests
// until one lands on a pod in the state it is checking.
async function addAdministratorUntil(party, person, wantedStatus, maxAttempts) {
  const axios = require("axios");
  const responses = [];
  while (responses.length < maxAttempts) {
    const response = await axios.post(
      `${bru.getEnvVar("baseUrl")}/accessmanagement/api/v1/enduser/bankruptcyestate/administrators?party=${party}`,
      person,
      {
        headers: { Authorization: `Bearer ${bru.getVar("bearerToken")}` },
        validateStatus: () => true,
      });
    responses.push({ status: response.status, code: response.data && response.data.code });
    if (response.status === wantedStatus) {
      break;
    }
  }

  return responses;
}

// The party uuids of the bostyrer's estates in the seed data (the client_* entries).
function estateIds(bostyrer) {
  return Object.keys(bostyrer)
    .filter(key => key.startsWith("client_"))
    .map(key => bostyrer[key].partyUuid.toLowerCase());
}

module.exports = {
  scopes,
  administratorRole,
  estatePackageUrns,
  unknownPartyUuid,
  supportedEnvironments,
  skipOutsideSupportedEnvironments,
  skipUnlessPersonLookupNegativesEnabled,
  testdata,
  loginAs,
  findAdministrator,
  listAdministrators,
  addAdministratorUntil,
  estateIds,
};
