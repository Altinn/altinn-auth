const testBaseUrl = "https://platform.at22.altinn.cloud/authorization/";
const stagingBaseUrl = "https://platform.tt02.altinn.no/authorization/";
const yt01BaseUrl = "https://platform.yt01.altinn.cloud/authorization/";
const testAuthenticationBaseUrl = "https://platform.at22.altinn.cloud/authentication/"
const stagingAuthenticationBaseUrl = "https://platform.tt02.altinn.no/authentication/";
const yt01AuthenticationBaseUrl = "https://platform.yt01.altinn.cloud/authentication/";
const testAmBaseUrl = "https://platform.at22.altinn.cloud/accessmanagement/";
const stagingAmBaseUrl = "https://platform.tt02.altinn.no/accessmanagement/";
const yt01AmBaseUrl = "https://platform.yt01.altinn.cloud/accessmanagement/";
const stagingAmUiBaseUrl = "https://am.ui.tt02.altinn.no/accessmanagement/"
const yt01AmUiBaseUrl = "https://am.ui.yt01.altinn.cloud/accessmanagement/"

const authorizeUrl = "api/v1/authorize";
const readSystemsUrl = "api/v1/systemregister"
const systemUsersUrl = "api/v1/systemuser/vendor/bysystem/"
const amDelegationUrl = "api/v1/internal/systemuserclientdelegation"
const amAuthorizedPartiesUrl = "api/v1/resourceowner/authorizedparties";
const amConsentUrl = "api/v1/enterprise/consentrequests/"
const amRightholders = "api/v1/user/rightholders";
const amConsentApprove = "api/v1/bff/consentrequests/";

export const env = __ENV.API_ENVIRONMENT ?? 'yt01';

export const urls = {
    v1: {
        authorizeUrl: {
            test: testBaseUrl + authorizeUrl,
            staging: stagingBaseUrl + authorizeUrl,
            yt01: yt01BaseUrl + authorizeUrl
        },
        readSystemsUrl: {
            test: testAuthenticationBaseUrl + readSystemsUrl,
            staging: stagingAuthenticationBaseUrl + readSystemsUrl,
            yt01: yt01AuthenticationBaseUrl + readSystemsUrl
        },
        systemUsersUrl: {
            test: testAuthenticationBaseUrl + systemUsersUrl,
            staging: stagingAuthenticationBaseUrl + systemUsersUrl,
            yt01: yt01AuthenticationBaseUrl + systemUsersUrl
        },
        amDelegationUrl: {
            test: testAmBaseUrl + amDelegationUrl,
            staging: stagingAmBaseUrl + amDelegationUrl,
            yt01: yt01AmBaseUrl + amDelegationUrl
        },
        authorizedPartiesUrl: {
            test: testAmBaseUrl + amAuthorizedPartiesUrl,
            staging: stagingAmBaseUrl + amAuthorizedPartiesUrl,
            yt01: yt01AmBaseUrl + amAuthorizedPartiesUrl
        },
        consentUrl: {
            test: testAmBaseUrl + amConsentUrl,
            staging: stagingAmBaseUrl + amConsentUrl,
            yt01: yt01AmBaseUrl + amConsentUrl
        },
        consentApproveUrl: {
            test: testAmBaseUrl + amConsentApprove,
            staging: stagingAmBaseUrl + amConsentApprove,
            yt01: yt01AmBaseUrl + amConsentApprove
        },
        rightHoldersUrl: {
            test: testAmBaseUrl + amRightholders,
            staging: stagingAmUiBaseUrl + amRightholders,
            yt01: yt01AmUiBaseUrl + amRightholders
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
export const postConsent = urls[__ENV.API_VERSION]["consentUrl"][__ENV.API_ENVIRONMENT];
export const postConsentApprove = urls[__ENV.API_VERSION]["consentApproveUrl"][__ENV.API_ENVIRONMENT];
export const getRightHoldersUrl = urls[__ENV.API_VERSION]["rightHoldersUrl"][__ENV.API_ENVIRONMENT];
export const tokenGeneratorEnv = (() => {
  switch (env) {
    case 'yt01':
      return 'yt01';
    case 'staging':
      return 'tt02';
    case 'test':
      return 'at22';
    default:
      return 'yt01';
  }
})();
