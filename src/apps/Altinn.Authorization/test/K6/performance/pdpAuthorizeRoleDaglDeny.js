import http from 'k6/http';
import exec from 'k6/execution';
import { SharedArray } from "k6/data";
import { expect, randomIntBetween, URL, describe } from "./common/testimports.js";
import { postAuthorizeUrl } from './common/config.js';
import { buildDaglAuthorizeBody } from './testData/buildAuthorizeBody.js';
import { buildOptions, getAuthorizeParams, getAuthorizeClientToken, getActionLabelAndExpectedResponseForDaglDeny, readCsv } from "./commonFunctions.js";

const resource = "ttd-dialogporten-performance-test-02";
const daglFilename = `./testData/OrgsDagl.csv`;
export const dagl = new SharedArray('dagl', function () {
  return readCsv(daglFilename);
});

export let options = buildOptions();

export default function() {
    const part = exec.vu.idInTest % (dagl.length/50);
    const clientIndex = (randomIntBetween(part*50, ((part+1)*50)) - 1) % dagl.length;
    const orgIndex = (randomIntBetween(part*50, ((part+1)*50)) - 1) % dagl.length;
    const client = dagl[clientIndex];
    const org = dagl[orgIndex];
    const [action, label, expectedResponse] = getActionLabelAndExpectedResponseForDaglDeny(orgIndex, clientIndex); 
    const token = getAuthorizeClientToken(client);
    const params = getAuthorizeParams(label, token);
    const body = buildDaglAuthorizeBody(client.SSN, resource, org.OrgNr, action);
    const url = new URL(postAuthorizeUrl);
    describe('PDP Authorize', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        let response = JSON.parse(r.body);
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody(); 
        expect(response.response[0].decision).to.equal(expectedResponse); 
    });   
}

 