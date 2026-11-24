// Shared helpers for the concurrent request creation scenarios (#3931).
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../../../TestToolsTokenGenerator.js");
const sharedtestdata = require("../../../testdata/sharedtestdata.js");

const paths = {
	v2: "/accessmanagement/api/v2/enduser/request",
};

// How many identical calls a scenario sends at once, like a user clicking the button repeatedly.
const parallelCalls = 5;

// Environments where AccessManagement.Enduser.RequestV2Api is on and the seed data is verified.
const enabledEnvironments = ["at22"];

// Environments where the fix for #3931 is deployed. The scenarios that expect one request
// where today two can be created are skipped everywhere else, so the post-deployment run
// stays green until the fix is out. Add an environment here when the fix reaches it.
const fixedEnvironments = [];

// Skips the current request outside the enabled environments, and, for a scenario that
// waits for the fix, outside the environments where it is deployed. Returns true when skipped,
// so the step can return before it loads seed data. bru.runner.skipRequest() only marks the
// request as skipped: the rest of the pre-request script still runs.
function skipOutsideEnabledEnvironments(options) {
	const env = bru.getEnvVar("tokenEnv");
	const waitsForFix = options && options.waitsForFix;
	if (enabledEnvironments.includes(env) && (!waitsForFix || fixedEnvironments.includes(env))) {
		bru.setVar("cc_skipped", null);
		return false;
	}

	// Read by isSkipped() in the post-response script and the tests, which still run when a
	// single request is sent from the Bruno app instead of the collection runner.
	bru.setVar("cc_skipped", "true");

	// Outside the collection runner, as when a single request is sent from the Bruno app, the
	// request may still go out, without a token, and answer 401. Say why in the console.
	console.log(waitsForFix && enabledEnvironments.includes(env)
		? `Skipped: this scenario waits for the fix for #3931. Add "${env}" to fixedEnvironments in helpers.js to run it.`
		: `Skipped: ConcurrentRequestCreation is not enabled in "${env}".`);
	bru.runner.skipRequest();
	return true;
}

// True when the pre-request script of the current step skipped it. The post-response script and
// the tests return early then, instead of failing on a 401 or on results that were never stored.
function isSkipped() {
	return bru.getVar("cc_skipped") === "true";
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
	const data = require(`../../../testdata/delegationrequest-scenarios/${bru.getEnvVar("tokenEnv")}.js`).concurrentRequestCreation;
	return {
		resource: data.resource,
		packages: data.packages,
		requester: {
			name: data.requester.name,
			partyUuid: data.requester.partyuuid.toLowerCase(),
			innehaver: person(data.requester.innehaver),
		},
		approver: person(data.approver),
	};
}

// Mints a personal token for a persona and stores it as the collection bearer token.
async function loginAs(persona) {
	const token = await tokenGenerator.getToken({
		auth_userId: persona.userId,
		auth_partyId: persona.partyId,
		auth_partyUuid: persona.partyUuid,
		auth_ssn: persona.pid,
		auth_tokenType: sharedtestdata.authTokenType.personal,
		auth_scopes: sharedtestdata.auth_scopes.portalEnduser,
	});
	bru.setVar("bearerToken", token);
}

// Clears the variables the steps pass between them, so a step never acts on a value
// left behind by an earlier run.
function clearScenarioVars() {
	for (const name of ["cc_results", "cc_keyA", "cc_keyB"]) {
		bru.setVar(name, null);
	}
}

// Sends the calls at the same time with the current bearer token, and returns the status and
// body of each, in the order given. Never throws on an error status, so a 409 or a 500 is
// reported by the tests instead of failing the script.
//
// The calls go over connections opened beforehand by as many parallel GETs of warmUpPath.
// Without that, each call first opens a connection of its own, the TLS handshakes spread the
// calls out in time, and they rarely reach the server close enough together to collide.
async function sendAtOnce(axios, calls, warmUpPath) {
	const https = require("https");
	const baseUrl = bru.getEnvVar("baseUrl");
	const headers = { Authorization: `Bearer ${bru.getVar("bearerToken")}` };
	const httpsAgent = new https.Agent({ keepAlive: true, maxSockets: calls.length });

	await Promise.all(calls.map(() =>
		axios.get(`${baseUrl}${warmUpPath}`, { headers, httpsAgent, validateStatus: () => true })));

	const responses = await Promise.all(calls.map(call =>
		axios.post(`${baseUrl}${call.path}`, call.body, { headers, httpsAgent, validateStatus: () => true })));
	httpsAgent.destroy();

	return responses.map(r => ({ status: r.status, body: r.data }));
}

// Stores the results of sendAtOnce for the later steps. Bruno variables are kept as strings.
function storeResults(results) {
	bru.setVar("cc_results", JSON.stringify(results));
}

// Reads the results the WHEN step stored, and fails clearly when it did not run.
function readResults(step) {
	const raw = bru.getVar("cc_results");
	if (!raw) {
		throw new Error(`cc_results is not set: run the scenario from step 01, and check that step ${step} succeeded.`);
	}

	return JSON.parse(raw);
}

// True when a request in a list is for the given package urn or resource id.
function isFor(request, reference) {
	return (request.package && request.package.referenceId === reference)
		|| (request.resource && request.resource.referenceId === reference);
}

// Rejects, with the approver's token, every pending request in the list that is for one of the
// references. Rejects all of them, not only the first: while #3931 is unfixed, a scenario can
// leave several pending requests for the same package or resource behind.
async function rejectPending(axios, td, pendingRequests, references) {
	const baseUrl = bru.getEnvVar("baseUrl");
	const headers = { Authorization: `Bearer ${bru.getVar("bearerToken")}` };

	const stale = (pendingRequests || []).filter(r => references.some(ref => isFor(r, ref)));
	for (const request of stale) {
		await axios.put(
			`${baseUrl}${paths.v2}/received/reject?party=${td.approver.partyUuid}&id=${request.id}`,
			null,
			{ headers, validateStatus: () => true });
	}
}

// The right keys in a delegation check response that the user can delegate, without duplicates.
function delegableKeys(check) {
	const rights = (check && check.rights) || [];
	return [...new Set(rights.filter(r => r.result === true).map(r => r.right.key))];
}

module.exports = {
	paths,
	parallelCalls,
	skipOutsideEnabledEnvironments,
	isSkipped,
	testdata,
	loginAs,
	clearScenarioVars,
	sendAtOnce,
	storeResults,
	readResults,
	isFor,
	rejectPending,
	delegableKeys,
};
