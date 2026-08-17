using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the package details page.
/// </summary>
public sealed record PackageDetailsData(
    Package Package,
    IReadOnlyList<PackageResource> Resources,
    IReadOnlyList<RolePackage> RolePackages);

/// <summary>
/// Loads data for the package details page.
/// </summary>
public sealed class PackageDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the package does not exist in the environment.
    /// </summary>
    public async Task<PackageDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var package = await db.Packages
            .AsNoTracking()
            .Include(p => p.EntityType)
            .Include(p => p.Area)
            .Include(p => p.Provider)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (package is null)
        {
            return null;
        }

        var resources = await db.PackageResources
            .AsNoTracking()
            .Include(pr => pr.Resource).ThenInclude(r => r.Type)
            .Include(pr => pr.Resource).ThenInclude(r => r.Provider)
            .Where(pr => pr.PackageId == id)
            .OrderBy(pr => pr.Resource.Name)
            .ToListAsync(ct);

        var rolePackages = await db.RolePackages
            .AsNoTracking()
            .Include(rp => rp.Role)
            .Include(rp => rp.EntityVariant)
            .Where(rp => rp.PackageId == id)
            .OrderBy(rp => rp.Role.Name)
            .ToListAsync(ct);

        return new PackageDetailsData(package, resources, rolePackages);
    }
}
