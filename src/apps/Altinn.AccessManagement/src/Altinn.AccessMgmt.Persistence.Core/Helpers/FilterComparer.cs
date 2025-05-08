namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Specifies the type of comparison to use in a filter.
/// </summary>
public enum FilterComparer
{
    /// <summary>
    /// Check if the property is equal to the value.
    /// </summary>
    Equals,

    /// <summary>
    /// Check if the property starts with the value.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Check if the property ends with the value.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Check if the property contains the value.
    /// </summary>
    Contains,

    /// <summary>
    /// Check if the property is greater than the value.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Check if the property is greater than or equal to the value.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Check if the property is less than the value.
    /// </summary>
    LessThan,

    /// <summary>
    /// Check if the property is less than or equal to the value.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Check if the property is not equal to the value.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Check if the property is like the value.
    /// </summary>
    Like
}
