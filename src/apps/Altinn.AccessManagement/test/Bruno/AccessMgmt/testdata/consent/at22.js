// Seed data for the consent scenarios under test/Consent.
// Every scenario creates fresh consent requests with a new id, so nothing here depends on
// earlier runs. Consent requests cannot be deleted; the scenarios leave each request they
// create in a final state (revoked or rejected) instead.
//
// The personas are not copied here. They come from the shared testdata files below and are
// only reshaped to the fields the consent helpers read. Requires inside this module resolve
// relative to this file.
const enduser = require("../enduser/at22.js");
const klientDelegation = require("../klient-delegation/at22.js");

// testdata/enduser writes its persons with lowercase keys and uppercase party uuids. The API
// returns party uuids in lowercase, so they are normalized here for the URN assertions.
function person(p) {
  return {
    name: p.name,
    pid: p.pid,
    userId: p.userid,
    partyId: p.partyid,
    partyUuid: p.partyuuid.toLowerCase(),
  };
}

module.exports = {
  env: "at22",

  // Consent resource in the AT22 resource registry (resourceType Consent, consentTemplate
  // "default"). Its policy lets any organization request consent (action requestconsent)
  // and lets a person consent on their own behalf (action consent, role priv). The
  // resource declares one required metadata key, inntektsaar.
  consentResource: {
    id: "samtykke-performance-test",
    action: "consent",
    templateId: "default",
    metadataKey: "inntektsaar",
    metadataValue: "2026",
  },

  // The enterprise that requests consent. Authenticates with a Maskinporten token whose
  // consumer claim carries this organization number.
  // OVERFLADISK LANG TIGER AS, REVI_Organisasjon in testdata/klient-delegation.
  enterprise: {
    name: klientDelegation.REVI_Organisasjon.name,
    orgno: klientDelegation.REVI_Organisasjon.orgno,
    partyUuid: klientDelegation.REVI_Organisasjon.partyUuid,
  },

  // An unrelated enterprise, used to show that one enterprise cannot read another
  // enterprise's consent requests.
  // GEOMETRISK VOKSENDE TIGER AS, org1_delegates_tilgangspakke in testdata/enduser.
  otherEnterprise: {
    name: enduser.org1_delegates_tilgangspakke.name,
    orgno: enduser.org1_delegates_tilgangspakke.orgno,
  },

  // The person asked for consent. Reads, accepts, rejects and revokes through the BFF.
  // KONGE ALPAKKA, hovedadministrator of org1_delegates_tilgangspakke in testdata/enduser.
  consentingPerson: person(enduser.org1_delegates_tilgangspakke.hovedadministrator),

  // A person with no rights for the consenting person, used for the authorization negatives.
  // PARODISK BOKHANDEL, dagligleder of org1_delegates_tilgangspakke in testdata/enduser.
  otherPerson: person(enduser.org1_delegates_tilgangspakke.dagligleder),
};
