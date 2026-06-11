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
    private static AuditValues AuditValues { get; set; } = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest, Guid.NewGuid().ToString(), DateTimeOffset.UtcNow);

    /// <summary>
    /// Namespace component of the transaction-scoped advisory lock that serializes
    /// <see cref="IngestAll"/> (ASCII "ACMI"). Paired with the current database's
    /// oid so the lock is scoped per database — Postgres advisory locks are
    /// otherwise global to the whole server instance.
    /// </summary>
    private const int IngestAdvisoryLockKey = 0x4143_4D49;

    public static async Task IngestAll(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // AutoIngest is read-existing-ids-then-insert-missing: idempotent when run
        // sequentially, but it races under concurrency — two callers can both
        // observe a row as absent and both INSERT it, violating its primary key
        // (e.g. pk_entity). IngestAll is reachable from several paths against the
        // same database (the EF UseAsyncSeeding hook on every MigrateAsync, the
        // explicit Program.Init call, and the integration-test fixtures), so those
        // paths can overlap.
        //
        // Take a transaction-scoped Postgres advisory lock so concurrent ingests
        // serialize: the second waits for the first to finish, then sees the rows
        // as present and updates instead of inserting. The lock is released when the
        // transaction ends. Advisory locks are global to the Postgres instance, so
        // the key is paired with the current database's oid to scope it per database,
        // letting different test databases on one server still ingest concurrently.
        // The transaction is owned here only when one is not already active, matching
        // how AppDbContext handles its own writes.
        var currentTransaction = dbContext.Database.CurrentTransaction is not null;
        using var transaction = currentTransaction ? null : await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await dbContext.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock({IngestAdvisoryLockKey}, (SELECT oid FROM pg_database WHERE datname = current_database())::int)",
                cancellationToken);

            await IngestStaticData(dbContext, cancellationToken);

            if (transaction is { })
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            if (transaction is { })
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private static async Task IngestStaticData(AppDbContext dbContext, CancellationToken cancellationToken)
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
                entity.Description = seed.Entity.Description;
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
                package.Code = seed.Entity.Code;
                package.Name = seed.Entity.Name;
                package.Description = seed.Entity.Description;
                package.IsDelegable = seed.Entity.IsDelegable;
                package.IsAvailableForServiceOwners = seed.Entity.IsAvailableForServiceOwners;
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
                role.LegacyUrn = seed.Entity.LegacyUrn;
                role.LegacyCode = seed.Entity.LegacyCode;
                role.IsAvailableForServiceOwners = seed.Entity.IsAvailableForServiceOwners;
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

        /* InstanceSourceType */
        await AutoIngest(
            dbContext,
            InstanceSourceTypeConstants.AllEntities(),
            (instanceSourceType, seed) =>
            {
                instanceSourceType.Name = seed.Entity.Name;
            },
            cancellationToken
        );

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
