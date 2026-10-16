#nullable enable

using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.ScopePrefixes;

/// <summary>
/// An immutable snapshot of the active prefixes each organization owns.
/// </summary>
internal sealed class ScopePrefixLookup
{
    private readonly Dictionary<string, string[]> _prefixesByOrganization;

    private ScopePrefixLookup(Dictionary<string, string[]> prefixesByOrganization)
    {
        _prefixesByOrganization = prefixesByOrganization;
    }

    /// <summary>
    /// A lookup in which no organization owns any prefix.
    /// </summary>
    public static ScopePrefixLookup Empty { get; } = new(new Dictionary<string, string[]>(StringComparer.Ordinal));

    /// <summary>
    /// The number of organizations that own at least one active prefix.
    /// </summary>
    public int OrganizationCount => _prefixesByOrganization.Count;

    /// <summary>
    /// Builds a lookup from the records returned by the prefix API. Inactive records, and
    /// records without a prefix or owner, are left out.
    /// </summary>
    public static ScopePrefixLookup Create(IEnumerable<MaskinportenScopePrefix> records)
    {
        var prefixesByOrganization = records
            .Where(r => r.Active && !string.IsNullOrWhiteSpace(r.Prefix) && !string.IsNullOrWhiteSpace(r.OwnerOrgNo))
            .GroupBy(r => r.OwnerOrgNo!, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Prefix!).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return new(prefixesByOrganization);
    }

    /// <summary>
    /// Returns the active prefixes owned by an organization, or an empty list if it owns none.
    /// </summary>
    public IReadOnlyList<string> GetPrefixes(string? organizationNumber)
    {
        if (organizationNumber is not null && _prefixesByOrganization.TryGetValue(organizationNumber, out var prefixes))
        {
            return prefixes;
        }

        return [];
    }
}
