// Shared pre-request helpers for the consent scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../testdata/sharedtestdata.js");

// Scopes used by the consent endpoints.
// - consentRequestsWrite: create and read consent requests (Api.Enterprise).
// - consentRequestsRead: read consent requests and events only (Api.Enterprise).
// - maskinportenConsentRead: the Maskinporten consent lookup.
// - portalEnduser: every BFF endpoint (Api.Internal), used by the Altinn portal.
const scopes = {
  consentRequestsWrite: "altinn:consentrequests.write",
  consentRequestsRead: "altinn:consentrequests.read",
  maskinportenConsentRead: "altinn:maskinporten/consent.read",
  portalEnduser: sharedtestdata.auth_scopes.portalEnduser,
};

function testdata() {
  return require(`../../testdata/consent/${bru.getEnvVar("tokenEnv")}.js`);
}

// Mints a personal token for a testdata persona and stores it as the collection bearer token.
async function loginAs(person, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: person.userId,
    auth_partyId: person.partyId,
    auth_partyUuid: person.partyUuid,
    auth_ssn: person.pid,
    auth_tokenType: sharedtestdata.authTokenType.personal,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

// Mints a Maskinporten style enterprise token for an organization and stores it as the
// collection bearer token. No service owner org code is set, so the token carries only the
// consumer claim, as a regular enterprise token from Maskinporten does.
async function loginAsEnterprise(enterprise, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_org: "",
    auth_orgNo: enterprise.orgno,
    auth_tokenType: sharedtestdata.authTokenType.enterprise,
    auth_scopes: authScopes,
  });
  bru.setVar("bearerToken", token);
}

// Sets the runtime variables that the create request body reads: a fresh request id, the
// parties as URNs, a validTo one week ahead and the consent resource.
function prepareConsentRequest(idVar) {
  const td = testdata();
  const { v4: uuidv4 } = require("uuid");
  const id = uuidv4();
  bru.setVar(idVar, id);
  bru.setVar("consentFrom", `urn:altinn:person:identifier-no:${td.consentingPerson.pid}`);
  bru.setVar("consentTo", `urn:altinn:organization:identifier-no:${td.enterprise.orgno}`);
  bru.setVar("consentValidTo", new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString());
  bru.setVar("consentResource", td.consentResource.id);
  bru.setVar("consentAction", td.consentResource.action);
  bru.setVar("consentMetadataValue", td.consentResource.metadataValue);
  return id;
}

module.exports = { loginAs, loginAsEnterprise, prepareConsentRequest, scopes, testdata };
