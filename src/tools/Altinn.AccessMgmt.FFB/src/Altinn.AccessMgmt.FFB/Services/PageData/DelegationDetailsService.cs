using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the delegation details page.
/// </summary>
public sealed record DelegationDetailsData(
    Delegation Delegation,
    IReadOnlyList<DelegationResource> Resources,
    IReadOnlyList<DelegationPackage> Packages);

/// <summary>
/// Loads data for the delegation details page.
/// </summary>
public sealed class DelegationDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the delegation does not exist in the environment.
    /// </summary>
    public async Task<DelegationDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var delegation = await db.Delegations
            .AsNoTracking()
            .Include(d => d.From).ThenInclude(a => a.Role)
            .Include(d => d.From).ThenInclude(a => a.From)
            .Include(d => d.From).ThenInclude(a => a.To)
            .Include(d => d.To).ThenInclude(a => a.Role)
            .Include(d => d.To).ThenInclude(a => a.From)
            .Include(d => d.To).ThenInclude(a => a.To)
            .Include(d => d.Facilitator)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (delegation is null)
        {
            return null;
        }

        var resources = await db.DelegationResources
            .AsNoTracking()
            .Include(dr => dr.Resource).ThenInclude(r => r.Type)
            .Include(dr => dr.Resource).ThenInclude(r => r.Provider)
            .Where(dr => dr.DelegationId == id)
            .OrderBy(dr => dr.Resource.Name)
            .ToListAsync(ct);

        var packages = await db.DelegationPackages
            .AsNoTracking()
            .Include(dp => dp.Package).ThenInclude(p => p.Area)
            .Include(dp => dp.Package).ThenInclude(p => p.Provider)
            .Include(dp => dp.AssignmentPackage)
            .Where(dp => dp.DelegationId == id)
            .OrderBy(dp => dp.Package.Name)
            .ToListAsync(ct);

        return new DelegationDetailsData(delegation, resources, packages);
    }
}
