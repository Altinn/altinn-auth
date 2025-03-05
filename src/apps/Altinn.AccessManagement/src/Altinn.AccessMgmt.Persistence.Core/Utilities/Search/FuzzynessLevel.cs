namespace Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

/// <summary>
/// Defines the level of fuzziness applied in search matching.
/// Determines the tolerance for inexact matches.
/// </summary>
public enum FuzzynessLevel
{
    /// <summary>
    /// High tolerance for typos and partial matches.
    /// Best for short fields like names where small differences should still result in a match.
    /// </summary>
    High,

    /// <summary>
    /// Medium tolerance for typos and partial matches.
    /// Used for moderately sized text fields like descriptions or categories.
    /// </summary>
    Medium,

    /// <summary>
    /// Low tolerance for typos and partial matches.
    /// Best for long text fields where accuracy is more important than flexibility.
    /// </summary>
    Low
}
