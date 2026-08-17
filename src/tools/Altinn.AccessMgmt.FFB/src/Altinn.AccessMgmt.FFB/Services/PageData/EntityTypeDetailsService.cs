using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the entity type details page.
/// </summary>
public sealed record EntityTypeDetailsData(
    EntityType EntityType,
    IReadOnlyList<EntityVariant> Variants,
    IReadOnlyList<Role> Roles,
    IReadOnlyList<EntityVariantRole> VariantRoles);

/// <summary>
/// Loads data for the entity type details page.
/// </summary>
public sealed class EntityTypeDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the entity type does not exist in the environment.
    /// </summary>
    public async Task<EntityTypeDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        var entityType = await db.EntityTypes
            .AsNoTracking()
            .Include(et => et.Provider)
            .FirstOrDefaultAsync(et => et.Id == id, ct);

        if (entityType is null)
        {
            return null;
        }

        var variants = await db.EntityVariants
            .AsNoTracking()
            .Where(ev => ev.TypeId == id)
            .OrderBy(ev => ev.Name)
            .ToListAsync(ct);

        // Roles scoped to this entity type via Role.EntityTypeId
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.Provider)
            .Where(r => r.EntityTypeId == id)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        // Variant-specific roles from EntityVariantRole, for all variants of this type
        var variantIds = variants.Select(v => v.Id).ToList();
        var variantRoles = await db.EntityVariantRoles
            .AsNoTracking()
            .Include(evr => evr.Variant)
            .Include(evr => evr.Role)
            .Where(evr => variantIds.Contains(evr.VariantId))
            .OrderBy(evr => evr.Variant.Name)
            .ThenBy(evr => evr.Role.Name)
            .ToListAsync(ct);

        return new EntityTypeDetailsData(entityType, variants, roles, variantRoles);
    }
}
