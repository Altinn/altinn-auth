import http from 'k6/http';
import { SharedArray } from "k6/data";
import { getAuthorizedPartiesUrl } from "./common/config.js";
import { expect, describe, randomItem, URL, getEnterpriseToken } from "./common/testimports.js";
import { buildOptions, getParams, readCsv } from "./commonFunctions.js";


const systemusersFilename = `./testData/systemusers.csv`;

const systemUsers = new SharedArray('systemusers', function () {
  return readCsv(systemusersFilename);
});

const getAuthorizedPartiesLabel = "Get authorized parties";
const labels = [getAuthorizedPartiesLabel];

export let options = buildOptions(labels);
  
export function setup() {
    const tokenOpts = {
        scopes: "altinn:accessmanagement/authorizedparties.resourceowner",
        orgNo: "713431400"
    }
    const token = getEnterpriseToken(tokenOpts);
    return token;
}

export default function (token) {
    const systemUser = randomItem(systemUsers);
    getAuthorizedParties(systemUser, token);
}

function getAuthorizedParties(systemUser, token) {
    const params = getParams(getAuthorizedPartiesLabel);
    params.headers.Authorization = "Bearer " + token;

    const body = {
        "type": "urn:altinn:systemuser:uuid",
        "value": systemUser.systemuserUuid
    }

    const url = new URL(getAuthorizedPartiesUrl);
    describe('Get authorized parties', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        if (r.timings.duration > 2000.0) {
            console.log(__ITER, systemUser.systemuserUuid, r.timings.duration, r.json().length);
        }
        if (r.status != 200) {
            console.log(r.status, r.status_text);
            console.log(r.body);
        }
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
    });
}

