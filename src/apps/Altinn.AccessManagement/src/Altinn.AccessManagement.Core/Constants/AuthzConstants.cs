namespace Altinn.AccessManagement.Core.Constants
{
    /// <summary>
    /// Constants related to authorization.
    /// </summary>
    public static class AuthzConstants
    {
        /// <summary>
        /// Policy tag for authorizing Altinn.Platform.Authorization API access from AltinnII Authorization
        /// </summary>
        public const string ALTINNII_AUTHORIZATION = "AltinnIIAuthorizationAccess";

        /// <summary>
        /// Policy tag for authorizing internal Altinn.Platform.Authorization API access
        /// </summary>
        public const string INTERNAL_AUTHORIZATION = "InternalAuthorizationAccess";

        /// <summary>
        /// Policy tag for authorizing Altinn.Platform.Authorization API access
        /// </summary>
        public const string PLATFORM_ACCESS_AUTHORIZATION = "PlatformAccess";

        /// <summary>
        /// Policy used to authorize that platform access token is issued by Altinn Platform
        /// </summary>
        public const string PLATFORM_ACCESSTOKEN_ISSUER_ISPLATFORM = "platform";

        /// <summary>
        /// Policy tag for reading an maskinporten delegation
        /// </summary>
        public const string POLICY_MASKINPORTEN_DELEGATION_READ = "MaskinportenDelegationRead";
        
        /// <summary>
        /// Policy tag for writing an maskinporten delegation
        /// </summary>
        public const string POLICY_MASKINPORTEN_DELEGATION_WRITE = "MaskinportenDelegationWrite";

        /// <summary>
        /// Policy tag for reading access management information
        /// </summary>
        public const string POLICY_ACCESS_MANAGEMENT_READ = "AccessManagementRead";

        /// <summary>
        /// Policy tag for reading enduser access management information for the authorized party
        /// </summary>
        public const string POLICY_ACCESS_MANAGEMENT_ENDUSER_READ_WITH_PASS_TROUGH = "AccessManagementEndUserReadOrAuthorizedParty";

        /// <summary>
        /// Policy tag for reading enduser access management information
        /// </summary>
        public const string POLICY_ACCESS_MANAGEMENT_ENDUSER_READ = "AccessManagementEndUserRead";

        /// <summary>
        /// Policy tag for reading enduser access management information
        /// </summary>
        public const string POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE = "AccessManagementEndUserWrite";

        /// <summary>
        /// Policy tag for writing access management delegations
        /// </summary>
        public const string POLICY_ACCESS_MANAGEMENT_WRITE = "AccessManagementWrite";

        /// <summary>
        /// Policy tag for scope authorization on the proxy API from Altinn II for the maskinporten integration API
        /// </summary>
        public const string POLICY_MASKINPORTEN_DELEGATIONS_PROXY = "MaskinportenDelegationsProxy";

        /// <summary>
        /// Policy tag for scope authorization on the consent API for maskinporten to create consent tokens
        /// </summary>
        public const string POLICY_MASKINPORTEN_CONSENT_READ = "MaskinportenConsentRead";

        /// <summary>
        /// Policy tag for scope authorization on the resource owner API for getting the Authorized Party list for a third party
        /// </summary>
        public const string POLICY_RESOURCEOWNER_AUTHORIZEDPARTIES = "ResourceOwnerAuthorizedParty";

        /// <summary>
        /// Policy tag for scope authorization on the instance delegation API for Apps
        /// </summary>
        public const string POLICY_APPS_INSTANCEDELEGATION = "AppsInstanceDelegation";

        /// <summary>
        /// Policy tag for authorizing client administration API read access
        /// </summary>
        public const string POLICY_CLIENTDELEGATION_READ = "CLIENTDELEGATION_READ";

        /// <summary>
        /// Policy tag for authorizing client administration API write access
        /// </summary>
        public const string POLICY_CLIENTDELEGATION_WRITE = "CLIENTDELEGATION_WRITE";

        /// <summary>
        /// Policy tag for authorizing enterprises for consent requests
        /// </summary>
        public const string POLICY_CONSENTREQUEST_WRITE = "CONSENTREQUEST_WRITE";

        /// <summary>
        /// Policy tag for authorizing enterprises for consent requests
        /// </summary>
        public const string POLICY_CONSENTREQUEST_READ = "CONSENTREQUEST_READ";

        /// <summary>
        /// Portal enduser scope giving access to most of the end user APIs
        /// </summary>
        public const string SCOPE_PORTAL_ENDUSER = "altinn:portal/enduser";

        /// <summary>
        /// MyClients-Administration Read enduser scope giving access to read operations on behalf of Agent having received client-delegations
        /// </summary>
        public const string SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_READ = "altinn:clientdelegations/myclients.read";

        /// <summary>
        /// MyClients-Administration Write enduser scope giving access to write operations on behalf of Agent having received client-delegations
        /// </summary>
        public const string SCOPE_ENDUSER_CLIENTDELEGATION_MYCLIENTS_WRITE = "altinn:clientdelegations/myclients.write";

        /// <summary>
        /// ClientDelegation.Read enduser scope giving access to read operations on client delegations
        /// </summary>
        public const string SCOPE_ENDUSER_CLIENTDELEGATION_READ = "altinn:clientdelegations.read";

        /// <summary>
        /// ClientDelegation.Write enduser scope giving access to write operations on client delegations
        /// </summary>
        public const string SCOPE_ENDUSER_CLIENTDELEGATION_WRITE = "altinn:clientdelegations.write";

        /// <summary>
        /// Scope giving access to getting authorized parties for a given subject.
        /// </summary>
        public const string SCOPE_AUTHORIZEDPARTIES_ENDUSERSYSTEM = "altinn:accessmanagement/authorizedparties";

        /// <summary>
        /// Scope giving access to getting authorized parties for any third party, for which the third party have access to one or more of the resource owners services, apps or resources.
        /// </summary>
        public const string SCOPE_AUTHORIZEDPARTIES_RESOURCEOWNER = "altinn:accessmanagement/authorizedparties.resourceowner";

        /// <summary>
        /// Scope giving access to getting all authorized parties for any third party
        /// </summary>
        public const string SCOPE_AUTHORIZEDPARTIES_ADMIN = "altinn:accessmanagement/authorizedparties.admin";

        /// <summary>
        /// Scope giving access to delegations for Maskinporten schemes owned by authenticated party 
        /// </summary>
        public const string SCOPE_MASKINPORTEN_DELEGATIONS = "altinn:maskinporten/delegations";

        /// <summary>
        /// Scope giving access to delegations for arbitrary Maskinporten schemes
        /// </summary>
        public const string SCOPE_MASKINPORTEN_DELEGATIONS_ADMIN = "altinn:maskinporten/delegations.admin";

        /// <summary>
        /// Scope giving access to creating consentrequests for anyone if you are the resource owner for the resources that is part of the consent request
        /// </summary>
        public const string SCOPE_CONSENTREQUEST_ORG = "altinn:consentrequests.org";

        /// <summary>
        /// Scope giving access to creating consentrequests
        /// </summary>
        public const string SCOPE_CONSENTREQUEST_WRITE = "altinn:consentrequests.write";

        /// <summary>
        /// Scope giving access to creating consentrequests
        /// </summary>
        public const string SCOPE_CONSENTREQUEST_READ = "altinn:consentrequests.read";

        /// <summary>
        /// Claim for scopes from maskinporten token
        /// </summary>
        public const string CLAIM_MASKINPORTEN_SCOPE = "scope";

        /// <summary>
        /// Claim for full consumer from maskinporten token
        /// </summary>
        public const string CLAIM_MASKINPORTEN_CONSUMER = "consumer";

        /// <summary>
        /// Claim for consumer prefixes from maskinporten token
        /// </summary>
        public const string CLAIM_MASKINPORTEN_CONSUMER_PREFIX = "consumer_prefix";
    }
}
