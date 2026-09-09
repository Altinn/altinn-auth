using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the provider details page.
/// </summary>
public sealed record ProviderDetailsData(
    Provider Provider,
    IReadOnlyList<Resource> Resources,
    IReadOnlyList<Package> Packages,
    IReadOnlyList<Role> Roles);

/// <summary>
/// Loads data for the provider details page.
/// </summary>
public sealed class ProviderDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the provider does not exist in the environment.
    /// </summary>
    public async Task<ProviderDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var provider = await db.Providers
            .AsNoTracking()
            .Include(p => p.Type)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (provider is null)
        {
            return null;
        }

        var resources = await db.Resources
            .AsNoTracking()
            .Include(r => r.Type)
            .Where(r => r.ProviderId == id)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var packages = await db.Packages
            .AsNoTracking()
            .Include(p => p.Area)
            .Where(p => p.ProviderId == id)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => r.ProviderId == id)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return new ProviderDetailsData(provider, resources, packages, roles);
    }
}
