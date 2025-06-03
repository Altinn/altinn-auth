namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Defines a constraint rule for a model, such as a primary key or unique constraint.
/// </summary>
public class DbConstraintDefinition
{
    /// <summary>
    /// The name of the constraint
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Properties that are part of this constraint along with their types.
    /// </summary>
    public Dictionary<string, Type> Properties { get; set; } = new();

    /// <summary>
    /// Nullable properties that are part of this constraint along with their types.
    /// </summary>
    public Dictionary<string, Type> NullableProperties { get; set; } = new();

    /// <summary>
    /// Properties that are included in the unique index. Resulting in an covering index.
    /// </summary>
    public Dictionary<string, Type> IncludedProperties { get; set; } = new();

    /// <summary>
    /// Indicates whether this constraint represents the primary key of the model.
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;
}
