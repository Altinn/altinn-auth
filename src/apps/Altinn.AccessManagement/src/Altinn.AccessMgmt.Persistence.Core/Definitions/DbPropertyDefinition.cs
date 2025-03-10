using System.Reflection;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Represents a column definition in a database table
/// </summary>
public class DbPropertyDefinition
{
    /// <summary>
    /// The property that this column definition is based on
    /// </summary>
    public PropertyInfo Property { get; set; }

    /// <summary>
    /// The name of the column
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Is column nullable
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Default value (if any)
    /// </summary>
    public string DefaultValue { get; set; }
}
