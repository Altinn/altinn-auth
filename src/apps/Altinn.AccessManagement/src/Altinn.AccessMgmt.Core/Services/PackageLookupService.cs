using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class PackageLookupService(AppDbContext db) : IPackageLookupService
{
    private const string UrnPrefix = "urn:altinn:accesspackage";

    private IQueryable<Package> Packages => db.Packages.AsNoTracking().Include(p => p.EntityType);

    /// <inheritdoc/>
    public async Task<Package> FindByUrn(string urn, CancellationToken cancellationToken = default)
    {
        var found = await FindByUrns([urn], cancellationToken);
        return found.Values.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, Package>> FindByUrns(IEnumerable<string> urns, CancellationToken cancellationToken = default)
    {
        var values = urns.Where(u => !string.IsNullOrEmpty(u)).Distinct(StringComparer.Ordinal).ToList();
        var result = new Dictionary<string, Package>(StringComparer.Ordinal);
        if (values.Count == 0)
        {
            return result;
        }

        var candidates = values.SelectMany(UrnCandidates).Distinct().ToList();
        var packages = await Packages
            .Where(p => candidates.Contains(p.Urn.ToLower()))
            .ToListAsync(cancellationToken);

        var byUrn = ByUrn(packages);
        foreach (var value in values)
        {
            if (MatchUrn(byUrn, value) is { } package)
            {
                result[value] = package;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Package> Find(string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var candidates = UrnCandidates(value);
        var code = value.ToLowerInvariant();
        var id = Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;

        var packages = await Packages
            .Where(p => candidates.Contains(p.Urn.ToLower()) || p.Code.ToLower() == code || p.Id == id)
            .ToListAsync(cancellationToken);

        return MatchUrn(ByUrn(packages), value)
            ?? packages.FirstOrDefault(p => string.Equals(p.Code, value, StringComparison.OrdinalIgnoreCase))
            ?? packages.FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// The URNs a value can stand for, in the order they are tried: the value itself, then ":code" or "code"
    /// with the prefix added. In lower case, to compare with the URN in lower case.
    /// </summary>
    private static List<string> UrnCandidates(string value)
    {
        var lower = value.ToLowerInvariant();
        return [lower, lower.StartsWith(':') ? UrnPrefix + lower : $"{UrnPrefix}:{lower}"];
    }

    private static Package MatchUrn(Dictionary<string, Package> byUrn, string value) =>
        UrnCandidates(value).Select(urn => byUrn.GetValueOrDefault(urn)).FirstOrDefault(p => p is { });

    private static Dictionary<string, Package> ByUrn(List<Package> packages)
    {
        var byUrn = new Dictionary<string, Package>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages.Where(p => !string.IsNullOrEmpty(p.Urn)))
        {
            byUrn.TryAdd(package.Urn, package);
        }

        return byUrn;
    }
}
