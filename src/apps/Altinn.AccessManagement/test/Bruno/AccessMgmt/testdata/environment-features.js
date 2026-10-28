// Features an environment cannot be tested against yet, because it runs an older release than
// AT22, its API gateway has no route for the endpoint, or a feature flag is off there.
// List them in the environment file as notYetDeployed: name1,name2 and remove a name once the
// feature is available there. Tests that need a listed feature skip, or expect the older
// behaviour, in that environment.
const FEATURES = {
  // serviceowner/authorizedparties and serviceowner/connections/resources{,/rights,/revoke}, used by
  // test/ServiceOwnerAPI/Scenarios. The rest of the service owner API, such as
  // serviceowner/connections/accesspackages, is available everywhere. In TT02 the API gateway answers
  // 404 for these paths, the same as for a path that does not exist, while the older
  // resourceowner/authorizedparties route works. Before removing the name, check the gateway route
  // and the feature flag AccessMgmt.Controller.ServiceOwner.ResourceDelegation, which gates the
  // three resource delegation endpoints (FeatureGate in the service owner ConnectionsController).
  serviceOwnerAuthorizedPartiesAndResources: "serviceowner-authorizedparties-and-resources",
  // GET enduser/request/received/count/subunits (#4337). TT02 has the release with it
  // (app/AccessManagement-v2026.10.28), but its API gateway still answers 404 for the path.
  requestCountSubunits: "request-count-subunits",
};

function notYetDeployed(feature) {
  return String(bru.getEnvVar("notYetDeployed") || "")
    .split(",")
    .map(name => name.trim())
    .includes(feature);
}

module.exports = { FEATURES, notYetDeployed };
