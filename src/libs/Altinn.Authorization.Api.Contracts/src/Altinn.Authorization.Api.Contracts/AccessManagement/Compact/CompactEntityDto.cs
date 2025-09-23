namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact Entity Model
/// </summary>
public class CompactEntityDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Variant
    /// </summary>
    public string Variant { get; set; }

    /// <summary>
    /// Values from entityLoookup
    /// </summary>
    public Dictionary<string,string> KeyValues { get; set; }

    /// <summary>
    /// Parent
    /// </summary>
    public CompactEntityDto Parent { get; set; }

    /// <summary>
    /// Children
    /// </summary>
    public List<CompactEntityDto> Children { get; set; }
}
