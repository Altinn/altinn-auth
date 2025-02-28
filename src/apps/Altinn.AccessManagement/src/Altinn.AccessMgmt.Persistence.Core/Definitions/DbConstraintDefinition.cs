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
    /// Gets or sets the properties that are part of this constraint along with their types.
    /// </summary>
    public Dictionary<string, Type> Properties { get; set; } = new();

    /// <summary>
    /// Indicates whether this constraint represents the primary key of the model.
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;
}
