namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Compact versjon of resource
/// </summary>
public class CompactResource
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; }
}
