using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Collection of Assignments and Delegations
/// </summary>
public class ConnectionDto
{
    /// <summary>
    /// Identity, either assignment og delegation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Delegation if connection is delegated
    /// </summary>
    public Delegation Delegation { get; set; }

    /// <summary>
    /// The entity identity the connection is from (origin, client, source etc) 
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// The role To identifies as
    /// </summary>
    public RoleDto Role { get; set; }

    /// <summary>
    /// The entity identity the connection is to (destination, agent, etc)
    /// </summary>
    public Entity To { get; set; }

    /// <summary>
    /// The entity betweeen from and to. When connection is delegated.
    /// </summary>
    public Entity Facilitator { get; set; }

    /// <summary>
    /// The role the facilitator has to the client 
    /// </summary>
    public RoleDto FacilitatorRole { get; set; }
}
