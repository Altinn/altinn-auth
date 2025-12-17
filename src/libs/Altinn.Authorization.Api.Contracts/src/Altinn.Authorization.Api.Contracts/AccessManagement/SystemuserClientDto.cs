namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Model representing a connected client party, meaning a party which has been authorized for one or more accesses, either directly or through role(s), access packages, resources or resource instances.
/// Model can be used both to represent a connection received from another party or a connection provided to another party.
/// </summary>
public class SystemuserClientDto
{
    /// <summary>
    /// Gets or sets the party
    /// </summary>
    public ClientParty Party { get; set; }

    /// <summary>
    /// Gets or sets a collection of all access information for the client 
    /// </summary>
    public List<ClientRoleAccessPackages> Access { get; set; } = [];

    /// <summary>
    /// Composite Key instances
    /// </summary>
    public class ClientParty
    {
        /// <summary>
        /// Gets or sets the universally unique identifier of the party
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the party
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the organization number if the party is an organization
        /// </summary>
        public string OrganizationNumber { get; set; }

        /// <summary>
        /// Gets or sets the unit type if the party is an organization
        /// </summary>
        public string UnitType { get; set; }
    }

    /// <summary>
    /// Composite Key instances
    /// </summary>
    public class ClientRoleAccessPackages
    {
        /// <summary>
        /// Role
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Packages
        /// </summary>
        public string[] Packages { get; set; }
    }
}
