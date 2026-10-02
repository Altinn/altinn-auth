// Shared pre-request helpers for the AvailableUsersAndRights scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../../testdata/sharedtestdata.js");

const scopes = sharedtestdata.auth_scopes;

// Returns the scenario personas for the environment the run targets.
function testdata() {
  return require(`../../../../testdata/enduser-connections-users-rights/${bru.getEnvVar("tokenEnv")}.js`);
}

// Mints a personal token for a persona and stores it as the collection bearer token.
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

module.exports = { loginAs, scopes, testdata };
