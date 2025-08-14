namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Compact Role Model
/// </summary>
public class CompactRole
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Children
    /// </summary>
    public List<CompactRole> Children { get; set; }
}
