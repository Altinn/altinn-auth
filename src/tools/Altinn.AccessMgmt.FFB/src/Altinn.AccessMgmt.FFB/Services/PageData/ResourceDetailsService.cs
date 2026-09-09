using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the resource details page.
/// </summary>
public sealed record ResourceDetailsData(
    Resource Resource,
    IReadOnlyList<PackageResource> PackageResources,
    IReadOnlyList<RoleResource> RoleResources);

/// <summary>
/// Loads data for the resource details page.
/// </summary>
public sealed class ResourceDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the resource does not exist in the environment.
    /// </summary>
    public async Task<ResourceDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var resource = await db.Resources
            .AsNoTracking()
            .Include(r => r.Type)
            .Include(r => r.Provider)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (resource is null)
        {
            return null;
        }

        var packageResources = await db.PackageResources
            .AsNoTracking()
            .Include(pr => pr.Package).ThenInclude(p => p.Area)
            .Include(pr => pr.Package).ThenInclude(p => p.Provider)
            .Where(pr => pr.ResourceId == id)
            .OrderBy(pr => pr.Package.Name)
            .ToListAsync(ct);

        var roleResources = await db.RoleResources
            .AsNoTracking()
            .Include(rr => rr.Role).ThenInclude(r => r.Provider)
            .Where(rr => rr.ResourceId == id)
            .OrderBy(rr => rr.Role.Name)
            .ToListAsync(ct);

        return new ResourceDetailsData(resource, packageResources, roleResources);
    }
}
