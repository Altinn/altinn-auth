// Fixture for the test/Policies suite. The app ttd/authz-bruno-testapp1 has a
// stored XACML policy in this environment (the body:text policy in
// shared/MetadataPolicy/UploadAppMetadataPolicy.bru): one Permit rule for the
// roles priv and dagl on the actions below, and a minimum authentication level
// of 2 for users.
module.exports = {
  app: {
    org: "ttd",
    app: "authz-bruno-testapp1",
    roles: ["priv", "dagl"],
    actions: ["instantiate", "read", "write", "delete", "complete"],
    minimumAuthenticationLevel: 2
  },
  unknownApp: {
    org: "ttd",
    app: "authz-bruno-no-such-app"
  }
};
