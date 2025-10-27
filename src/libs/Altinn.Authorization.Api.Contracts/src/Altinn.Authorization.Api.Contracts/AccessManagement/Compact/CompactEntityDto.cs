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
    public Dictionary<string, string> KeyValues { get; set; }

    /// <summary>
    /// Parent
    /// </summary>
    public CompactEntityDto Parent { get; set; }

    /// <summary>
    /// Children
    /// </summary>
    public List<CompactEntityDto> Children { get; set; }

    /// <summary>
    /// PartyId
    /// </summary>
    public int? PartyId { get; set; }

    /// <summary>
    /// UserId
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// OrganizationIdentifier
    /// </summary>
    public string? OrganizationIdentifier { get; set; }

    /// <summary>
    /// PersonIdentifier
    /// </summary>
    public string? PersonIdentifier { get; set; }

    /// <summary>
    /// DateOfBirth
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// DateOfDeath
    /// </summary>
    public DateOnly? DateOfDeath { get; set; }

    /// <summary>
    /// IsDeleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// DeletedAt
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
