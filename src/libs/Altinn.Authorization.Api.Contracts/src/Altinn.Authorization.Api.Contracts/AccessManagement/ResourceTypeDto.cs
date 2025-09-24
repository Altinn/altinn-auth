namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Define the types of Resources
/// </summary>
public class ResourceTypeDto
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
