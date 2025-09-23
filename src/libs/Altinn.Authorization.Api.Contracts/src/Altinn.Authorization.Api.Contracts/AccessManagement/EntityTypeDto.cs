namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Extended EntityType
/// </summary>
public class EntityTypeDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public ProviderDto Provider { get; set; }
}
