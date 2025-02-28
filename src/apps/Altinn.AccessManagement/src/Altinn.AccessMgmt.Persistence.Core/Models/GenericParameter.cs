namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Generic Parameter
/// </summary>
/// <param name="key">Key</param>
/// <param name="value">Value</param>
public class GenericParameter(string key, object value)
{
    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; set; } = key;

    /// <summary>
    /// Value
    /// </summary>
    public object Value { get; set; } = value;
}
