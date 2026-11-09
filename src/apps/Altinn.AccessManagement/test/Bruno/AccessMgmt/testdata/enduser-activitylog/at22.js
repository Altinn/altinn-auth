// Personas for the activity log scenarios under test/EnduserAPI/ActivityLog.
// Nothing is defined here: every persona, package and resource is taken from the shared
// at22 fixtures and only renamed to the roles the scenarios give them.
const personDelegation = require("../person-accesspackage-delegation/at22.js");
const beOmTilgang = require("../be-om-tilgang/at22.js");
const delegationRequests = require("../delegationrequest-scenarios/at22.js");
const maskinporten = require("../maksinporten-delegation/at22.js");

// The shared fixtures write partyid, userid and partyuuid in lower case; the scenarios use camel case.
function person(p) {
  return {
    name: p.name,
    lastname: p.lastname,
    pid: p.pid,
    userId: p.userid,
    partyId: p.partyid,
    partyUuid: p.partyuuid,
  };
}

function organization(o) {
  return {
    name: o.name,
    orgNo: o.orgno ?? o.org_no,
    partyUuid: o.partyuuid,
  };
}

module.exports = {
  env: "at22",

  connections: {
    // Private person who makes the receiver a rightholder and gives an access package.
    delegator: person(personDelegation.privat_person_C),

    // Private person who receives access. No other suite pairs this person with the
    // delegator, so the scenario owns the connection between the two.
    receiver: person(personDelegation.privat_person_A),

    // Access package a private person can give another private person.
    package: personDelegation.package_to_delegate.innbygger_pacakage_urn,
  },

  request: {
    // Private person the service owner asks access for. The draft is the same one the
    // DraftAndCounts scenarios use; creating it is idempotent.
    requester: person(delegationRequests.draftAndCounts.requester),

    // Organization the access is asked at.
    organization: organization(delegationRequests.draftAndCounts.organization),

    // Access package the service owner asks for on behalf of the requester.
    package: delegationRequests.draftAndCounts.package,
  },

  maskinporten: {
    // Organization that delegates a Maskinporten scope, acting through its daglig leder.
    consumer: {
      ...organization(maskinporten.consumer_Org),
      dagligleder: person(maskinporten.consumer_Org.dagligleder),
    },

    // Organization that receives the delegation, acting through its innehaver. No other
    // suite pairs it with the consumer, so the scenario owns the supplier relation.
    supplier: {
      ...organization(maskinporten.supplier_Org2),
      innehaver: person(maskinporten.supplier_Org2.innehaver),
    },

    // Maskinporten schema resource owned by ttd.
    resource: maskinporten.resource_to_delegate.maskinporten_ressurs,
  },

  // Private person with no relation to any of the parties above, for authorization negatives.
  outsider: person(beOmTilgang.Bot_from_person),
};
