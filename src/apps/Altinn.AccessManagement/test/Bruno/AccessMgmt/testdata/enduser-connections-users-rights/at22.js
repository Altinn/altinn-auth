// Personas for the enduser connections scenarios under test/EnduserAPI/Connections/AvailableUsersAndRights.
// Nothing is defined here: every persona, instance and resource is taken from the shared
// at22 fixtures and only renamed to the roles the scenarios give them.
const beOmTilgang = require("../be-om-tilgang/at22.js");
const instansDelegation = require("../enduser_instans-delegation/at22.js");

// be-om-tilgang writes partyid, userid and partyuuid in lower case; the scenarios use camel case.
function fromBeOmTilgang(person) {
  return {
    lastname: person.lastname,
    pid: person.pid,
    userId: person.userid,
    partyId: person.partyid,
    partyUuid: person.partyuuid,
  };
}

module.exports = {
  env: "at22",

  // Private person who owns the app instance and delegates access to it.
  delegator: instansDelegation.instance_from_user.user_1,

  // Private person who receives access in the scenarios. No other suite pairs this person
  // with the delegator, so the scenarios own the connection between the two.
  receiver: fromBeOmTilgang(beOmTilgang.Bot_package_from_person),

  // Private person with no relation to the delegator, used for authorization negatives.
  outsider: fromBeOmTilgang(beOmTilgang.Bot_from_person),

  // App instance owned by the delegator.
  app: {
    appId: instansDelegation.app.app_id,
    instanceUrn: instansDelegation.app.instans_urn,
  },

  // Single resource a private person can delegate (read and write).
  resource: {
    refId: beOmTilgang.resource_to_delegate.resource_id_privatperson,
  },
};
