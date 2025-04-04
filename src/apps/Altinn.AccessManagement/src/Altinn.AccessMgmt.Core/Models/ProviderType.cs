namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Define the types of Providers
/// </summary>
public class ProviderType
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
