namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Delegation check model for an instance
/// </summary>
public class InstanceCheckDto
{
    /// <summary>
    /// Resource the delegation check is regarding
    /// </summary>
    public required ResourceDto Resource { get; set; }

    /// <summary>
    /// Instance the delegation check is regarding
    /// </summary>
    public required InstanceDto Instance { get; set; }

    /// <summary>
    /// Rights for which access has been checked on the instance.
    /// </summary>
    public required IEnumerable<RightCheckDto> Rights { get; set; }
}
