namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// View version of Entity
/// </summary>
public class EntityParty
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Refrence identity
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// EntityType Name
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// EntityVariant Name
    /// </summary>
    public string Variant { get; set; }
}
