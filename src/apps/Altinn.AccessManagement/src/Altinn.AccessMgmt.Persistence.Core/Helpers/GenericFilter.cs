namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Generic Filter
/// </summary>
public class GenericFilter
{
    /// <summary>
    /// PropertyName
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// FilterComparer
    /// </summary>
    public FilterComparer Comparer { get; set; }

    /// <summary>
    /// GenericFilter
    /// </summary>
    /// <param name="propertyName">propertyName</param>
    /// <param name="value">value</param>
    /// <param name="comparer">comparer</param>
    public GenericFilter(string propertyName, object value, FilterComparer comparer = FilterComparer.Equals)
    {
        PropertyName = propertyName;
        Value = value;
        Comparer = comparer;
    }
}
