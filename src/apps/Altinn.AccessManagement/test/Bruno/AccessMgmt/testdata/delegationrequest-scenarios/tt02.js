// Personas for the delegation request scenarios under test/DelegationRequest/DraftAndCounts,
// test/DelegationRequest/ServiceOwnerDelegationRequests and test/DelegationRequest/ResourceRequestRightKeysV2.
// Every persona comes from the shared TT02 fixtures, so ids are not copied.
// The two persons of the service owner delegation requests are not paired in any other
// service owner request suite, so a create here never returns another suite's draft.
const beOmTilgang = require("../be-om-tilgang/tt02.js");

module.exports = {
  env: "tt02",

  draftAndCounts: {
    // Private person who asks for access.
    requester: beOmTilgang.Bot_package_from_person,
    // Organization the requester asks for access at, acting through its daglig leder.
    organization: beOmTilgang.Bot_Org_for_serviceowner,
    // Private person with no relation to the requester or the organization.
    outsider: beOmTilgang.Bot_from_person,
    // Access package the service owner asks for on behalf of the requester.
    package: beOmTilgang.package_to_delegate.package_id_virksomhet,
  },

  serviceOwnerDelegationRequests: {
    // Delegable resource owned by ttd.
    resource: beOmTilgang.resource_to_delegate.resource_id_privatperson,
    // Assignable package for a person.
    package: beOmTilgang.package_to_delegate.package_id_privatperson,
    // Person asked to grant access.
    grantor: { name: beOmTilgang.Bot_person.lastname, pid: beOmTilgang.Bot_person.pid, partyUuid: beOmTilgang.Bot_person.partyuuid },
    // Person who would receive access.
    requester: { name: beOmTilgang.Bot_package_person_reject.lastname, pid: beOmTilgang.Bot_package_person_reject.pid, partyUuid: beOmTilgang.Bot_package_person_reject.partyuuid },
  },
  resourceRequestRightKeysV2: {
    // Delegable resource owned by ttd, with at least two rights the approver can delegate.
    resource: beOmTilgang.resource_to_delegate.resource_id_privatperson,
    // Organization asking for access, acting through its innehaver. It is connected to the approver.
    requester: beOmTilgang.Bot_Org,
    // Private person asked to grant access to the resource.
    approver: beOmTilgang.Bot_person,
  },
};
