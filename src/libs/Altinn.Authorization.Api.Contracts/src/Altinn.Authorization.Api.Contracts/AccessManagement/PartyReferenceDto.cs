namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// A party the caller may see referenced by name, but must not be able to identify further.
/// </summary>
public class PartyReferenceDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string? Name { get; set; }
}
