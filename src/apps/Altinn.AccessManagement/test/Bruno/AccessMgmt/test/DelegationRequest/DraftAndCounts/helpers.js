// Shared pre-request helpers for the delegation request scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = sharedtestdata.auth_scopes;

// Maps a person entry from the shared testdata files to the shape the steps use.
function person(p) {
  return {
    pid: p.pid,
    userId: p.userid,
    partyId: p.partyid,
    partyUuid: p.partyuuid,
  };
}

// Returns the scenario personas for the environment the run targets.
function testdata() {
  const data = require(`../../../testdata/delegationrequest-scenarios/${bru.getEnvVar("tokenEnv")}.js`).draftAndCounts;
  return {
    requester: person(data.requester),
    organization: {
      name: data.organization.name,
      orgNo: data.organization.org_no,
      partyId: data.organization.partyid,
      partyUuid: data.organization.partyuuid,
      dagligleder: person(data.organization.dagligleder),
    },
    outsider: person(data.outsider),
    package: data.package,
  };
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

// Mints an enterprise token for the ttd service owner and stores it as the collection bearer token.
async function loginAsServiceOwner(authScopes) {
  const token = await tokenGenerator.getToken({
    auth_org: sharedtestdata.serviceOwners.ttd.org,
    auth_orgNo: sharedtestdata.serviceOwners.ttd.orgno,
    auth_tokenType: sharedtestdata.authTokenType.enterprise,
    auth_scopes: authScopes || scopes.serviceownerDelegationRequestWrite,
  });
  bru.setVar("bearerToken", token);
}

module.exports = { loginAs, loginAsServiceOwner, scopes, testdata };
