// Personas for the delegation request scenarios under test/DelegationRequest/DraftAndCounts
// and test/DelegationRequest/ServiceOwnerDelegationRequests.
// Personas come from the shared AT22 fixtures wherever one exists, so ids are not copied.
// The two persons of the service owner delegation requests exist elsewhere only under
// testdata/manualtests, which an automated suite should not depend on, so they are kept here.
const beOmTilgang = require("../be-om-tilgang/at22.js");

module.exports = {
  env: "at22",

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
    grantor: { name: "HALV BAR", pid: "21886599509", partyUuid: "6d52840a-d12a-4d8c-a5af-79dd9e03a504" },
    // Person who would receive access.
    requester: { name: "FIKTIV FOTBALL", pid: "26848897956", partyUuid: "e92c27c4-4f4a-40d0-9a15-0120fe5c1916" },
  },
};
