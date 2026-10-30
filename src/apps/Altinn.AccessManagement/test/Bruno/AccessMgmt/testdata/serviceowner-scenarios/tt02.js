// Seed data for the service owner API scenarios under test/ServiceOwnerAPI/Scenarios.
// The service owners come from testdata/sharedtestdata.js through the suite's helpers.js.
//
// The two persons are synthetic TT02 test persons from testdata/be-om-tilgang/tt02.js
// (Bot_package_person_reject and Bot_package_person_withdraw). The suites there never pair
// them with each other, so the delegation between them belongs to these scenarios. The grantor
// has no other party visible to digdir as service owner, as 03_AuthorizedPartiesLookup expects.
// Names are as the register returns them.
module.exports = {
  env: "tt02",

  resourceDelegation: {
    // Delegable GenericAccessResource owned by digdir, with read and write rights.
    resource: "k6-serviceowner-resource-delegation",
    unknownResource: "bruno-serviceowner-scenarios-unknown-resource",
    grantor: {
      name: "GUL KALV",
      pid: "27920999617",
      partyid: 50886468,
      partyuuid: "000124da-184f-44bf-9618-d3dfeea3e23c",
    },
    recipient: {
      name: "FULLKOMMEN FAMILIEBARNEHAGE",
      pid: "13859797622",
      partyid: 50865622,
      partyuuid: "af504a64-5060-42aa-b84e-1f7c0e81923d",
    },
  },
};
