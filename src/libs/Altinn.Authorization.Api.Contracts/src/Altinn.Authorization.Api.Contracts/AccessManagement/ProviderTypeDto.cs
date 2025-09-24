namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Define the types of Providers
/// </summary>
public class ProviderTypeDto
{
    /// <summary>
    /// Provider type identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Provider type name
    /// </summary>
    public string Name { get; set; }
}
