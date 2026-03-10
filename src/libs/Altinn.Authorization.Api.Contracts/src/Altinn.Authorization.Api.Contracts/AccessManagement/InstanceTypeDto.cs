namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Define the types of Instances
/// </summary>
public class InstanceTypeDto
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
