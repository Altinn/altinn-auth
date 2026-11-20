#nullable enable

using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.ScopePrefixes;

/// <summary>
/// An immutable snapshot of the prefixes each organization owns.
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
    /// The number of organizations that own at least one prefix.
    /// </summary>
    public int OrganizationCount => _prefixesByOrganization.Count;

    /// <summary>
    /// Builds a lookup from the records returned by the prefix API. Records without a prefix or
    /// owner are left out. Inactive prefixes are kept: a service owner whose prefix has been
    /// deactivated must still be able to update resources that already use it.
    /// </summary>
    public static ScopePrefixLookup Create(IEnumerable<MaskinportenScopePrefix> records)
    {
        var prefixesByOrganization = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Prefix) && !string.IsNullOrWhiteSpace(r.OwnerOrgNo))
            .GroupBy(r => r.OwnerOrgNo!, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Prefix!).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return new(prefixesByOrganization);
    }

    /// <summary>
    /// Returns the prefixes owned by an organization, or an empty list if it owns none.
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
