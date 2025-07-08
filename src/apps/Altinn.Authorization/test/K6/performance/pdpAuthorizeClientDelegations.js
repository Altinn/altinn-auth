import http from 'k6/http';
import { SharedArray } from "k6/data";
import { expect, describe, randomItem, URL } from "./common/testimports.js";
import { postAuthorizeUrl } from './common/config.js';
import { buildClientDelegationAuthorizeBody } from './testData/buildAuthorizeBody.js';
import { buildOptions, getAuthorizeParams, getActionLabelAndExpectedResponse, getAuthorizeToken, readCsv } from "./commonFunctions.js";

const regnResources = "ttd-performance-clientdelegation";
const fforResource = "ttd-performance-clientdelegation-ffor";
const revResource = "ttd-performance-clientdelegation-revisor";

const systemUsersFilename = `./testData/customers.csv`;
const systemUsers = new SharedArray('systemUsers', function () {
  return readCsv(systemUsersFilename);
});

export let options = buildOptions();

export default function() {
    const client = randomItem(systemUsers);
    const resource = getResource(client);
    if (!resource) {
        console.log(`No resource for ${client.role}`);
        return;
    }
    const [action, label, expectedResponse] = getActionLabelAndExpectedResponse(); 
    const token = getAuthorizeToken(client);
    const params = getAuthorizeParams(label, token);
    const body = buildClientDelegationAuthorizeBody(client.systemUserId, resource, client.customerOrgNo, action);
    const url = new URL(postAuthorizeUrl);
    describe('PDP Authorize', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        let response = JSON.parse(r.body);
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody(); 
        expect(response.response[0].decision).to.equal(expectedResponse); 
    });   
}

function getResource(client) {
    switch (client.role) {
        case "regnskapsforer":
            return regnResources;
        case "forretningsforer":
            return fforResource;
        case "revisor":
            return revResource;
        default:
            return null;
    }
}   