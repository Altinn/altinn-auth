namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// The party that made a change, as shown in <see cref="AuditInfoDto"/>.
/// Deliberately holds only what is needed to show who it was: no identifiers such as
/// organization or person numbers, party ids, user ids, usernames or dates of birth.
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
}
