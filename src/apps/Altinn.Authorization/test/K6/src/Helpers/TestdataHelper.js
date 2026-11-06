import { check } from 'k6';
import { addErrorCount } from '../errorcounter.js';
import * as authorization from '../api/platform/authorization/authorization.js';
import * as setUpData from '../setup.js';

let pdpInputJson = open('../data/pdpinput.json');

/**
 * Helper function for quickly checking whether the PDP's decision is 'permit' or something else
 * @param {*} offeredByPartyId The partyid in the Resource
 * @param {*} coveredBy  The AccessSubject's userid/partyid
 * @param {*} taskName 'Task_1', 'EndEvent_1'
 * @param {*} actionName 'read', 'write', 'sign'
 * @param {*} expectedDecision 'Permit', 'NotApplicable'
 */
export function checkPDPDecision(offeredByPartyId, coveredBy, taskName, actionName, expectedDecision, showResults, appOwner, appName, testTitle) {
  var jsonPermitData = {
    AccessSubject: ['urn:altinn:userid'],
    Action: [actionName],
    Resource: ['urn:altinn:app', 'urn:altinn:org', 'urn:altinn:partyid', 'urn:altinn:task'],
  };
  var res = authorization.postGetDecision(pdpInputJson, jsonPermitData, appOwner, appName, coveredBy, offeredByPartyId, taskName);

  // Assert
  var success = check(res, {
    [`${testTitle} - Get PDP Decision for delegated rule Status is 200`]: (r) => r.status === 200,
    [`${testTitle} - Get PDP Decision for delegated rule - decision is permit`]: (r) => r.json('response.0.decision') === expectedDecision,
  });

  if(!success && showResults) {
    console.log('Expected decision to be ' + expectedDecision + ', but it was not.');
  }

  // console.log('Decision was ' + expectedDecision + ', as expected.');
  addErrorCount(success);
  return success;
}

export function minimumSBLVersion(major, minor) {
  var altinnBuildVersion = setUpData.getSBLBuildVersion(); 
  if (altinnBuildVersion.split('.')[0] >= major && altinnBuildVersion.split('.')[1] >= minor) {
    return true;
  }
  return false;
}