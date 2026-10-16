#nullable enable

namespace Altinn.ResourceRegistry.Core.ScopePrefixes;

/// <summary>
/// Checks Maskinporten scopes against the prefixes an organization owns, using the prefix list
/// from Samarbeidsportalen.
/// </summary>
public interface IScopePrefixValidator
{
    /// <summary>
    /// Returns the scopes that are not under a prefix owned by <paramref name="organizationNumber"/>.
    /// A scope is covered by a prefix when it starts with the prefix followed by <c>:</c>.
    /// Only active prefixes count, and an organization missing from the list owns no prefixes.
    /// </summary>
    /// <param name="organizationNumber">Organization number of the resource owner (<c>HasCompetentAuthority.Organization</c>)</param>
    /// <param name="scopes">The Maskinporten scopes to check</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The scopes the organization does not own, or an empty list if it owns all of them</returns>
    /// <exception cref="ScopePrefixDataUnavailableException">No usable prefix list is available.</exception>
    ValueTask<IReadOnlyList<string>> GetUnownedScopes(string? organizationNumber, IEnumerable<string> scopes, CancellationToken cancellationToken = default);
}
