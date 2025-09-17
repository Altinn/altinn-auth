import { getParams, buildOptions } from "./commonFunctions.js";
import { getAuthorizedParties, getParty } from "./getAuthorizedPartiesForParty.js"
export { setup } from "./getAuthorizedPartiesForParty.js"

const getAuthorizedPartiesByUserLabel = "Get authorized parties by user";
const labels = [ getAuthorizedPartiesByUserLabel];

export let options = buildOptions(labels);

export default function (token) {
  const party = getParty()
  const paramsForUser = getParams(getAuthorizedPartiesByUserLabel);
  paramsForUser.headers.Authorization = "Bearer " + token;
  const bodyForUser = {
      "type": "urn:altinn:person:identifier-no",
      "value": party.ssn
  }
  getAuthorizedParties(bodyForUser, paramsForUser, party.ssn);
}
