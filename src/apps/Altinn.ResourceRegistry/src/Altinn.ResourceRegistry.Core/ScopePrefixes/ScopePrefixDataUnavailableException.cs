#nullable enable

namespace Altinn.ResourceRegistry.Core.ScopePrefixes;

/// <summary>
/// Thrown when scope prefixes cannot be checked because the prefix list could not be fetched
/// and no cached list is recent enough to use.
/// </summary>
public class ScopePrefixDataUnavailableException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScopePrefixDataUnavailableException"/> class.
    /// </summary>
    /// <param name="innerException">Why the prefix list could not be fetched</param>
    public ScopePrefixDataUnavailableException(Exception innerException)
        : base("The Maskinporten scope prefix list is unavailable", innerException)
    {
    }
}
