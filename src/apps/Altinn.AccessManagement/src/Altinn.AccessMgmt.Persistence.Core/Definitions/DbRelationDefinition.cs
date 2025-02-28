using Altinn.AccessMgmt.Persistence.Core.Helpers;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Represents a foreign key definition in a database table
/// </summary>
public class DbRelationDefinition
{
    /// <summary>
    /// The name of the foreign key
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The base type of the foreign key
    /// </summary>
    public Type Base { get; set; }

    /// <summary>
    /// The reference type of the foreign key
    /// </summary>
    public Type Ref { get; set; }

    /// <summary>
    /// The reference type of the foreign key
    /// </summary>
    public Type ExtendedType { get; set; }

    /// <summary>
    /// The base property for the foreign key
    /// </summary>
    public string BaseProperty { get; set; }

    /// <summary>
    /// The reference property for the foreign key
    /// </summary>
    public string RefProperty { get; set; }

    /// <summary>
    /// The extended property for the foreign key
    /// </summary>
    public string ExtendedProperty { get; set; }

    /// <summary>
    /// The filters for the foreign key
    /// </summary>
    public List<GenericFilter> Filters { get; set; }

    /// <summary>
    /// Indicates whether the foreign key results in a list
    /// </summary>
    public bool IsList { get; set; }

    /// <summary>
    /// Indicates whether the foreign key is optional
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Indicates whether the foreign key should cascade delete
    /// </summary>
    public bool UseCascadeDelete { get; set; }
}
