import http from 'k6/http';
import exec from 'k6/execution';
import { SharedArray } from "k6/data";
import { expect, randomIntBetween, URL, describe } from "./common/testimports.js";
import { postAuthorizeUrl } from './common/config.js';
import { buildPrivAuthorizeBody } from './testData/buildAuthorizeBody.js';
import { buildOptions, getAuthorizeParams, getActionLabelAndExpectedResponse, getAuthorizeClientToken, readCsv } from "./commonFunctions.js";

// resource with read/write for PRIV and DAGL
const resource = "ttd-dialogporten-performance-test-02";
const noOfClientsPerVu = 50;

const daglFilename = `./testData/OrgsDagl.csv`;
export const dagl = new SharedArray('dagl', function () {
  return readCsv(daglFilename);
});
export let options = buildOptions();

export default function() {
    const part = exec.vu.idInTest % (dagl.length/noOfClientsPerVu)
    const clientIndex = (randomIntBetween(part*noOfClientsPerVu, ((part+1)*noOfClientsPerVu)) - 1) % dagl.length;
    const client = dagl[clientIndex];
    const [action, label, expectedResponse] = getActionLabelAndExpectedResponse(); 
    const token = getAuthorizeClientToken(client);
    const params = getAuthorizeParams(label, token);
    const body = buildPrivAuthorizeBody(client.SSN, resource, action);
    const url = new URL(postAuthorizeUrl);
    describe('PDP Authorize', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        let response = JSON.parse(r.body);
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody(); 
        expect(response.response[0].decision).to.equal(expectedResponse); 
    });   
}