namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Instance Dto
/// </summary>
public class InstanceDto
{
    /// <summary>
    /// InstanceId
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// InstanceUrn
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public InstanceTypeDto Type { get; set; }
}
