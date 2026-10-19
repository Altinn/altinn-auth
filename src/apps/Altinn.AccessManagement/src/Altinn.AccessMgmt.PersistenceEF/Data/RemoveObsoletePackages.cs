using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

/// <summary>
/// Partial StaticDataIngest
/// </summary>
public static partial class StaticDataIngest
{
    /// <summary>
    /// Packages that have been removed from <see cref="Constants.PackageConstants"/>. The ingest only adds and
    /// updates static data, so a removed package stays in the database until it is deleted here.
    /// </summary>
    public static readonly Guid[] ObsoletePackageIds =
    [
        
        // Konkursbo administrator (urn:altinn:accesspackage:konkursbo-tilgangsstyrer), now modelled as the role
        // RoleConstants.BankruptcyEstateAdministrator.
        Guid.Parse("0195efb8-7c80-7e9c-95c1-48937e23960a"),
    ];

    /// <summary>
    /// Deletes the obsolete packages and every row that references them. Runs on every ingest and does
    /// nothing once the packages are gone.
    /// </summary>
    public static async Task RemoveObsoletePackages(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var packageIds = ObsoletePackageIds;

        var packages = await dbContext.Packages.AsTracking().Where(p => packageIds.Contains(p.Id)).ToListAsync(cancellationToken);
        if (packages.Count == 0)
        {
            return;
        }

        dbContext.AssignmentPackages.RemoveRange(await dbContext.AssignmentPackages.AsTracking().Where(p => packageIds.Contains(p.PackageId)).ToListAsync(cancellationToken));
        dbContext.DelegationPackages.RemoveRange(await dbContext.DelegationPackages.AsTracking().Where(p => packageIds.Contains(p.PackageId)).ToListAsync(cancellationToken));
        dbContext.RolePackages.RemoveRange(await dbContext.RolePackages.AsTracking().Where(p => packageIds.Contains(p.PackageId)).ToListAsync(cancellationToken));
        dbContext.PackageResources.RemoveRange(await dbContext.PackageResources.AsTracking().Where(p => packageIds.Contains(p.PackageId)).ToListAsync(cancellationToken));
        dbContext.RequestAssignmentPackages.RemoveRange(await dbContext.RequestAssignmentPackages.AsTracking().Where(p => packageIds.Contains(p.PackageId)).ToListAsync(cancellationToken));
        dbContext.TranslationEntries.RemoveRange(await dbContext.TranslationEntries.AsTracking().Where(t => t.Type == nameof(Models.Package) && packageIds.Contains(t.Id)).ToListAsync(cancellationToken));
        dbContext.Packages.RemoveRange(packages);

        await dbContext.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
