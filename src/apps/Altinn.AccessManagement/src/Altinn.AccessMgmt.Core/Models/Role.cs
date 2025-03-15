namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Role
/// </summary>
public class Role
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// EntityTypeId
    /// e.g Organization
    /// </summary>
    public Guid EntityTypeId { get; set; }

    /// <summary>
    /// ProviderId
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Name
    /// e.g Dagligleder
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Code
    /// e.g daglig-leder
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Description
    /// e.g The main operator of the organization
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Urn
    /// e.g altinn:external-role:ccr:daglig-leder
    /// altinn:role:tilgangsstyrer
    /// </summary>
    public string Urn { get; set; }
}

/// <summary>
/// Extended Role
/// </summary>
public class ExtRole : Role
{
    /// <summary>
    /// EntityType
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// Provider
    /// </summary>
    public Provider Provider { get; set; }
}
