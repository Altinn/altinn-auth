import http from 'k6/http';
import { SharedArray } from "k6/data";
import { getRightHoldersUrl, tokenGeneratorEnv } from "./common/config.js";
import { expect, describe, randomItem, URL, getPersonalToken } from "./common/testimports.js";
import { buildOptions, getParams, readCsv } from "./commonFunctions.js";

const randomize = (__ENV.RANDOMIZE ?? 'false') === 'true';
const env = __ENV.API_ENVIRONMENT ?? 'yt01';
const partiesFilename = `./testData/orgsIn-${env}-WithPartyUuid.csv`;

const parties = new SharedArray('parties', function () {
  return readCsv(partiesFilename);
});

const getRightHoldersLabel = "Get right holders";
const labels = [ getRightHoldersLabel ];

export let options = buildOptions(labels);
  
function getToken(userId) {
  const tokenOpts = {
      scopes: "altinn:portal/enduser",
      userId: userId,
      env: tokenGeneratorEnv
  }
  const token = getPersonalToken(tokenOpts);
  return token;
}

export default function () {
  const userParty = (() => { 
    if (randomize) { return randomItem(parties) } 
    else { return parties[__ITER % parties.length] };
  }
  )();
  getRightHolders(userParty);
}

function getRightHolders(userParty) {
  const token = getToken(userParty.userId);
  const params = getParams(getRightHoldersLabel);
  params.headers.Authorization = "Bearer " + token;
  const url = new URL(getRightHoldersUrl);
  url.searchParams.append('party', userParty.orgUuid);
  url.searchParams.append('to', userParty.orgUuid);
  describe('Get right holders', () => {
      let r = http.get(url.toString(), params);
      expect(r.status, "response status").to.equal(200);
      expect(r, 'response').to.have.validJsonBody();
  });
}

