namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Extended Resource
/// </summary>
public class ResourceDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Refrence identifier
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public ProviderDto Provider { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public ResourceTypeDto Type { get; set; }

    public class ResourceDtoCheck
    {
        /// <summary>
        /// Resource the delegation check is regarding
        /// </summary>
        public required ResourceDto Resource { get; set; }

        /// <summary>
        /// Resource actions
        /// </summary>
        public List<string> Actions { get; set; }

        /// <summary>
        /// Result of the delegation check.
        /// True if the user is authorized to give others access to the resource on behalf of the specified party, false otherwise.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Reason for the result of the delegation check.
        /// </summary>
        public IEnumerable<Reason> Reasons { get; set; } = [];

        /// <summary>
        /// Resource delegation check response model
        /// </summary>
        public class Reason
        {
            /// <summary>
            /// Description of the reason.
            /// </summary>
            public required string Description { get; set; }

            /// <summary>
            /// Role ID of the role providing access
            /// </summary>
            public Guid? RoleId { get; set; }

            /// <summary>
            /// Role URN of the role providing access
            /// </summary>
            public string? RoleUrn { get; set; }

            /// <summary>
            /// From party ID of the role providing access
            /// </summary>
            public Guid? FromId { get; set; }

            /// <summary>
            /// Name of the party providing access
            /// </summary>
            public string? FromName { get; set; }

            /// <summary>
            /// To party ID of the role providing access
            /// </summary>
            public Guid? ToId { get; set; }

            /// <summary>
            /// Name of the party the role is providing access to
            /// </summary>
            public string? ToName { get; set; }

            /// <summary>
            /// Via party ID of the keyrole party the user has inherited access through
            /// </summary>
            public Guid? ViaId { get; set; }

            /// <summary>
            /// Name of the party the user has inherited access through
            /// </summary>
            public string? ViaName { get; set; }

            /// <summary>
            /// Role ID for the keyrole the user has for the ViaId party
            /// </summary>
            public Guid? ViaRoleId { get; set; }

            /// <summary>
            /// Role URN for the keyrole the user has for the ViaId party
            /// </summary>
            public string? ViaRoleUrn { get; set; }
        }
    }
}
