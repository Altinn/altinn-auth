namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    /// <summary>
    /// Defines a resource delegation between two parties, performed by the service owner of the resource
    /// </summary>
    public class ServiceOwnerResourceDelegation
    {
        /// <summary>
        /// The party the resource is delegated from, given as a person identifier or an organization identifier
        /// </summary>
        public required ServiceOwnerConnectionPartyUrn From { get; set; }

        /// <summary>
        /// The party the resource is delegated to, given as a person identifier or an organization identifier
        /// </summary>
        public required ServiceOwnerConnectionPartyUrn To { get; set; }

        /// <summary>
        /// The resource registry identifier of the resource
        /// </summary>
        public required string Resource { get; set; }

        /// <summary>
        /// The right keys to delegate. Required when delegating.
        /// Not used when revoking, as a revoke removes the whole resource delegation.
        /// </summary>
        public RightKeyListDto? RightKeys { get; set; }
    }
}
