namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Party entity reference
/// </summary>
public class PartyEntityDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of entity (e.g. organization, person, systemuser)
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// SubType of entity (e.g. for organization: AS, ENK, DA)
    /// </summary>
    public string SubType { get; set; }

    /// <summary>
    /// OrganizationIdentifier
    /// </summary>
    public string? OrganizationIdentifier { get; set; }

    /// <summary>
    /// PersonIdentifier
    /// </summary>
    public string? PersonIdentifier { get; set; }
}
