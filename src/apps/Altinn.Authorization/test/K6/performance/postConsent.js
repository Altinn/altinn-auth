import http from 'k6/http';
import { SharedArray } from "k6/data";
import { postConsent, postConsentApprove, env } from './common/config.js';
import { expect, describe, randomItem, randomIntBetween, URL, uuidv4 } from "./common/testimports.js";
import { buildOptions, readCsv, getConsentRequestToken, getConsentApproveToken, getAuthorizeParams } from './commonFunctions.js';

const orgsDaglFilename = `./testData/orgsIn-${env}-WithPartyUuid.csv`;
const orgsDagl = new SharedArray('orgsDagl', function () {
  return readCsv(orgsDaglFilename);
});

const requestlabelorg = "consentrequests org";
const requestlabelpriv = "consentrequests priv";
const approvelabelorg = "consentapprove org";
const approvelabelpriv = "consentapprove priv";
const labels = [requestlabelorg, requestlabelpriv, approvelabelorg, approvelabelpriv];

export let options = buildOptions(labels);

export default function() {
    const from = randomItem(orgsDagl);
    let to;
    do {
        to = randomItem(orgsDagl);
    } while (to === from);
    const [id, approvelabel] = requestConsent(from, to);
    approveConsent(from, id, approvelabel);
    
}

function requestConsent(from, to) {
    const resource = "samtykke-performance-test";
    const token = getConsentRequestToken(to.orgNo);
    const fromOrg = `urn:altinn:organization:identifier-no:${from.orgNo}`;
    const toOrg = `urn:altinn:organization:identifier-no:${to.orgNo}`;
    const fromPerson = `urn:altinn:person:identifier-no:${from.ssn}`;
    let fromSome = fromPerson;
    let requestlabel = requestlabelpriv;
    let approvelabel = approvelabelpriv;
    if (randomIntBetween(0, 100) <= 10) {
      fromSome = fromOrg;
      requestlabel = requestlabelorg;
      approvelabel = approvelabelorg;
    }
    const params = getAuthorizeParams(requestlabel, token);
    const body = getBody(fromSome, toOrg, null, null, resource, "consent");
    const url = new URL(postConsent);
    describe('POST Consent', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        expect(r.status, "response status").to.equal(201);
        expect(r, 'response').to.have.validJsonBody(); 
    });
    return [body.id, approvelabel];   
}

function approveConsent(from, id, approvelabel) {
    const token = getConsentApproveToken(from);
    const params = getAuthorizeParams(approvelabel, token);
    const body = { "language": "nb" };
    const url = new URL(`${postConsentApprove}${id}/accept`);
    describe('POST Consent Approve', () => {
        let r = http.post(url.toString(), JSON.stringify(body), params);
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody(); 
    });
}

function getBody(from, to, delegator, handledBy, resource, action) {
    const id = uuidv4();
    const body = {
        "id": id,
        "from": from, 
        "to": to,
        "validTo": new Date(Date.now() + 7*24*60*60*1000).toISOString(),
        "consentRights": [
            {
                "action": [action],
                "resource": [
                    {
                        "type": "urn:altinn:resource",
                        "value": resource
                    } //Vil alltid være bare en
                ],
                "metaData": {
                  "inntektsaar" : "2026"
                }
            }
        ],
        "redirectUrl": "https://altinn.no"
    }
    if (delegator) {
        body.requiredDelegator = delegator
    }
    if (handledBy) {
        body.handledBy = handledBy;
    }
    return body;
}