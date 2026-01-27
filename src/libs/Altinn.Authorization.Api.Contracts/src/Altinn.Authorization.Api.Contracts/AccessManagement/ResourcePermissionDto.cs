namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Resource permissions
/// </summary>
public class ResourcePermissionDto
{
    /// <summary>
    /// Resource the permissions are for
    /// </summary>
    public CompactResourceDto Resource { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<PermissionDto> Permissions { get; set; }

    public class Check
    {
        /// <summary>
        /// Package the delegation check is regarding
        /// </summary>
        public required ResourceDto Resource { get; set; }

        /// <summary>
        /// Result of the delegation check.
        /// True if the user is authorized to give others access to the package on behalf of the specified party, false otherwise.
        /// </summary>
        public bool Result { get; set; }

        /// <summary>
        /// Reason for the result of the delegation check.
        /// </summary>
        public IEnumerable<Reason> Reasons { get; set; } = [];

        /// <summary>
        /// Package delegation check response model
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
