/*
  Command: docker-compose run k6 run src/tests/policyDecisionPoint.js
  -e env=*** -e tokengenuser=*** -e tokengenuserpwd=*** -e appsaccesskey=*** 
*/
import { check, sleep, fail } from 'k6';
import { addErrorCount, stopIterationOnFail } from '../errorcounter.js';
import * as delegation from '../api/platform/authorization/delegations.js';
import { generateToken } from '../api/altinn-testtools/token-generator.js';
import { generateJUnitXML, reportPath } from '../report.js';
import * as helper from '../Helpers/TestdataHelper.js';

const environment = __ENV.env.toLowerCase();
const tokenGeneratorUserName = __ENV.tokengenuser;
const tokenGeneratorUserPwd = __ENV.tokengenuserpwd;

let testDataFile = open(`../data/testdata/${environment}testdata.json`);
var testdata = JSON.parse(testDataFile);
var org1;
var org2;
var org3;
var token;
var org;
var app;
var showResults;

export const options = {
  thresholds: {
    errors: ['count<1'],
  },
  setupTimeout: '1m',
};

export function setup() {
  var tokenGenParams = {
    env: environment,
    app: 'sbl.authorization',
  };

  testdata.token = generateToken('platform', tokenGeneratorUserName, tokenGeneratorUserPwd, tokenGenParams);
  return testdata;

}

//Tests for platform Authorization:Delegations:Inheritance
export default function (data) {
  org1 = data.org1;
  org2 = data.org2;
  org3 = data.org3;
  token = data.token;
  org = data.org;
  app = data.app;
  showResults = 0;

  //tests
  directDelegationFromOrgToUser();
  directDelegationFromOrgToOrg();
  directDelegationFromMainUnitToUser();
  directDelegationFromMainUnitToOrg();
  directDelegationFromMainUnitToOrgInheritedByDAGLViaKeyRole();
  delegationToOrgIsInheritedByECUserViaKeyrole();
}

export function CleanupBeforeTests() {
  helper.deleteAllRules(token, org1.dagl.userid, org1.partyid, org2.dagl.userid, 'userid', org, app);
  helper.deleteAllRules(token, org1.dagl.userid, org1.partyid, org2.partyid, 'partyid', org, app);
  helper.deleteAllRules(token, org3.dagl.userid, org3.partyid, org2.dagl.userid, 'userid', org, app);
  helper.deleteAllRules(token, org3.dagl.userid, org3.partyid, org2.partyid, 'partyid', org, app);
}

/**
 * Tests that PDP returns "Permit" for delegations between organization (org1) and user (user2)
 */
 export function directDelegationFromOrgToUser() {
  // Arrange
  const performedByUserId = org1.dagl.userid;
  const offeredByPartyId = org1.partyid;
  const coveredByUserId = org2.dagl.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:userid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, offeredByPartyId, coveredByUserId, resources, null, null);
  if (res.body.includes('[]')) {
    // console.log('getrules returned empty body, adding rule')
    helper.addRulesForTest(token, performedByUserId, offeredByPartyId, coveredByUserId, 'userid', 'Task_1', 'read', org, app);
  }
  
  // Act
  var success = helper.checkPDPDecision(offeredByPartyId, coveredByUserId, 'Task_1', 'read', 'Permit', showResults, org, app, "directDelegationFromOrgToUser");
  
  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('directDelegationFromOrgToUser:' + success)}
}

/**
 * Tests that PDP returns "Permit" for delegations between two organizations (org1 and org2)
 */
export function directDelegationFromOrgToOrg() {
  // Arrange
  const performedByUserId = org1.dagl.userid;
  const offeredByPartyId = org1.partyid;
  const coveredByPartyId = org2.partyid;
  const DAGLUserIdForCoveredBy= org2.dagl.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:partyid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, offeredByPartyId, coveredByPartyId, resources, null, null);
  if (res.body.includes('[]')) {
    helper.addRulesForTest(token, performedByUserId, offeredByPartyId, coveredByPartyId, 'partyid', 'Task_1', 'read', org, app);
  }

  // Act
  var success = helper.checkPDPDecision(offeredByPartyId, DAGLUserIdForCoveredBy, 'Task_1', 'read', 'Permit', showResults, org, app, "directDelegationFromOrgToOrg");

  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('directDelegationFromOrgToOrg:' + success)}
}

/**
 * Tests that PDP returns "Permit" when org3 delegates to user2, and that user tries to access the organization's subunit (org3.subunit)
 */
export function directDelegationFromMainUnitToUser() {
  // Arrange
  const performedByUserId = org3.dagl.userid;
  const offeredByParentPartyId = org3.partyid;
  const subUnitPartyId = org3.subunit.partyid;
  const coveredByUserId = org2.dagl.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:userid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, subUnitPartyId, coveredByUserId, resources, offeredByParentPartyId, null);
  if (res.body.includes('[]')) {
    helper.addRulesForTest(token, performedByUserId, offeredByParentPartyId, coveredByUserId, 'userid', 'Task_1', 'read', org, app);
  }

  // Act
  var success = helper.checkPDPDecision(subUnitPartyId, coveredByUserId, 'Task_1', 'read', 'Permit', showResults, org, app, "directDelegationFromMainUnitToUser");

  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('directDelegationFromMainUnitToUser:' + success)}
}

/**
 * Tests that PDP returns "Permit" when org3 delegates to org2, and the DAGL for org2 tries to access the organization's subunit (org3.subunit)
 */
export function directDelegationFromMainUnitToOrg() {
  // Arrange
  const performedByUserId = org3.dagl.userid;
  const offeredByParentPartyId = org3.partyid;
  const subUnitPartyId = org3.subunit.partyid;
  const coveredByPartyId = org2.partyid;
  const DAGLUserIdForCoveredBy= org2.dagl.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:partyid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, subUnitPartyId, coveredByPartyId, resources, offeredByParentPartyId, null);
  if (res.body.includes('[]')) {
    helper.addRulesForTest(token, performedByUserId, offeredByParentPartyId, coveredByPartyId, 'partyid', 'Task_1', 'read', org, app);
  }

  // Act
  var success = helper.checkPDPDecision(subUnitPartyId, DAGLUserIdForCoveredBy, 'Task_1', 'read', 'Permit', showResults, org, app, "directDelegationFromMainUnitToOrg");

  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('directDelegationFromMainUnitToOrg:' + success)}
}

/**
 * Tests that PDP returns "Permit" when org3 delegates to org2, and the DAGL for org2 tries to access the organization's subunit (org3.subunit) via keyrole
 */
export function directDelegationFromMainUnitToOrgInheritedByDAGLViaKeyRole() {
  // Arrange
  const performedByUserId = org3.dagl.userid;
  const offeredByParentPartyId = org3.partyid;
  const subUnitPartyId = org3.subunit.partyid;
  const coveredByPartyId = org2.partyid;
  const DAGLUserIdForCoveredBy= org2.dagl.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:userid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, subUnitPartyId, DAGLUserIdForCoveredBy, resources, offeredByParentPartyId, [coveredByPartyId]);
  if (res.body.includes('[]')) {
    helper.addRulesForTest(token, performedByUserId, offeredByParentPartyId, coveredByPartyId, 'partyid', 'Task_1', 'read', org, app);
  }

  // Act
  var success = helper.checkPDPDecision(subUnitPartyId, DAGLUserIdForCoveredBy, 'Task_1', 'read', 'Permit', showResults, org, app, "directDelegationFromMainUnitToOrgInheritedByDAGLViaKeyRole");
  
  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('directDelegationFromMainUnitToOrgInheritedByDAGLViaKeyRole:' + success)}
}

/**
 * Verifies that when a delegation is made from one org (org1) to another (org2), the Enterprise Certificate user (ECUser) for that organization is also given access
 */
export function delegationToOrgIsInheritedByECUserViaKeyrole() {

  // Arrange
  const performedByUserId = org1.dagl.userid;
  const offeredByPartyId = org1.partyid;
  const coveredByPartyId = org2.partyid;
  const ecUserIdForCoveredBy= org2.ecuser.userid;
  var policyMatchKeys = {
    coveredBy: 'urn:altinn:userid',
    resource: ['urn:altinn:app', 'urn:altinn:org'],
  };
  var resources = [{ appOwner: org, appName: app }];
  var res = delegation.getRules(token, policyMatchKeys, offeredByPartyId, ecUserIdForCoveredBy, resources, null, [coveredByPartyId]);
  if (res.body.includes('[]')) {
    helper.addRulesForTest(token, performedByUserId, offeredByPartyId, coveredByPartyId, 'partyid', 'Task_1', 'read', org, app);
  }
  
  // Act
  var success = helper.checkPDPDecision(offeredByPartyId, ecUserIdForCoveredBy, 'Task_1', 'read', 'Permit', showResults, org, app,"delegationToOrgIsInheritedByECUserViaKeyrole");
  // Assert
  addErrorCount(success);
  if(showResults == 1) {console.log('delegationToOrgIsInheritedByECUserViaKeyrole:' + success)}
}

export function handleSummary(data) {
  let result = {};
  result[reportPath('policyDecisionPoint.xml')] = generateJUnitXML(data, 'policy-decision-point');
  return result;
}

export function showTestData() {
console.log('token: ' + token);
console.log('org1.orgno: ' + org1.orgno);
console.log('org1.partyid: ' + org1.partyid);
console.log('org2.orgno: ' + org2.orgno);
console.log('org2.partyid: ' + org2.partyid);
console.log('org3.orgno: ' + org3.orgno);
console.log('org3.partyid: ' + org3.partyid);
console.log('org3.subunit.orgno: ' + org3.subunit.orgno);
console.log('org3.subunit.partyid: ' + org3.subunit.partyid);
console.log('org1.dagl.userid: ' + org1.dagl.userid);
console.log('org1.dagl.partyid: ' + org1.dagl.partyid);
console.log('org2.dagl.userid: ' + org2.dagl.userid);
console.log('org2.dagl.partyid: ' + org2.dagl.partyid);
console.log('org3.dagl.userid: ' + org3.dagl.userid);
console.log('org3.dagl.partyid: ' + org3.dagl.partyid);
console.log('org2.ecuser.userid: ' + org2.ecuser.userid);
console.log('org2.ecuser.partyid: ' + org2.ecuser.partyid);
}
