using System.Reflection;

namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

public class DbExtendedPropertyDefinition
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
    /// The extended property that this column definition is based on
    /// </summary>
    public PropertyInfo ExtendedProperty { get; set; }

    /// <summary>
    /// Function to use in query
    /// </summary>
    public string Function { get; set; }
}
