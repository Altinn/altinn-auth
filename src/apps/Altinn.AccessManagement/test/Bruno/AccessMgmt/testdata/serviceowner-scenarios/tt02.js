// Placeholder seed data for the service owner API scenarios under test/ServiceOwnerAPI/Scenarios.
// The service owner API is not deployed in TT02 yet: every endpoint the scenarios call answers
// 404 there. environments/TT02.bru lists serviceowner-api in notYetDeployed, so the folder skips
// every request. The values below are empty on purpose; the request scripts read them before
// the request is skipped.
//
// When the API reaches TT02, remove serviceowner-api from notYetDeployed and fill in the
// resource and two TT02 persons, as in at22.js: the grantor must have no other parties visible
// to digdir as service owner, since 03_AuthorizedPartiesLookup expects only the person's own party.
module.exports = {
  env: "tt02",

  resourceDelegation: {
    resource: "k6-serviceowner-resource-delegation",
    unknownResource: "bruno-serviceowner-scenarios-unknown-resource",
    grantor: {
      name: "",
      pid: "",
      partyid: 0,
      partyuuid: "",
    },
    recipient: {
      name: "",
      pid: "",
      partyid: 0,
      partyuuid: "",
    },
  },
};
