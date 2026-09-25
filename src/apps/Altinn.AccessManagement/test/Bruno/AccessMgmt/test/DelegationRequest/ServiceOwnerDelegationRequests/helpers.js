// Shared helpers for the service owner delegation request scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

// Maskinporten scopes the delegation request endpoints require.
const scopes = {
  delegationRequestsWrite: sharedtestdata.auth_scopes.serviceownerDelegationRequestWrite,
  delegationRequestsRead: sharedtestdata.auth_scopes.serviceownerDelegationRequestRead,
};

// Maps a person entry from the shared testdata files to the shape the steps use.
// Party UUIDs are lowercased because the API returns them in lowercase.
function person(p) {
  return {
    pid: p.pid,
    partyuuid: p.partyUuid.toLowerCase(),
  };
}

// Seed data for the environment the run targets.
function td() {
  const data = require(`../../../testdata/delegationrequest-scenarios/${bru.getEnvVar("tokenEnv")}.js`).serviceOwnerDelegationRequests;
  return {
    serviceOwners: sharedtestdata.serviceOwners,
    delegationRequest: {
      resource: data.resource,
      package: data.package,
      grantor: person(data.grantor),
      requester: person(data.requester),
    },
  };
}

// Mints an enterprise (Maskinporten) token for a service owner and stores it as the
// collection bearer token.
async function loginAsServiceOwner(serviceOwner, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_org: serviceOwner.org,
    auth_orgNo: serviceOwner.orgno,
    auth_tokenType: sharedtestdata.authTokenType.enterprise,
    auth_scopes: authScopes,
  });
  bru.setVar("bearerToken", token);
}

module.exports = { td, loginAsServiceOwner, scopes };
