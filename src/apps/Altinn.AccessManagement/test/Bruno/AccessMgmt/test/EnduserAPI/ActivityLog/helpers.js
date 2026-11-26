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

// The sentence template per activity type and language, copied from ActivityTypeConstants
// for the types these scenarios produce. Kept as literals on purpose: a changed wording
// should fail here and be updated deliberately.
const entryTexts = {
  [activityTypes.roleAssignmentCreated]: {
    nob: "{to} fikk rollen «{role}» hos {from} tildelt av {by}",
    nno: "{to} fekk rolla «{role}» hos {from} tildelt av {by}",
    eng: "{to} was given the role “{role}” at {from} by {by}",
  },
  [activityTypes.roleAssignmentDeleted]: {
    nob: "Rollen «{role}» for {to} hos {from} ble fjernet av {by}",
    nno: "Rolla «{role}» for {to} hos {from} vart fjerna av {by}",
    eng: "The role “{role}” for {to} at {from} was removed by {by}",
  },
  [activityTypes.packageGranted]: {
    nob: "{to} fikk tilgangspakken «{package}» hos {from} tildelt av {by}",
    nno: "{to} fekk tilgangspakken «{package}» hos {from} tildelt av {by}",
    eng: "{to} was granted the access package “{package}” at {from} by {by}",
  },
  [activityTypes.packageRemoved]: {
    nob: "Tilgangspakken «{package}» for {to} hos {from} ble fjernet av {by}",
    nno: "Tilgangspakken «{package}» for {to} hos {from} vart fjerna av {by}",
    eng: "The access package “{package}” for {to} at {from} was removed by {by}",
  },
  [activityTypes.resourceGranted]: {
    nob: "{to} fikk enkelttjenesten «{resource}» hos {from} tildelt av {by}",
    nno: "{to} fekk enkelttenesta «{resource}» hos {from} tildelt av {by}",
    eng: "{to} was granted the single service “{resource}” at {from} by {by}",
  },
  [activityTypes.resourceRemoved]: {
    nob: "Enkelttjenesten «{resource}» for {to} hos {from} ble fjernet av {by}",
    nno: "Enkelttenesta «{resource}» for {to} hos {from} vart fjerna av {by}",
    eng: "The single service “{resource}” for {to} at {from} was removed by {by}",
  },
  [activityTypes.packageRequestDraftCreated]: {
    nob: "{by} opprettet et utkast til forespørsel om tilgangspakken «{package}» for {to} hos {from}",
    nno: "{by} oppretta eit utkast til førespurnad om tilgangspakken «{package}» for {to} hos {from}",
    eng: "{by} created a draft request for the access package “{package}” for {to} at {from}",
  },
  [activityTypes.packageRequestWithdrawn]: {
    nob: "{by} trakk tilbake forespørselen om tilgangspakken «{package}» for {to} hos {from}",
    nno: "{by} trekte tilbake førespurnaden om tilgangspakken «{package}» for {to} hos {from}",
    eng: "{by} withdrew the request for the access package “{package}” for {to} at {from}",
  },
};

const unknownName = { nob: "ukjent", nno: "ukjend", eng: "unknown" };

// The sentence the API should return for an entry: its type's template filled from the
// entry's own name snapshots, so the check holds whatever names the environment has.
// language is the three-letter code; Bokmål when omitted, as the API defaults to.
function expectedText(entry, language) {
  const lang = language || "nob";
  const templates = entryTexts[entry.activityTypeId];
  if (!templates) {
    throw new Error(`no entry text in helpers.js for activity type ${entry.activityTypeId}`);
  }
  const template = templates[lang];
  const names = {
    from: entry.fromName,
    to: entry.toName,
    via: entry.viaName,
    by: entry.byName,
    role: entry.roleName,
    package: entry.packageName,
    resource: entry.resourceName,
  };
  return template.replace(/\{(\w+)\}/g, (_, key) => (names[key] && names[key].trim()) ? names[key] : unknownName[lang]);
}

// Whether an entry text is filled in: a non-empty sentence with no placeholder left.
function isRenderedText(text) {
  return typeof text === "string" && text.trim().length > 0 && !/\{\w+\}/.test(text);
}

const rightholderRoleId ="42cae370-2dc1-4fdc-9c67-c2f4b0f0f829";
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
  expectedText,
  isNewestFirst,
  isRenderedText,
  loginAs,
  loginAsServiceOwner,
  parseLink,
  rightholderRoleId,
  sameId,
  scopes,
  supplierRoleId,
  testdata,
};
