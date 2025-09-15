using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

public class EntityService(AppDbContext db) : IEntityService
{
    public AuditValues AuditValues { get; set; } = new AuditValues(AuditDefaults.InternalApi, AuditDefaults.InternalApi, Guid.NewGuid().ToString());

    public async ValueTask<Entity> GetEntity(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Entities.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> CreateEntity(Entity entity, CancellationToken cancellationToken = default)
    {
        db.Entities.Add(entity);
        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
        return result > 0;
    }

    public async ValueTask<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, CancellationToken cancellationToken = default)
    {
        var et = await db.EntityTypes.FirstOrDefaultAsync(t => t.Name == type);
        var ev = await db.EntityVariants.FirstOrDefaultAsync(t => t.Name == variant && t.TypeId == et.Id);
        
        var entity = await GetEntity(id);

        if (entity != null)
        {
            if (!entity.TypeId.Equals(et.Id))
            {
                throw new ArgumentException(string.Format("Entity is not of desired type '{0}'", type), paramName: "Type");
            }

            if (!entity.VariantId.Equals(ev.Id))
            {
                throw new ArgumentException(string.Format("Entity is not of desired variant '{0}'", variant), paramName: "Variant");
            }

            return entity;
        }

        await CreateEntity(
            new Entity()
            {
                Id = id,
                Name = name,
                RefId = refId,
                TypeId = et.Id,
                VariantId = ev.Id
            },
            cancellationToken
            );

        return await GetEntity(id);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByOrgNo(string orgNo, CancellationToken cancellationToken = default)
    {
        var entityId = await db.EntityLookups.AsNoTracking().Where(t => t.Key == "OrgNo" && t.Value == orgNo).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default)
    {
        var entityId = await db.EntityLookups.AsNoTracking().Where(t => t.Key == "PersonIdentifier" && t.Value == persNo).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByProfile(string profileId, CancellationToken cancellationToken = default)
    {
        var entityId = await db.EntityLookups.AsNoTracking().Where(t => t.Key == "ProfileId" && t.Value == profileId).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await db.Entities.AsNoTracking().Where(t => t.ParentId == parentId).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await GetEntity(parentId, cancellationToken);
    }
}
