import http from 'k6/http';
import exec from 'k6/execution';
import { SharedArray } from "k6/data";
import { expect, randomIntBetween, URL, describe } from "./common/testimports.js";
import { postAuthorizeUrl } from './common/config.js';
import { buildDaglAuthorizeBody } from './testData/buildAuthorizeBody.js';
import { buildOptions, getAuthorizeParams, getActionLabelAndExpectedResponse, getAuthorizeClientToken, readCsv} from "./commonFunctions.js";

// resource with read/write for PRIV and DAGL
const resource = "ttd-dialogporten-performance-test-02";
const noOfClientsPerVu = 50;
const daglFilename = `./testData/OrgsDagl.csv`;
export const dagl = new SharedArray('dagl', function () {
  return readCsv(daglFilename);
});

export let options = buildOptions();

export default function() {
    // Use a limited number of clients pr VU, to avoid calling getToken for each authorize-request
    const part = exec.vu.idInTest % (dagl.length/noOfClientsPerVu);

    // Pick random client from the list of clients
    const clientIndex = (randomIntBetween(part*noOfClientsPerVu, ((part+1)*noOfClientsPerVu)) - 1) % dagl.length;
    const client = dagl[clientIndex];

    // Get action, label and expected response
    const [action, label, expectedResponse] = getActionLabelAndExpectedResponse(); 

    // Get params, token, request body and url
    const params = getAuthorizeParams(label, getAuthorizeClientToken(client));
    const body = buildDaglAuthorizeBody(client.SSN, resource, client.OrgNr, action);
    const url = new URL(postAuthorizeUrl);
    const data = {
        client: client,
        action: action,
        label: label,
        expectedResponse: expectedResponse,
        resource: resource,
        requestBody: body
    };
    //altinnK6Lib.postSlackMessage(data)


    // Run request and check response
    describe('PDP Authorize', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        let response = JSON.parse(r.body);
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody(); 
        expect(response.response[0].decision, `'${response.response[0].decision}'`).to.equal(expectedResponse); 
    });   
}