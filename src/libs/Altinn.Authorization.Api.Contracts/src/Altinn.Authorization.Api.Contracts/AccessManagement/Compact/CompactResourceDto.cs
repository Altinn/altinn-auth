namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact versjon of resource
/// </summary>
public class CompactResourceDto
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
