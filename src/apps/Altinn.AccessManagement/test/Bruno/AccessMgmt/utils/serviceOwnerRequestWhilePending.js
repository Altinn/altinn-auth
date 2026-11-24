// Shared helpers for the scenarios where a service owner creates a request again while the
// same request is pending (#5028).
// Used by Resource_DelegationRequest/Resource_serviceOwner_API/SameResourceRequestWhilePending and
// AccPkg_DelegationRequest/AccPkg_Serviceowner_API/SamePackageRequestWhilePending.
// Requires inside a helper module resolve relative to this file, unlike requires in
// .bru scripts, which resolve from the collection root.
const tokenGenerator = require("../TestToolsTokenGenerator.js");
const sharedtestdata = require("../testdata/sharedtestdata.js");

const paths = {
	enduser: "/accessmanagement/api/v1/enduser/request",
	serviceOwner: "/accessmanagement/api/v1/serviceowner/delegationrequests",
};

// Environments where the personas are verified.
const enabledEnvironments = ["at22"];

// Environments where the fix for #5028 is deployed. The scenarios expect the fixed behaviour
// and are skipped everywhere else, so the post-deployment run stays green until the fix is out.
// Add an environment here when the fix reaches it.
const fixedEnvironments = [];

// Skips the current request outside the environments above. Returns true when skipped, so the
// step can return before it loads seed data. Also sets the flag isSkipped() reads, since the
// post-response script and the tests still run when a single request is sent from the Bruno app.
function skipOutsideEnabledEnvironments() {
	const env = bru.getEnvVar("tokenEnv");
	if (enabledEnvironments.includes(env) && fixedEnvironments.includes(env)) {
		bru.setVar("sop_skipped", null);
		return false;
	}

	bru.setVar("sop_skipped", "true");
	console.log(enabledEnvironments.includes(env)
		? `Skipped: this scenario waits for the fix for #5028. Add "${env}" to fixedEnvironments in helpers.js to run it.`
		: `Skipped: the #5028 scenarios are not enabled in "${env}".`);
	bru.runner.skipRequest();
	return true;
}

// True when the pre-request script of the current step skipped it.
function isSkipped() {
	return bru.getVar("sop_skipped") === "true";
}

// Returns the scenario personas for the environment the run targets.
function testdata() {
	const data = require(`../testdata/delegationrequest-scenarios/${bru.getEnvVar("tokenEnv")}.js`).serviceOwnerRequestWhilePending;
	return {
		resource: data.resource,
		package: data.package,
		requester: {
			pid: data.requester.pid,
			userId: data.requester.userid,
			partyId: data.requester.partyid,
			partyUuid: data.requester.partyuuid.toLowerCase(),
		},
		organization: {
			orgNo: data.organization.org_no,
			partyUuid: data.organization.partyuuid.toLowerCase(),
		},
	};
}

// Mints a personal token for the requester and stores it as the collection bearer token.
async function loginAsRequester(td) {
	const token = await tokenGenerator.getToken({
		auth_userId: td.requester.userId,
		auth_partyId: td.requester.partyId,
		auth_partyUuid: td.requester.partyUuid,
		auth_ssn: td.requester.pid,
		auth_tokenType: sharedtestdata.authTokenType.personal,
		auth_scopes: sharedtestdata.auth_scopes.portalEnduser,
	});
	bru.setVar("bearerToken", token);
}

// Mints an enterprise (Maskinporten) token for the ttd service owner and stores it as the
// collection bearer token.
async function loginAsServiceOwner() {
	const token = await tokenGenerator.getToken({
		auth_org: sharedtestdata.serviceOwners.ttd.org,
		auth_orgNo: sharedtestdata.serviceOwners.ttd.orgno,
		auth_tokenType: sharedtestdata.authTokenType.enterprise,
		auth_scopes: sharedtestdata.auth_scopes.serviceownerDelegationRequestWrite,
	});
	bru.setVar("bearerToken", token);
}

// Clears the variables the steps pass between them, so a step never acts on a value
// left behind by an earlier run.
function clearScenarioVars() {
	bru.setVar("sop_requestId", null);
}

// True when a request in a list is for the given resource id or package urn.
function isFor(request, reference) {
	return (request.package && request.package.referenceId === reference)
		|| (request.resource && request.resource.referenceId === reference);
}

// Withdraws, with the requester's token, every open (Draft or Pending) request in the list that
// is for the reference. Withdraws all of them: while #5028 is unfixed, a run leaves a draft
// beside the pending request.
async function withdrawOpen(axios, td, openRequests, reference) {
	const baseUrl = bru.getEnvVar("baseUrl");
	const headers = { Authorization: `Bearer ${bru.getVar("bearerToken")}` };

	for (const request of (openRequests || []).filter(r => isFor(r, reference))) {
		await axios.put(
			`${baseUrl}${paths.enduser}/sent/withdraw?party=${td.requester.partyUuid}&id=${request.id}`,
			null,
			{ headers, validateStatus: () => true });
	}
}

module.exports = {
	paths,
	skipOutsideEnabledEnvironments,
	isSkipped,
	testdata,
	loginAsRequester,
	loginAsServiceOwner,
	clearScenarioVars,
	isFor,
	withdrawOpen,
};
