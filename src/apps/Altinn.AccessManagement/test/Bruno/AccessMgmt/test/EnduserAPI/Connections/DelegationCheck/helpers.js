// Shared pre-request helpers for the delegation check tests, used by
// EnduserAPI/Connections/DelegationCheck and EnduserAPI/Connections/Resources/DelegationCheck.

// Environments whose test data has been verified for these tests. The others either have no
// test data file or a fixture without the rights and roles the tests expect.
const supportedEnvironments = ["at22", "tt02"];

// Skips the current request outside the supported environments. Returns true when skipped,
// so the caller can stop before it loads test data that does not exist there.
function skipOutsideSupportedEnvironments() {
  if (supportedEnvironments.includes(bru.getEnvVar("tokenEnv"))) {
    return false;
  }

  bru.runner.skipRequest();
  return true;
}

module.exports = {
  supportedEnvironments,
  skipOutsideSupportedEnvironments,
};
