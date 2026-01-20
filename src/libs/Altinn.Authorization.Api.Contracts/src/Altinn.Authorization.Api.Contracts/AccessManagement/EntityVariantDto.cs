namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Extended EntityVariant
/// </summary>
public class EntityVariantDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public TypeDto Type { get; set; }
}

/// <summary>
/// Extended EntityVariant
/// </summary>
public class SubTypeDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public TypeDto Type { get; set; }
}
