// Shared helpers for the v2 resource request scenarios with right keys.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

// Paths of the end user request API. Version 2 is behind the feature flag
// AccessManagement.Enduser.RequestV2Api.
const paths = {
	v1: "/accessmanagement/api/v1/enduser/request",
	v2: "/accessmanagement/api/v2/enduser/request",
	connections: "/accessmanagement/api/v1/enduser/connections",
};

// Environments where AccessManagement.Enduser.RequestV2Api is on and the seed data is verified.
// TT02 has seed data, but stays out until the flag is on there.
const enabledEnvironments = ["at22"];

// Skips the current request outside the enabled environments. Returns true when skipped, so the
// step can return before it loads seed data or reads variables that earlier, skipped steps never
// set. bru.runner.skipRequest() only marks the request as skipped: the rest of the pre-request
// script still runs, and an error there fails the run instead of skipping the request.
function skipOutsideEnabledEnvironments() {
	if (enabledEnvironments.includes(bru.getEnvVar("tokenEnv"))) {
		return false;
	}

	bru.runner.skipRequest();
	return true;
}

// Maps a person entry from the shared testdata files to the shape the steps use.
// Party UUIDs are lowercased because the API returns them in lowercase.
function person(p) {
	return {
		pid: p.pid,
		userId: p.userid,
		partyId: p.partyid,
		partyUuid: p.partyuuid.toLowerCase(),
	};
}

// Returns the scenario personas for the environment the run targets.
function testdata() {
	const data = require(`../../../testdata/delegationrequest-scenarios/${bru.getEnvVar("tokenEnv")}.js`).resourceRequestRightKeysV2;
	return {
		resource: data.resource,
		requester: {
			name: data.requester.name,
			orgNo: data.requester.org_no,
			partyUuid: data.requester.partyuuid.toLowerCase(),
			innehaver: person(data.requester.innehaver),
		},
		approver: person(data.approver),
	};
}

// Mints a personal token for a persona and stores it as the collection bearer token.
async function loginAs(persona, authScopes) {
	const token = await tokenGenerator.getToken({
		auth_userId: persona.userId,
		auth_partyId: persona.partyId,
		auth_partyUuid: persona.partyUuid,
		auth_ssn: persona.pid,
		auth_tokenType: sharedtestdata.authTokenType.personal,
		auth_scopes: authScopes || sharedtestdata.auth_scopes.portalEnduser,
	});
	bru.setVar("bearerToken", token);
}

// Clears the variables the steps pass between them, so a step never acts on a value
// left behind by an earlier run.
function clearScenarioVars() {
	for (const name of ["rk_requestId", "rk_keyA", "rk_keyB"]) {
		bru.setVar(name, null);
	}
}

// Brings the pair back to a known state, with the approver's token: rejects the pending
// requests from the requester for the resource, so creating a request starts a new one
// instead of returning an old one with other right keys, and removes the resource the
// approver may have delegated to the requester in an earlier run.
async function resetPair(axios, td, pendingRequests) {
	const baseUrl = bru.getEnvVar("baseUrl");
	const headers = { Authorization: `Bearer ${bru.getVar("bearerToken")}` };
	const approver = td.approver.partyUuid;
	const requester = td.requester.partyUuid;

	const stale = (pendingRequests || []).filter(r => r.resource && r.resource.referenceId === td.resource);
	for (const request of stale) {
		await axios.put(
			`${baseUrl}${paths.v2}/received/reject?party=${approver}&id=${request.id}`,
			null,
			{ headers, validateStatus: () => true });
	}

	await axios.delete(
		`${baseUrl}${paths.connections}/resources?party=${approver}&from=${approver}&to=${requester}&resource=${td.resource}`,
		{ headers, validateStatus: () => true });
}

// The right keys in a delegation check response that the user can delegate, without duplicates.
function delegableKeys(check) {
	const rights = (check && check.rights) || [];
	return [...new Set(rights.filter(r => r.result === true).map(r => r.right.key))];
}

// Fails a step early with a clear message when an earlier step did not set a variable.
function requireVar(name, step) {
	if (!bru.getVar(name)) {
		throw new Error(`${name} is not set: run the scenario from step 01, and check that step ${step} succeeded.`);
	}
}

module.exports = { skipOutsideEnabledEnvironments, testdata, loginAs, clearScenarioVars, resetPair, delegableKeys, requireVar, paths };
