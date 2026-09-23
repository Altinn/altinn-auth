// Seed data for the service owner API scenarios under test/ServiceOwnerAPI/Scenarios.
// The service owners come from testdata/sharedtestdata.js through the suite's helpers.js.
//
// The two persons are synthetic AT22 test persons. Elsewhere they only appear in
// testdata/manualtests/systemuser-directdelegation/at22.js, which backs manual tests,
// so they are kept here rather than making an automated suite depend on manualtests.
module.exports = {
  env: "at22",

  resourceDelegation: {
    // Delegable GenericAccessResource owned by digdir, with read and write rights.
    resource: "k6-serviceowner-resource-delegation",
    unknownResource: "bruno-serviceowner-scenarios-unknown-resource",
    grantor: {
      name: "KVADRATISK OMTALE",
      pid: "27812748705",
      partyid: 51188030,
      partyuuid: "ca7f57c0-96f4-4e1c-8bc1-d04d5428a8ac",
    },
    recipient: {
      name: "FIKTIV FOTBALL",
      pid: "26848897956",
      partyid: 50880990,
      partyuuid: "e92c27c4-4f4a-40d0-9a15-0120fe5c1916",
    },
  },
};
