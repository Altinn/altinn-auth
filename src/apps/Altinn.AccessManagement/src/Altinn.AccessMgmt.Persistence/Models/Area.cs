namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// Area to organize accesspackages stuff
/// </summary>
public class Area
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
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// IconUrl
    /// </summary>
    public string IconUrl { get; set; }

    /// <summary>
    /// GroupId
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Urn
    /// </summary>
    public string Urn { get; set; }
}

/// <summary>
/// Extended Area
/// </summary>
public class ExtArea : Area
{
    /// <summary>
    /// EntityGroup
    /// </summary>
    public AreaGroup Group { get; set; }
}

/// <summary>
/// Extended Area
/// </summary>
public class ExtendedArea : Area
{
    /// <summary>
    /// EntityGroup
    /// </summary>
    public ExtendedAreaGroup Group { get; set; }
}
