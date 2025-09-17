import http from 'k6/http';
import exec from 'k6/execution';
import { SharedArray } from "k6/data";
import { getAmDelegationUrl } from "./common/config.js";
import { expect, describe, randomItem, URL, getPersonalToken, randomIntBetween } from "./common/testimports.js";
import { buildOptions, getParams, breakpoint, stages_target, readCsv } from "./commonFunctions.js";


const orgsWithPartyUuidFilename = `./testData/orgsIn-yt01-WithPartyUuid.csv`;

const orgsWithPartyUuid = new SharedArray('orgs2', function () {
  return readCsv(orgsWithPartyUuidFilename);
});

const getDelegationsLabel = "Get delegations";
const labels = [getDelegationsLabel];

export const regnskapsforerPackages = [
    'regnskapsforer-med-signeringsrettighet',
    'regnskapsforer-uten-signeringsrettighet',
    'regnskapsforer-lonn',
  ];
  export const revisorPackages = [
    'ansvarlig-revisor',
    'revisormedarbeider',
  ];
  export const forretningsforerPackages = ['forretningsforer-eiendom'];

export let options = buildOptions(labels);

function endUsersPart(totalVus, vuId) {
    const endUsersLength = orgsWithPartyUuid.length;
    if (totalVus == 1) {
        return orgsWithPartyUuid.slice(0, endUsersLength);
    }
    let usersPerVU = Math.floor(endUsersLength / totalVus);
    let extras = endUsersLength % totalVus;
    let ixStart = (vuId-1) * usersPerVU;
    if (vuId <= extras) {
        usersPerVU++;
        ixStart += vuId - 1;
    }
    else {
        ixStart += extras;
    }
    return orgsWithPartyUuid.slice(ixStart, ixStart + usersPerVU);
}
  
export function setup() {
    let totalVus = 1;
    if (breakpoint) {
        totalVus = stages_target;
    } else {
        totalVus = exec.test.options.scenarios.default.vus;
    }
    let parts = [];
    for (let i = 1; i <= totalVus; i++) {
        parts.push(endUsersPart(totalVus, i));
    }
    return parts;
}

export default function (data) {
    const systemUser = randomItem(data[exec.vu.idInTest - 1]);
    getDelegations(systemUser);
}

function getDelegations(systemUser) {
    const params = getParams(getDelegationsLabel);
    params.headers.Authorization = "Bearer " + getToken(systemUser);

    const url = new URL(getAmDelegationUrl + "/clients");
    url.searchParams.append('party', systemUser.orgUuid);
    add_queryParams(url, systemUser);
    let delegations = [];
    describe('Get delegations', () => {
        let r = http.get(url.toString(), params);
        delegations = r.json();
        expect(r.status, "response status").to.equal(200);
        expect(r, 'response').to.have.validJsonBody();
    });
    return delegations;
}

function add_queryParams(url, systemUser) {
    if (systemUser.orgType == "regnskapsforer") {
        url.searchParams.append('roles', 'regnskapsforer');
        let r = randomIntBetween(0, 3);
        if (r == 1) {
            url.searchParams.append('packages', randomItem(regnskapsforerPackages));
        } else if (r == 2) {
            let elem1 = randomItem(regnskapsforerPackages);
            let elem2 = randomItem(regnskapsforerPackages.filter(item => item !== elem1));
            url.searchParams.append('packages', elem1);
            url.searchParams.append('packages', elem2);
        } else if (r == 3) {
            url.searchParams.append('packages', regnskapsforerPackages[0]);
            url.searchParams.append('packages', regnskapsforerPackages[1]);
            url.searchParams.append('packages', regnskapsforerPackages[2]);
        }
    }
    if (systemUser.orgType == "forretningsforer") {
        url.searchParams.append('roles', 'forretningsforer');
        let r = randomIntBetween(0, 2);
        if (r < 2) {
            url.searchParams.append('packages', forretningsforerPackages[0]);
        }
    }
    if (systemUser.orgType == "revisor") {
        url.searchParams.append('roles', 'revisor');
        let r = randomIntBetween(0, 2);
        if (r == 1) {
            url.searchParams.append('packages', randomItem(revisorPackages));
        } else if (r == 2) {
            url.searchParams.append('packages', revisorPackages[0]);
            url.searchParams.append('packages', revisorPackages[1]);
        } 
    }
}


function getToken (systemUser) {
    const tokenOptions = {
        scopes: "altinn:portal/enduser",
        userid: systemUser.userId,
        partyid: systemUser.partyId,
        partyuuid: systemUser.partyUuid,
        ssn: systemUser.ssn,
    }
    return getPersonalToken(tokenOptions);
} 