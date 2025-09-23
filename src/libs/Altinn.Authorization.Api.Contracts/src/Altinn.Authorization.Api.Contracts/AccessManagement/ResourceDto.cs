namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Extended Resource
/// </summary>
public class ResourceDto
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
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Refrence identifier
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public ProviderDto Provider { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public ResourceTypeDto Type { get; set; }
}
