// Shared helpers for the EnduserAuthorizedParties scenarios.
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

// Mints a personal token for a self identified user, which authenticates by user name at
// authentication level 1 and carries no national identity number.
async function loginAsSelfIdentifiedUser(user, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: user.userId,
    auth_partyId: user.partyId,
    auth_partyUuid: user.partyUuid,
    auth_username: user.username,
    auth_ssn: "",
    auth_tokenLvl: 1,
    auth_tokenType: sharedtestdata.authTokenType.personal,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

// Mints a token for an ID-porten user registered by email address.
async function loginAsEmailUser(user, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_userId: user.userId,
    auth_partyId: user.partyId,
    auth_partyUuid: user.partyUuid,
    auth_email: user.emailId,
    auth_tokenLvl: 0,
    auth_tokenType: sharedtestdata.authTokenType.selfRegisteredEmailUser,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

// Mints a system user token. The endpoint resolves the caller from the system user uuid
// when the token carries no user id claim.
async function loginAsSystemUser(systemUser, orgNo, authScopes) {
  const token = await tokenGenerator.getToken({
    auth_systemUserId: systemUser.partyUuid,
    auth_orgNo: orgNo,
    auth_clientId: systemUser.clientId,
    auth_tokenType: sharedtestdata.authTokenType.systemUser,
    auth_scopes: authScopes || scopes.portalEnduser,
  });
  bru.setVar("bearerToken", token);
}

// Mints a platform access token for an app, used by the app instance delegation endpoints.
async function platformAccessTokenFor(app) {
  const token = await tokenGenerator.getToken({
    auth_org: app.org,
    auth_app: app.app,
    auth_tokenType: sharedtestdata.authTokenType.platformAccess,
  });
  bru.setVar("platformAccessToken", token);
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
  loginAs,
  loginAsSelfIdentifiedUser,
  loginAsEmailUser,
  loginAsSystemUser,
  platformAccessTokenFor,
  flatten,
  findParty,
  partyUuids,
  duplicatePartyUuids,
  lower,
  isInsideRetentionWindow,
  scopes,
};
