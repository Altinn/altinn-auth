// Shared pre-request helpers for the ClientDelegationsV2 scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = sharedtestdata.auth_scopes;

// Mints a personal token for a testdata persona and stores it as the collection bearer token.
// Tolerates the key spellings used across the testdata files (userId/userid, pid/personidentity,
// partyUuid/partyuuid/userUuid) so personas from any fixture file can be used directly.
async function loginAs(person, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: person.userId || person.userid,
    auth_partyId: person.partyId || person.partyid,
    auth_partyUuid: person.partyUuid || person.partyuuid || person.userUuid,
    auth_ssn: person.pid || person.personidentity,
    auth_tokenType: sharedtestdata.authTokenType.personal,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

module.exports = { loginAs, scopes };
