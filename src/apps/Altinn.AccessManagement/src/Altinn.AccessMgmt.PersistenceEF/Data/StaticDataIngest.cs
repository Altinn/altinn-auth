using System.Data;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

/// <summary>
/// Ingest static data into the database
/// </summary>
public static partial class StaticDataIngest
{
    private static AuditValues AuditValues { get; set; } = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest, Guid.NewGuid().ToString());

    public static async Task IngestAll(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        /* ProviderType */
        await AutoIngest(
            dbContext,
            ProviderTypeConstants.AllEntities(),
            (entity, seed) =>
            {
                entity.Name = seed.Entity.Name;
            },
            cancellationToken);

        /* Provider*/
        await AutoIngest(
            dbContext,
            ProviderConstants.AllEntities(),
            (entity, seed) =>
            {
                entity.Name = seed.Entity.Name;
            },
            cancellationToken);

        /* EntityType */
        await AutoIngest(
            dbContext,
            EntityTypeConstants.AllEntities(),
            (entity, seed) =>
            {
                entity.Name = seed.Entity.Name;
            },
            cancellationToken);

        /* EntityVariant */
        await AutoIngest(
            dbContext,
            EntityVariantConstants.AllEntities(),
            (entity, seed) =>
            {
                entity.Name = seed.Entity.Name;
            },
            cancellationToken);

        /* AreaGroups */
        await AutoIngest(
            dbContext,
            AreaGroupConstants.AllEntities(),
            (areaGroup, seed) =>
            {
                areaGroup.Name = seed.Entity.Name;
                areaGroup.Description = seed.Entity.Description;
            },
            cancellationToken);

        /* Area */
        await AutoIngest(
            dbContext,
            AreaConstants.AllEntities(),
            (area, seed) =>
            {
                area.Name = seed.Entity.Name;
                area.Description = seed.Entity.Description;
                area.Urn = seed.Entity.Urn;
                area.Name = seed.Entity.Name;
                area.Description = seed.Entity.Description;
                area.IconUrl = seed.Entity.IconUrl;
                area.GroupId = seed.Entity.GroupId;
            },
            cancellationToken);

        /* Packages */
        await AutoIngest(
            dbContext,
            PackageConstants.AllEntities(),
            (package, seed) =>
            {
                package.ProviderId = seed.Entity.ProviderId;
                package.EntityTypeId = seed.Entity.EntityTypeId;
                package.AreaId = seed.Entity.AreaId;
                package.Urn = seed.Entity.Urn;
                package.Name = seed.Entity.Name;
                package.Description = seed.Entity.Description;
                package.IsDelegable = seed.Entity.IsDelegable;
                package.HasResources = seed.Entity.HasResources;
                package.IsAssignable = seed.Entity.IsAssignable;
            },
            cancellationToken);

        /* Role */
        await AutoIngest(
            dbContext,
            RoleConstants.AllEntities(),
            (role, seed) =>
            {
                role.Name = seed.Entity.Name;
                role.Code = seed.Entity.Code;
                role.Description = seed.Entity.Description;
                role.EntityTypeId = seed.Entity.EntityTypeId;
                role.IsAssignable = seed.Entity.IsAssignable;
                role.IsKeyRole = seed.Entity.IsKeyRole;
                role.ProviderId = seed.Entity.ProviderId;
                role.Urn = seed.Entity.Urn;
            },
            cancellationToken);

        await AutoIngest(
            dbContext,
            SystemEntityConstants.AllEntities(),
            (systemEntity, seed) =>
            {
                systemEntity.Name = seed.Entity.Name;
                systemEntity.ParentId = seed.Entity.ParentId;
                systemEntity.RefId = seed.Entity.RefId;
                systemEntity.TypeId = seed.Entity.TypeId;
                systemEntity.VariantId = seed.Entity.VariantId;
            },
            cancellationToken
        );

        await IngestRoleLookup(dbContext, cancellationToken);
        await IngestRoleMap(dbContext, cancellationToken);
        await IngestRolePackage(dbContext, cancellationToken);
        await IngestEntityVariantRole(dbContext, cancellationToken);
    }

    internal static async Task AutoIngest<T>(
        AppDbContext dbContext,
        IEnumerable<ConstantDefinition<T>> seeds,
        Action<T, ConstantDefinition<T>> onUpdate,
        CancellationToken cancellationToken)
        where T : class, IEntityId
    {
        var table = dbContext.Set<T>();
        var ids = seeds.Select(s => s.Id);
        var entities = await table
            .AsTracking()
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, cancellationToken);

        var translations = await dbContext.TranslationEntries
            .AsTracking()
            .Where(e => e.Type == typeof(T).Name)
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            if (entities.TryGetValue(seed.Id, out var entity))
            {
                onUpdate(entity, seed);
            }
            else
            {
                table.Add(seed);
            }

            foreach (var translation in seed?.EN?.SingleEntries() ?? [])
            {
                if (!TryUpdateTranslation(translations, translation))
                {
                    await dbContext.TranslationEntries.AddAsync(translation, cancellationToken);
                }
            }

            foreach (var translation in seed?.NN?.SingleEntries() ?? [])
            {
                if (!TryUpdateTranslation(translations, translation))
                {
                    await dbContext.TranslationEntries.AddAsync(translation, cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(AuditValues, cancellationToken);

        static bool TryUpdateTranslation(List<TranslationEntry> translations, TranslationEntry translation)
        {
            foreach (var t in translations.Where(t => t.Id == translation.Id))
            {
                if (t.FieldName == translation.FieldName && t.LanguageCode == translation.LanguageCode)
                {
                    t.Value = translation.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
