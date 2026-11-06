// AccessManagement.Roles.DisableAltinn2 (#4691) removes every Altinn 2 role (provider sys-altinn2) from
// connections, authorized parties and delegation checks. Set altinn2RolesDisabled: true in the environment
// file where that feature flag is on; every other environment keeps returning Altinn 2 roles.
const ALTINN2_PROVIDER = "sys-altinn2";

function altinn2RolesDisabled() {
  return String(bru.getEnvVar("altinn2RolesDisabled")) === "true";
}

module.exports = { ALTINN2_PROVIDER, altinn2RolesDisabled };
