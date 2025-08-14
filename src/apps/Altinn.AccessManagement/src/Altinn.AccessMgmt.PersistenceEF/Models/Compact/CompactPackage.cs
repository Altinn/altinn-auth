namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Compact Package Model HELLO
/// </summary>
public class CompactPackage
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
