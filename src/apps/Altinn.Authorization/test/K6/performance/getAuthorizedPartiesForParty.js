import http from 'k6/http';
import { SharedArray } from "k6/data";
import { getAuthorizedPartiesUrl, tokenGeneratorEnv } from "./common/config.js";
import { expect, describe, randomItem, URL, getEnterpriseToken } from "./common/testimports.js";
import { buildOptions, getParams, readCsv } from "./commonFunctions.js";

const randomize = (__ENV.RANDOMIZE ?? 'false') === 'true';
const env = __ENV.API_ENVIRONMENT ?? 'yt01';
const byOrganization = (__ENV.BY_ORGANIZATION ?? 'true') === 'true';
const byUser = (__ENV.BY_USER ?? 'true') === 'true';
const includeAltinn2 = (__ENV.INCLUDE_ALTINN2 ?? 'false') === 'true';

const partiesFilename = `./testData/orgsIn-${env}-WithPartyUuid.csv`;
const parties = new SharedArray('parties', function () {
  return readCsv(partiesFilename);
});

const getAuthorizedPartiesByOrgLabel = "Get authorized parties by organization";
const getAuthorizedPartiesByUserLabel = "Get authorized parties by user";
const labels = [getAuthorizedPartiesByOrgLabel, getAuthorizedPartiesByUserLabel];

export let options = buildOptions(labels);
  
export function setup() {
    const tokenOpts = {
        scopes: "altinn:accessmanagement/authorizedparties.resourceowner",
        orgNo: "713431400",
        env: tokenGeneratorEnv
    }
    const token = getEnterpriseToken(tokenOpts);
    return token;
}

export function getParty() {
  if (randomize) { return randomItem(parties) } 
  else { return parties[__ITER % parties.length] };
}

export default function (token) {
    const userParty = getParty();
    const paramsForOrg = getParams(getAuthorizedPartiesByOrgLabel);
    paramsForOrg.headers.Authorization = "Bearer " + token;
    const paramsForUser = getParams(getAuthorizedPartiesByUserLabel);
    paramsForUser.headers.Authorization = "Bearer " + token;

    const bodyForOrg = {
        "type": "urn:altinn:partyid",
        "value": userParty.orgNo
    }

    const bodyForUser = {
        "type": "urn:altinn:person:identifier-no",
        "value": userParty.ssn
    }

    if (byOrganization) { 
      getAuthorizedParties(bodyForOrg, paramsForOrg, userParty.orgNo);
    }
    if (byUser) {
      getAuthorizedParties(bodyForUser, paramsForUser, userParty.ssn);
    } 
}

export function getAuthorizedParties(body, params, party) {
  const url = new URL(getAuthorizedPartiesUrl);
  if (includeAltinn2) {
      url.searchParams.append('includeAltinn2', 'true');
  }
  describe('Get authorized parties', () => {
      let r = http.post(url.toString(), JSON.stringify(body), params);
      if (r.timings.duration > 10000.0) {
          console.log(__ITER, party, r.timings.duration, r.json().length);
      }
      if (r.status != 200) {
        console.log(__ITER, party);
        console.log(r.status, r.status_text);
        console.log(r.body);
      }
      expect(r.status, "response status").to.equal(200);
      expect(r, 'response').to.have.validJsonBody();
  }); 
}

