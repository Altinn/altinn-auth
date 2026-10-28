// Features an environment does not have yet because it runs an older release than AT22.
// List them in the environment file as notYetDeployed: name1,name2 and remove a name once the
// release that brings the feature is deployed there. Tests that need a listed feature skip,
// or expect the older behaviour, in that environment.
const FEATURES = {
  // /accessmanagement/api/v1/serviceowner/*: authorized parties lookup and resource delegation.
  serviceOwnerApi: "serviceowner-api",
  // GET enduser/request/received/count/subunits (#4337).
  requestCountSubunits: "request-count-subunits",
  // konkursbo-tilgangsstyrer modelled as a role instead of an access package (#4418).
  konkursboPackageRemoved: "konkursbo-package-removed",
};

function notYetDeployed(feature) {
  return String(bru.getEnvVar("notYetDeployed") || "")
    .split(",")
    .map(name => name.trim())
    .includes(feature);
}

module.exports = { FEATURES, notYetDeployed };
