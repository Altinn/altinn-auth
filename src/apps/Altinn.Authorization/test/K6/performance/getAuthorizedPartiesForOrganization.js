import { getParams, buildOptions } from "./commonFunctions.js";
import { getAuthorizedParties, getParty } from "./getAuthorizedPartiesForParty.js"
export { setup } from "./getAuthorizedPartiesForParty.js"

const getAuthorizedPartiesByOrganizationLabel = "Get authorized parties by organization";
const labels = [ getAuthorizedPartiesByOrganizationLabel];

export let options = buildOptions(labels);

export default function (token) {
  const party = getParty()
  const paramsForUser = getParams(getAuthorizedPartiesByOrganizationLabel);
  paramsForUser.headers.Authorization = "Bearer " + token;
  const bodyForUser = {
      "type": "urn:altinn:person:identifier-no",
      "value": party.orgNo
  }
  getAuthorizedParties(bodyForUser, paramsForUser, party.orgNo);
}
