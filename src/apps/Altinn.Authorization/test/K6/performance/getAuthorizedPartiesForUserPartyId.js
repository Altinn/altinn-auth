import { SharedArray } from "k6/data";
import { randomItem } from "./common/testimports.js";
import { getParams, buildOptions, readCsv } from "./commonFunctions.js";
import { getAuthorizedParties } from "./getAuthorizedPartiesForParty.js"
export { setup } from "./getAuthorizedPartiesForParty.js"

const randomize = (__ENV.RANDOMIZE ?? 'false') === 'true';

const partiesFilename = `./testData/userPartyIds`;
const parties = new SharedArray('parties', function () {
  return readCsv(partiesFilename);
});

const getAuthorizedPartiesByUserLabel = "Get authorized parties by userPartyId";
const labels = [ getAuthorizedPartiesByUserLabel];

function getParty() {
  if (randomize) { return randomItem(parties) } 
  else { return parties[__ITER % parties.length] };
}
export let options = buildOptions(labels);

export default function (token) {
  const party = getParty()
  const paramsForUser = getParams(getAuthorizedPartiesByUserLabel);
  paramsForUser.headers.Authorization = "Bearer " + token;
  const bodyForUser = {
      "type": "urn:altinn:partyid",
      "value": party.userPartyId
  }
  getAuthorizedParties(bodyForUser, paramsForUser, party.userPartyId);
}
