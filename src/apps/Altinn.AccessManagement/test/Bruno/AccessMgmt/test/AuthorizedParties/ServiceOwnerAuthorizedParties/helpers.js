// Shared helpers for the ServiceOwnerAuthorizedParties scenarios.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const scopes = sharedtestdata.auth_scopes;

// Mints an enterprise token for a service owner and stores it as the collection bearer token.
// The service owner endpoint takes the subject to look up in the request body, so the token
// only identifies the calling service owner, not the party whose parties are requested.
async function loginAsServiceOwner(serviceOwner, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_org: serviceOwner.org,
    auth_orgNo: serviceOwner.orgno,
    auth_tokenType: sharedtestdata.authTokenType.enterprise,
    auth_scopes: authScopes || scopes.authorizedPartiesResourceOwner,
  });
  bru.setVar("bearerToken", token);
}

// Every party in the response, main units and their subunits alike.
function flatten(parties) {
  return (parties || []).flatMap(party => [party, ...flatten(party.subunits)]);
}

// Party uuids are returned lower cased but written upper cased in some fixture files.
function findParty(parties, partyUuid) {
  const wanted = String(partyUuid).toLowerCase();
  return flatten(parties).find(party => String(party.partyUuid).toLowerCase() === wanted);
}

function partyUuids(parties) {
  return flatten(parties).map(party => String(party.partyUuid).toLowerCase());
}

// Party uuids that appear more than once anywhere in the response tree.
function duplicatePartyUuids(parties) {
  const seen = new Map();
  const duplicates = [];
  for (const uuid of partyUuids(parties)) {
    const count = (seen.get(uuid) || 0) + 1;
    seen.set(uuid, count);
    if (count === 2) {
      duplicates.push(uuid);
    }
  }
  return duplicates;
}

function lower(values) {
  return (values || []).map(value => (typeof value === "string" ? value.toLowerCase() : value));
}

// True when the deletion date is inside the window where a deleted party still grants access.
// The cutoff is snapped to midnight UTC so the result is stable for a whole day.
function isInsideRetentionWindow(deletedDate, retentionYears) {
  const now = new Date();
  const cutoff = new Date(Date.UTC(now.getUTCFullYear() - retentionYears, now.getUTCMonth(), now.getUTCDate()));
  return new Date(`${deletedDate}T00:00:00Z`) > cutoff;
}

module.exports = {
  loginAsServiceOwner,
  flatten,
  findParty,
  partyUuids,
  duplicatePartyUuids,
  lower,
  isInsideRetentionWindow,
  scopes,
};
