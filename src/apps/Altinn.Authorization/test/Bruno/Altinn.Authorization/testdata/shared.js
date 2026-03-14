module.exports = 
{
    "auth_scopes": {
        "authorize": "altinn:authorization/authorize",
        "authorizeAdmin": "altinn:authorization/authorize.admin"
    },
    "auth_apps": {
        "studio": "studio.designer"
    },
    "authTokenType": {
        "personal": "Personal",
        "enterprise": "Enterprise",
        "enterpriseUser": "EnterpriseUser",
        "platformToken": "PlatformToken",
        "platformAccessToken": "PlatformAccessToken",
        "selfRegisteredEmailUser": "SelfRegisteredEmailUser"
    },
    "serviceOwners": {
		"ttd":
		{
			"org": "ttd",
			"orgno": "991825827"
		},
        "digdir":
		{
			"org": "digdir",
			"orgno": "991825827"
		}
	},
    "systemResources": {
        "client_administration": "altinn_client_administration",
        "accessmanagment": "altinn_access_management",
        "instance_delegation": "altinn_instance_delegation",
        "mainadmin": "altinn_access_management_hovedadmin"
    }
};