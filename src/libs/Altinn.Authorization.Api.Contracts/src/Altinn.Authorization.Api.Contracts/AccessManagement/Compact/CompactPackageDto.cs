namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact Package Model
/// </summary>
public class CompactPackageDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// AreaId
    /// </summary>
    public Guid AreaId { get; set; }
}
