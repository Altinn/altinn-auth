namespace Altinn.AccessManagement.Api.Enduser.Models;

/// <summary>
/// CompactPackageDto
/// </summary>
public class CompactPackageDto
{
    /// <summary>
    /// Package Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Package Urn
    /// </summary>
    public string Urn { get; set; }

    /// <summary>
    /// Package AreaId
    /// </summary>
    public Guid AreaId { get; set; }
}
