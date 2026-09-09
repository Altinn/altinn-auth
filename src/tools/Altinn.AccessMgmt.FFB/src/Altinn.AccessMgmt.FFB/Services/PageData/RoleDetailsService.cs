using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the role details page.
/// </summary>
public sealed record RoleDetailsData(
    Role Role,
    IReadOnlyList<RoleResource> Resources,
    IReadOnlyList<RolePackage> Packages,
    IReadOnlyList<RoleMap> RoleMapsHas,
    IReadOnlyList<RoleMap> RoleMapsGet);

/// <summary>
/// Loads data for the role details page.
/// </summary>
public sealed class RoleDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the role does not exist in the environment.
    /// </summary>
    public async Task<RoleDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var role = await db.Roles
            .AsNoTracking()
            .Include(r => r.EntityType)
            .Include(r => r.Provider)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (role is null)
        {
            return null;
        }

        var resources = await db.RoleResources
            .AsNoTracking()
            .Include(rr => rr.Resource).ThenInclude(r => r.Type)
            .Include(rr => rr.Resource).ThenInclude(r => r.Provider)
            .Where(rr => rr.RoleId == id)
            .OrderBy(rr => rr.Resource.Name)
            .ToListAsync(ct);

        var packages = await db.RolePackages
            .AsNoTracking()
            .Include(rp => rp.Package).ThenInclude(p => p.Area)
            .Include(rp => rp.Package).ThenInclude(p => p.Provider)
            .Include(rp => rp.EntityVariant)
            .Where(rp => rp.RoleId == id)
            .OrderBy(rp => rp.Package.Name)
            .ToListAsync(ct);

        var roleMapsHas = await db.RoleMaps
            .AsNoTracking()
            .Include(rm => rm.GetRole)
            .Where(rm => rm.HasRoleId == id)
            .OrderBy(rm => rm.GetRole.Name)
            .ToListAsync(ct);

        var roleMapsGet = await db.RoleMaps
            .AsNoTracking()
            .Include(rm => rm.HasRole)
            .Where(rm => rm.GetRoleId == id)
            .OrderBy(rm => rm.HasRole.Name)
            .ToListAsync(ct);

        return new RoleDetailsData(role, resources, packages, roleMapsHas, roleMapsGet);
    }
}
