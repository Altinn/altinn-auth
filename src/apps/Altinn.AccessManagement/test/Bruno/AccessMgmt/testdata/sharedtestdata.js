module.exports = 
{
    "scopes": {
        "read": "altinn:instances:read",
        "k6Read": "test:am/k6.read"
    },
    "auth_scopes": {
        "portalEnduser": "altinn:portal/enduser",
        "read": "altinn:instances:read",
        "maskinportenDelegations": "altinn:maskinporten/delegations",
        "maskinportenAdmin": "altinn:maskinporten/delegations.admin",
        "authorizedParties": "altinn:accessmanagement/authorizedparties",
        "authorizedPartiesResourceOwner": "altinn:accessmanagement/authorizedparties.resourceowner",
        "authorizedPartiesAdmin": "altinn:accessmanagement/authorizedparties.admin",
        "sluttbrukersystemsopesReadToOthers": "altinn:accmgmt/enduser:connections:to-others.read",
        "sluttbrukersystemsopesReadFromOthers": "altinn:accmgmt/enduser:connections:from-others.read",
        "sluttbrukersystemsopesWriteToOrganizations": "altinn:accmgmt/enduser:connections:to-others.write",
        "sluttbrukersystemsopesWriteFromOrganizations": "altinn:accmgmt/enduser:connections:from-others.write",
    },
    "authTokenType": {
        "personal": "Personal",
        "enterprise": "Enterprise",
        "enterpriseUser": "EnterpriseUser",
        "platformAccess": "PlatformAccess",
        "platformToken": "PlatformToken",
        "systemUser": "SystemUser",
        "selfRegisteredEmailUser": "SelfRegisteredEmailUser"
    },
    "serviceOwners": {
        "ttd": {
            "org": "ttd",
            "orgno": "991825827"
        },
        "digdir": {
            "org": "digdir",
            "orgno": "991825827"
        }
    },
    "expectedDaglAccessPackageCount": 126
};
