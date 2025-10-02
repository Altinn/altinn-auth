namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Responsemodel for Create System Delegation
/// </summary>
public class CreateDelegationResponseDto
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
