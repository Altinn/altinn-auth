// Shared helpers for the activity log scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = {
  ...sharedtestdata.auth_scopes,
  maskinportenSuppliersRead: "altinn:accessmanagement/enduser:maskinportensuppliers.read",
};

// Activity type catalog ids, as listed by GET .../activitylog/types. The ids are fixed in
// ActivityTypeConstants and identical in every environment.
const activityTypes = {
  roleAssignmentCreated: "228ceaa6-0213-4673-94d2-d12572869e99",
  roleAssignmentDeleted: "1ad8e8b5-cd6e-49d6-9de8-6118dab071e7",
  packageGranted: "928868d6-5c58-4815-be71-f944a7edcf02",
  packageRemoved: "43ec6f13-c464-4667-9b91-a21a385c1044",
  resourceGranted: "cdb21c9c-efe9-4dd8-a4ff-05e6ed85b363",
  resourceRemoved: "2cc78d26-16a1-4a88-95cd-3ca427fb0731",
  requestCreated: "904cdd82-37b6-41d7-b4e3-a85fcc152d25",
  packageRequestDraftCreated: "0b818b59-1066-4734-bf65-894905c0a1e8",
  packageRequestWithdrawn: "43400193-44eb-49dd-b715-7220abb631f1",
};

const rightholderRoleId = "42cae370-2dc1-4fdc-9c67-c2f4b0f0f829";
const supplierRoleId = "961f6ebb-ecf7-4fc6-ab5a-ca0841db4931";

// Returns the scenario personas for the environment the run targets.
function testdata() {
  return require(`../../../testdata/enduser-activitylog/${bru.getEnvVar("tokenEnv")}.js`);
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

// Case-insensitive uuid comparison; the API returns lower case, the fixtures mix cases.
function sameId(a, b) {
  return typeof a === "string" && typeof b === "string" && a.toLowerCase() === b.toLowerCase();
}

// The entries in a page whose affected row is the given id.
function entriesFor(body, itemId) {
  return (body.data || []).filter(e => sameId(e.itemId, itemId));
}

// Whether the entries are ordered newest first, as the log promises.
function isNewestFirst(entries) {
  return entries.every((e, i) => i === 0 || Date.parse(entries[i - 1].when) >= Date.parse(e.when));
}

// Splits a links.next URL into its path and its query parameters. Bruno's
// default script sandbox has no URL class.
function parseLink(link) {
  const match = /^https?:\/\/[^/]+(\/[^?#]*)(\?[^#]*)?/.exec(link);
  const path = match[1];
  const search = match[2] || "";
  const query = {};
  search.replace(/^\?/, "").split("&").filter(Boolean).forEach(pair => {
    const [key, value = ""] = pair.split("=");
    query[decodeURIComponent(key)] = decodeURIComponent(value.replace(/\+/g, " "));
  });
  return { path, query };
}

module.exports = {
  activityTypes,
  entriesFor,
  isNewestFirst,
  loginAs,
  loginAsServiceOwner,
  parseLink,
  rightholderRoleId,
  sameId,
  scopes,
  supplierRoleId,
  testdata,
};
