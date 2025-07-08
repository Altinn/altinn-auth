const testBaseUrl = "https://platform.at22.altinn.cloud/authorization/";
const yt01BaseUrl = "https://platform.yt01.altinn.cloud/authorization/";
const testAuthenticationBaseUrl = "https://platform.yt01.altinn.cloud/authentication/"
const yt01AuthenticationBaseUrl = "https://platform.yt01.altinn.cloud/authentication/";
const testAmBaseUrl = "https://platform.at22.altinn.cloud/accessmanagement/";
const yt01AmBaseUrl = "https://platform.yt01.altinn.cloud/accessmanagement/";

const authorizeUrl = "api/v1/authorize";
const readSystemsUrl = "api/v1/systemregister"
const systemUsersUrl = "api/v1/systemuser/vendor/bysystem/"
const amDelegationUrl = "api/v1/internal/systemuserclientdelegation"
const amAuthorizedPartiesUrl = "api/v1/resourceowner/authorizedparties";

export const urls = {
    v1: {
        authorizeUrl: {
            test: testBaseUrl + authorizeUrl,
            yt01: yt01BaseUrl + authorizeUrl
        },
        readSystemsUrl: {
            test: testAuthenticationBaseUrl + readSystemsUrl,
            yt01: yt01AuthenticationBaseUrl + readSystemsUrl
        },
        systemUsersUrl: {
            test: testAuthenticationBaseUrl + systemUsersUrl,
            yt01: yt01AuthenticationBaseUrl + systemUsersUrl
        },
        amDelegationUrl: {
            test: testAmBaseUrl + amDelegationUrl,
            yt01: yt01AmBaseUrl + amDelegationUrl
        },
        authorizedPartiesUrl: {
            test: testAmBaseUrl + amAuthorizedPartiesUrl,
            yt01: yt01AmBaseUrl + amAuthorizedPartiesUrl
        }
    }
};

if (!urls[__ENV.API_VERSION]) {
    throw new Error(`Invalid API version: ${__ENV.API_VERSION}. Please ensure it's set correctly in your environment variables.`);
}

if (!urls[__ENV.API_VERSION]["authorizeUrl"][__ENV.API_ENVIRONMENT]) {
    throw new Error(`Invalid API environment: ${__ENV.API_ENVIRONMENT}. Please ensure it's set correctly in your environment variables.`);
}

export const postAuthorizeUrl = urls[__ENV.API_VERSION]["authorizeUrl"][__ENV.API_ENVIRONMENT];
export const getSystemsUrl = urls[__ENV.API_VERSION]["readSystemsUrl"][__ENV.API_ENVIRONMENT];
export const getSystemUsersUrl = urls[__ENV.API_VERSION]["systemUsersUrl"][__ENV.API_ENVIRONMENT];
export const getAmDelegationUrl = urls[__ENV.API_VERSION]["amDelegationUrl"][__ENV.API_ENVIRONMENT];
export const getAuthorizedPartiesUrl = urls[__ENV.API_VERSION]["authorizedPartiesUrl"][__ENV.API_ENVIRONMENT];
export const tokenGeneratorEnv = __ENV.API_ENVIRONMENT == "yt01" ? "yt01" : "tt02"; // yt01 is the only environment that has a separate token generator environment
