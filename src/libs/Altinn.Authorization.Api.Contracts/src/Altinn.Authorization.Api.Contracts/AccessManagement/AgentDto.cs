namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Model representing a connected client party, meaning a party which has been authorized for one or more accesses, either directly or through role(s), access packages, resources or resource instances.
/// Model can be used both to represent a connection received from another party or a connection provided to another party.
/// </summary>
public class AgentDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    public CompactEntityDto Agent { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information for the client 
    /// </summary>
    public List<AgentRoleAccessPackages> Access { get; set; } = [];

    /// <summary>
    /// Composite Key instances
    /// </summary>
    public class AgentRoleAccessPackages
    {
        /// <summary>
        /// Roles
        /// </summary>
        public CompactRoleDto Role { get; set; }

        /// <summary>
        /// Packages
        /// </summary>
        public CompactPackageDto[] Packages { get; set; }
    }
}
