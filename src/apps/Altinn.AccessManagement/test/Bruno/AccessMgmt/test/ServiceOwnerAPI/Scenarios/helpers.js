// Shared helpers for the service owner API scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

// Maskinporten scopes the service owner endpoints require. Scopes already listed in
// sharedtestdata.js are taken from there.
const scopes = {
  authorizedParties: sharedtestdata.auth_scopes.authorizedPartiesResourceOwner,
  resourceDelegationWrite: "altinn:serviceowner/delegations:resource.write",
  packageDelegationWrite: sharedtestdata.auth_scopes.serviceownerConnectionsAccessPackages,
};

// Service owners from sharedtestdata.js. digdir owns the delegated resource; the
// connections API compares the organization number in the token's consumer claim with
// the resource provider's. skd owns none of the resources the scenarios use.
const serviceOwners = {
  digdir: sharedtestdata.serviceOwners.digdir,
  skd: sharedtestdata.serviceOwners.skd,
};

// Seed data for the environment the run targets.
function td() {
  return require(`../../../testdata/serviceowner-scenarios/${bru.getEnvVar("tokenEnv")}.js`);
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

module.exports = { td, loginAsServiceOwner, scopes, serviceOwners };
