namespace Altinn.Authorization.Tests.Util
{
    /// <summary>
    /// Named identities and resource identifiers for the access list authorization scenarios decided
    /// through the external <c>authorization/api/v1/authorize</c> endpoint, mirroring the Bruno
    /// collection test/Authorize/AccessList.
    /// </summary>
    /// <remarks>
    /// Access list membership is resolved by <c>ResourceRegistryMock.GetMembershipsForResourceForParty</c>,
    /// which is keyed on the reportee organization number and the resource identifier. Every scenario
    /// below therefore pairs one of the two reportee organizations with <see cref="ResourceId"/>:
    /// <see cref="MemberOrgNumber"/> resolves to a membership, <see cref="NonMemberOrgNumber"/> resolves
    /// to none, and nothing else differs between a Permit case and its Deny guard sibling.
    /// <para>
    /// The subject side is seeded the way the production <c>ContextHandler</c> resolves it, never by
    /// putting the decisive attribute in the request itself:
    /// <see cref="DaglUserId"/> gets its role from <c>Data/Roles/user_20990010/party_{partyId}/roles.json</c>
    /// (the policy Target of <c>Data/Xacml/3.0/ResourceRegistry/ttd-accesslist-resource/policy.xml</c>
    /// references <c>urn:altinn:rolecode</c>, which is what triggers the role lookup), while
    /// <see cref="DelegationUserId"/> and <see cref="SystemUserUuid"/> hold no role at all and reach
    /// Permit only through a delegation registered in <c>AccessManagementWrapperMock</c> whose policy
    /// lives under <c>Data/blobs/input/ttd-accesslist-resource/{offeredByPartyId}/{coveredBy}/delegationpolicy.xml</c>.
    /// </para>
    /// Request/response fixture pairs live under
    /// <c>Data/Xacml/3.0/ResourceRegistry/ResourceRegistry_AccessList{Scenario}_{Outcome}Request.json</c>.
    /// </remarks>
    public static class AccessListTestData
    {
        /// <summary>
        /// Resource registry id of the access list protected resource (AccessListMode Enabled in
        /// Data/Json/ResourceList/ResourceList.json), whose access lists carry no action filter.
        /// </summary>
        public const string ResourceId = "ttd-accesslist-resource";

        /// <summary>
        /// Organization number of the reportee that is a member of an access list for <see cref="ResourceId"/>.
        /// </summary>
        public const string MemberOrgNumber = "910459880";

        /// <summary>
        /// Party id of <see cref="MemberOrgNumber"/> (Data/Register/50005545.json).
        /// </summary>
        public const int MemberPartyId = 50005545;

        /// <summary>
        /// Organization number of the reportee that is a member of no access list for <see cref="ResourceId"/>.
        /// It is a registered organization, so the decision reaches the membership lookup rather than
        /// being refused for being a person or an unknown party.
        /// </summary>
        public const string NonMemberOrgNumber = "810418672";

        /// <summary>
        /// Party id of <see cref="NonMemberOrgNumber"/> (Data/Register/50004222.json).
        /// </summary>
        public const int NonMemberPartyId = 50004222;

        /// <summary>
        /// User id holding role DAGL for both <see cref="MemberPartyId"/> and <see cref="NonMemberPartyId"/>,
        /// so the role decision on <see cref="ResourceId"/> is Permit for either reportee.
        /// </summary>
        public const int DaglUserId = 20990010;

        /// <summary>
        /// User id holding no role for either reportee, but having received a delegation of the read
        /// action on <see cref="ResourceId"/> from both of them.
        /// </summary>
        public const int DelegationUserId = 20990011;

        /// <summary>
        /// User id holding no role for either reportee and having received no delegation from either of them,
        /// so the decision on <see cref="ResourceId"/> never reaches the access list lookup.
        /// </summary>
        public const int UnauthorizedUserId = 20990012;

        /// <summary>
        /// System user having received a delegation of the read action on <see cref="ResourceId"/>
        /// from <see cref="MemberPartyId"/>.
        /// </summary>
        public const string SystemUserUuid = "47caea5b-a80b-4343-b1d3-31eb523a4e28";
    }
}
