namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// The party that made a change, as shown in <see cref="AuditInfoDto"/>.
/// Deliberately has no fields for personal identifiers or dates of birth, so they cannot be exposed.
/// </summary>
public class AuditEntityDto
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
    /// Entity type, such as person, organization or system
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Entity variant
    /// </summary>
    public string Variant { get; set; }

    /// <summary>
    /// Altinn party id
    /// </summary>
    public int? PartyId { get; set; }

    /// <summary>
    /// Altinn user id
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Altinn username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Organization number. Only set for organizations.
    /// </summary>
    public string? OrganizationIdentifier { get; set; }
}
