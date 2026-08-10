namespace Altinn.Authorization.Tests.Util
{
    /// <summary>
    /// Named identities and resource identifiers for the system-resource role-decision matrix
    /// (ClientAdministration / EnduserAccessManagement / InstanceDelegation / MainAdmin decided as
    /// roles Dagl / Hadm / Kladm / Admai against the <c>authorization/api/v1/decision</c> endpoint).
    /// </summary>
    /// <remarks>
    /// The production <c>ContextHandler</c> enriches a request's subject with <c>urn:altinn:rolecode</c>
    /// values by looking up <c>Data/Roles/user_{userId}/party_{partyId}/roles.json</c> whenever the
    /// resolved policy's Target references that attribute id - it does this for every ResourceRegistry
    /// decision request, not just the ones ported here. So a new cell of the matrix needs only:
    /// (1) the role listed in the resource's policy.xml Target (see
    /// Data/Xacml/3.0/ResourceRegistry/altinn_client_administration/policy.xml for the shape), and
    /// (2) a roles.json for a user/party pair holding that role - reuse <see cref="ReporteePartyId"/>
    /// and add a sibling user id here rather than inventing a new party.
    /// Request/response fixture pairs live under
    /// Data/Xacml/3.0/ResourceRegistry/ResourceRegistry_SystemResource{Resource}_{Role}_{Outcome}Request.json.
    /// </remarks>
    public static class SystemResourceTestData
    {
        /// <summary>
        /// The reportee party shared by every role holder below. One party per role (rather than one
        /// party per test) keeps the subject the only fact that differs between a Permit and a
        /// NotApplicable case for the same resource.
        /// </summary>
        public const int ReporteePartyId = 50990001;

        /// <summary>
        /// User id seeded with role DAGL for <see cref="ReporteePartyId"/>
        /// (Data/Roles/user_20990001/party_50990001/roles.json).
        /// </summary>
        public const int DaglUserId = 20990001;

        /// <summary>
        /// User id seeded with role ADMAI only for <see cref="ReporteePartyId"/>
        /// (Data/Roles/user_20990002/party_50990001/roles.json). ADMAI is not in the
        /// ClientAdministration/MainAdmin allow-lists, so this id is the NotApplicable counterpart
        /// to <see cref="DaglUserId"/> - the only fact that differs between the two cases.
        /// </summary>
        public const int AdmaiUserId = 20990002;

        /// <summary>
        /// Resource registry id for the ClientAdministration system resource (mirrors Bruno
        /// test/Decision/SystemResource_Tests/testdata/shared.js systemResources.client_administration).
        /// </summary>
        public const string ClientAdministrationResourceId = "altinn_client_administration";

        /// <summary>
        /// Resource registry id for the MainAdmin system resource (mirrors Bruno
        /// test/Decision/SystemResource_Tests/testdata/shared.js systemResources.mainadmin).
        /// </summary>
        public const string MainAdminResourceId = "altinn_access_management_hovedadmin";
    }
}
