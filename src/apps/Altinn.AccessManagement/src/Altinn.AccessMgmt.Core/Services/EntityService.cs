using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

public class EntityService : IEntityService
{
    public EntityService(AppDbContextFactory dbContextFactory)
    {
        Db = dbContextFactory.CreateDbContext();
    }

    private AuditValues AuditValues { get; set; } = new AuditValues(SystemEntityConstants.InternalApi, SystemEntityConstants.InternalApi, Guid.NewGuid().ToString());

    private AppDbContext Db { get; }

    public async ValueTask<Entity> GetEntity(Guid id, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking().Include(t => t.Type).Include(t => t.Variant).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> CreateEntity(Entity entity, CancellationToken cancellationToken = default)
    {
        Db.Entities.Add(entity);
        var result = await Db.SaveChangesAsync(AuditValues, cancellationToken);
        return result > 0;
    }

    public async ValueTask<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, CancellationToken cancellationToken = default)
    {
        var et = await Db.EntityTypes.FirstOrDefaultAsync(t => t.Name == type);
        var ev = await Db.EntityVariants.FirstOrDefaultAsync(t => t.Name == variant && t.TypeId == et.Id);

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
        var entityId = await Db.EntityLookups.AsNoTracking().Where(t => t.Key == "OrganizationIdentifier" && t.Value == orgNo).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default)
    {
        var entityId = await Db.EntityLookups.AsNoTracking().Where(t => t.Key == "PersonIdentifier" && t.Value == persNo).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByPartyId(string partyId, CancellationToken cancellationToken = default)
    {
        var entityId = await Db.EntityLookups.AsNoTracking().Where(t => t.Key == "PartyId" && t.Value == partyId).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByUserId(string userId, CancellationToken cancellationToken = default)
    {
        var entityId = await Db.EntityLookups.AsNoTracking().Where(t => t.Key == "UserId" && t.Value == userId).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetByProfile(string profileId, CancellationToken cancellationToken = default)
    {
        var entityId = await Db.EntityLookups.AsNoTracking().Where(t => t.Key == "ProfileId" && t.Value == profileId).Select(t => t.EntityId).FirstOrDefaultAsync();
        return await GetEntity(entityId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking().Where(t => t.ParentId == parentId).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await GetEntity(parentId, cancellationToken);
    }    
}
