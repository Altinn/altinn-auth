// Placeholder seed data for the service owner API scenarios under test/ServiceOwnerAPI/Scenarios.
// In TT02 the API gateway answers 404 for serviceowner/authorizedparties and the three
// serviceowner/connections/resources endpoints the scenarios call. environments/TT02.bru lists
// serviceowner-authorizedparties-and-resources in notYetDeployed, so the folder skips every
// request. The values below are empty on purpose; the request scripts read them before the
// request is skipped.
//
// Once those endpoints answer in TT02 (check the gateway route and the feature flag
// AccessMgmt.Controller.ServiceOwner.ResourceDelegation), remove the name from notYetDeployed and
// fill in the resource and two TT02 persons, as in at22.js: the grantor must have no other parties
// visible to digdir as service owner, since 03_AuthorizedPartiesLookup expects only the person's
// own party.
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
