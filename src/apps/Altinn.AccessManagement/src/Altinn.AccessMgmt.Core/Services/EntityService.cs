using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

public class EntityService : IEntityService
{
    public EntityService(AppDbContext appDbContext)
    {
        Db = appDbContext;
    }

    private AuditValues AuditValues { get; set; } = new AuditValues(SystemEntityConstants.InternalApi, SystemEntityConstants.InternalApi, Guid.NewGuid().ToString());

    private AppDbContext Db { get; }

    public async ValueTask<Entity> GetEntity(Guid id, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking()
            .Include(t => t.Type)
            .Include(t => t.Variant)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> CreateEntity(Entity entity, CancellationToken cancellationToken = default)
    {
        Db.Entities.Add(entity);
        var result = await Db.SaveChangesAsync(AuditValues, cancellationToken);
        return result > 0;
    }

    public async ValueTask<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, CancellationToken cancellationToken = default)
    {
        var et = await Db.EntityTypes
            .FirstOrDefaultAsync(t => t.Name == type);
        var ev = await Db.EntityVariants
            .FirstOrDefaultAsync(t => t.Name == variant && t.TypeId == et.Id);

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
    public async Task<Entity> GetByOrgNo(string orgNo, CancellationToken cancellationToken = default) => await
        Db.Entities
            .AsNoTracking()
            .Include(t => t.Type)
            .Include(t => t.Variant)
            .Where(e => e.OrganizationIdentifier == orgNo
                   || Db.EntityLookups.Any(l => l.EntityId == e.Id
                                              && l.Key == "OrganizationIdentifier"
                                              && l.Value == orgNo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Entity> GetByPersNo(string persNo, CancellationToken cancellationToken = default) => await 
        Db.Entities
            .AsNoTracking()
            .Include(t => t.Type)
            .Include(t => t.Variant)
            .Where(e => e.PersonIdentifier == persNo
                   || Db.EntityLookups.Any(l => l.EntityId == e.Id
                                              && l.Key == "PersonIdentifier"
                                              && l.Value == persNo))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<Entity?> GetByPartyId(int partyId, CancellationToken ct = default) => await
       Db.Entities
            .AsNoTracking()
            .Include(t => t.Type)
            .Include(t => t.Variant)
            .Where(e => e.PartyId == partyId
                   || Db.EntityLookups.Any(l => l.EntityId == e.Id
                                              && l.Key == "PartyId"
                                              && l.Value == partyId.ToString()))
            .FirstOrDefaultAsync(ct);
    
    /// <inheritdoc/>
    public async Task<Entity> GetByPartyId(string partyId, CancellationToken cancellationToken = default)
    {
        return await GetByPartyId(int.Parse(partyId), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity?> GetByUserId(int userId, CancellationToken ct = default) => await
        Db.Entities
            .AsNoTracking()
            .Include(t => t.Type)
            .Include(t => t.Variant)
            .Where(e => e.UserId == userId
                   || Db.EntityLookups.Any(l => l.EntityId == e.Id
                                              && l.Key == "UserId"
                                              && l.Value == userId.ToString()))
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity> GetByUserId(string userId, CancellationToken cancellationToken = default)
    {
        return await GetByUserId(int.Parse(userId), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity?> GetByUsername(string username, CancellationToken ct = default) => await
        Db.Entities
            .AsNoTracking()
            .Where(e => e.Username == username)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetChildren(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking().Where(t => t.ParentId == parentId).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetChildren(IEnumerable<Guid> parentIds, CancellationToken cancellationToken = default)
    {
        return await Db.Entities
            .AsNoTracking()
            .Where(t => t.ParentId.HasValue && parentIds.Contains(t.ParentId.Value))
            .Include(t => t.Parent)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Entity> GetParent(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await GetEntity(parentId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetEntities(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Include(t => t.Parent)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetEntitiesByPartyIds(IEnumerable<int> partyIds, CancellationToken cancellationToken = default)
    {
        return await Db.Entities.AsNoTracking()
            .Where(r => r.PartyId.HasValue && partyIds.Contains(r.PartyId.Value))
            .ToListAsync(cancellationToken);
    }
}
