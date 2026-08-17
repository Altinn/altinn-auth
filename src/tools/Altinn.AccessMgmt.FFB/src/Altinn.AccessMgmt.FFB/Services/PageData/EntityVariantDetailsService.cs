using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the entity variant details page.
/// </summary>
public sealed record EntityVariantDetailsData(
    EntityVariant Variant,
    IReadOnlyList<EntityVariantRole> VariantRoles);

/// <summary>
/// Loads data for the entity variant details page.
/// </summary>
public sealed class EntityVariantDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the entity variant does not exist in the environment.
    /// </summary>
    public async Task<EntityVariantDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var variant = await db.EntityVariants
            .AsNoTracking()
            .Include(ev => ev.Type)
            .FirstOrDefaultAsync(ev => ev.Id == id, ct);

        if (variant is null)
        {
            return null;
        }

        var variantRoles = await db.EntityVariantRoles
            .AsNoTracking()
            .Include(evr => evr.Role)
            .Where(evr => evr.VariantId == id)
            .OrderBy(evr => evr.Role.Name)
            .ToListAsync(ct);

        return new EntityVariantDetailsData(variant, variantRoles);
    }
}
