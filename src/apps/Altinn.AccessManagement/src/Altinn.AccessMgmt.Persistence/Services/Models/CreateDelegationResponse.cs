namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Responsemodel for Create System Delegation
/// </summary>
public class CreateDelegationResponse
{
    /// <summary>
    /// Delegationidentifier
    /// </summary>
    public Guid DelegationId { get; set; }

    /// <summary>
    /// Client party identifier
    /// </summary>
    public Guid FromEntityId { get; set; }
}
