using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the assignment details page.
/// </summary>
public sealed record AssignmentDetailsData(
    Assignment Assignment,
    IReadOnlyList<AssignmentResource> Resources,
    IReadOnlyList<AssignmentPackage> Packages,
    IReadOnlyList<AssignmentInstance> Instances,
    IReadOnlyList<RoleResource> AutoResources,
    IReadOnlyList<RolePackage> AutoPackages);

/// <summary>
/// Loads data for the assignment details page.
/// </summary>
public sealed class AssignmentDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the assignment does not exist in the environment.
    /// </summary>
    public async Task<AssignmentDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Role)
            .Include(a => a.From)
            .Include(a => a.To)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (assignment is null)
        {
            return null;
        }

        var resources = await db.AssignmentResources
            .AsNoTracking()
            .Include(ar => ar.Resource).ThenInclude(r => r.Type)
            .Where(ar => ar.AssignmentId == id)
            .OrderBy(ar => ar.Resource.Name)
            .ToListAsync(ct);

        var packages = await db.AssignmentPackages
            .AsNoTracking()
            .Include(ap => ap.Package).ThenInclude(p => p.Area)
            .Include(ap => ap.Package).ThenInclude(p => p.Provider)
            .Where(ap => ap.AssignmentId == id)
            .OrderBy(ap => ap.Package.Name)
            .ToListAsync(ct);

        var instances = await db.AssignmentInstances
            .AsNoTracking()
            .Include(ai => ai.Resource).ThenInclude(r => r.Type)
            .Include(ai => ai.InstanceSourceType)
            .Where(ai => ai.AssignmentId == id)
            .OrderBy(ai => ai.Resource.Name)
            .ToListAsync(ct);

        var autoResources = await db.RoleResources
            .AsNoTracking()
            .Include(rr => rr.Resource).ThenInclude(r => r.Type)
            .Include(rr => rr.Resource).ThenInclude(r => r.Provider)
            .Where(rr => rr.RoleId == assignment.RoleId)
            .OrderBy(rr => rr.Resource.Name)
            .ToListAsync(ct);

        var autoPackages = await db.RolePackages
            .AsNoTracking()
            .Include(rp => rp.Package).ThenInclude(p => p.Area)
            .Include(rp => rp.Package).ThenInclude(p => p.Provider)
            .Include(rp => rp.EntityVariant)
            .Where(rp => rp.RoleId == assignment.RoleId)
            .OrderBy(rp => rp.Package.Name)
            .ToListAsync(ct);

        return new AssignmentDetailsData(assignment, resources, packages, instances, autoResources, autoPackages);
    }
}
