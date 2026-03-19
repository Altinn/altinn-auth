namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Instance Dto
/// </summary>
public class InstanceDto
{
    /// <summary>
    /// Instance RefId
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public InstanceTypeDto Type { get; set; }
}
