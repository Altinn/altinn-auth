// Shared pre-request helpers for the SystemUserPackageRequest scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = {
  ...sharedtestdata.auth_scopes,
  systemUserRequestsWrite: "altinn:accessmanagement/systemuser:requests.write",
};

function testdata() {
  return require(`./testdata/${bru.getEnvVar("tokenEnv")}.js`);
}

// Mints a personal token for a testdata persona and stores it as the collection bearer token.
async function loginAs(person, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: person.userId || person.userid,
    auth_partyId: person.partyId || person.partyid,
    auth_partyUuid: person.partyUuid || person.partyuuid,
    auth_ssn: person.pid || person.personidentity,
    auth_tokenType: sharedtestdata.authTokenType.personal,
    auth_scopes: authScopes || `${scopes.portalEnduser} ${scopes.sluttbrukerdelegationrequestread} ${scopes.sluttbrukerdelegationrequestwrite}`,
  });
  bru.setVar("bearerToken", token);
}

// Mints a system user token. consumerOrgNo is the consumer claim (the calling organisation) and
// systemUserOrgNo is systemuser_org (the organisation owning the system user). The API requires both
// to be the same KOMM, FYLK or STAT organisation.
async function loginAsSystemUser(systemUser, consumerOrgNo, systemUserOrgNo, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_systemUserId: systemUser.partyUuid,
    auth_systemUserOrg: systemUserOrgNo,
    auth_orgNo: consumerOrgNo,
    auth_clientId: systemUser.clientId,
    auth_tokenType: sharedtestdata.authTokenType.systemUser,
    auth_scopes: authScopes || scopes.systemUserRequestsWrite,
  });
  bru.setVar("bearerToken", token);
}

module.exports = { testdata, loginAs, loginAsSystemUser, scopes };
