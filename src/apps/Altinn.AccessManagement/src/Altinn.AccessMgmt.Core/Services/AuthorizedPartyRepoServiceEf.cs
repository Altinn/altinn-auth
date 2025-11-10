using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class AuthorizedPartyRepoServiceEf(AppDbContext db, ConnectionQuery connectionQuery) : IAuthorizedPartyRepoServiceEf
{
    /// <inheritdoc/>
    public async Task<Entity?> GetEntity(Guid id, CancellationToken ct = default) =>
        await db.Entities
            .AsNoTracking()
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByPartyId(int partyId, CancellationToken ct = default) =>
        await db.Entities
            .AsNoTracking()
            .Where(t => t.PartyId == partyId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByOrganizationId(string organizationId, CancellationToken ct = default) => 
        await db.Entities
            .AsNoTracking()
            .Where(e => e.OrganizationIdentifier == organizationId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByPersonId(string personId, CancellationToken ct = default) => 
        await db.Entities
            .AsNoTracking()
            .Where(e => e.PersonIdentifier == personId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByUserId(int userId, CancellationToken ct = default) =>
        await db.Entities
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<Entity?> GetEntityByUsername(string username, CancellationToken ct = default) =>
        await db.Entities
            .AsNoTracking()
            .Where(e => e.Username == username)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetEntities(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        return await db.Entities
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Include(t => t.Parent)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetEntitiesByPartyIds(IEnumerable<int> partyIds, CancellationToken ct = default)
    {
        return await db.Entities
            .AsNoTracking()
            .Where(r => r.PartyId.HasValue && partyIds.Contains(r.PartyId.Value))
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Entity>> GetSubunits(IEnumerable<Guid> parentIds, CancellationToken ct = default)
    {
        return await db.Entities
            .AsNoTracking()
            .Where(t => t.ParentId.HasValue && parentIds.Contains(t.ParentId.Value))
            .Include(t => t.Parent)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Assignment>> GetKeyRoleAssignments(Guid toId, CancellationToken ct = default)
    {
        return await db.Assignments.AsNoTracking()
            .Where(t => t.ToId == toId)
            .Include(t => t.Role)
            .Where(t => t.Role.IsKeyRole)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagesFromOthers(
        Guid toId,
        CancellationToken ct = default)
    {
        var connections = await connectionQuery.GetConnectionsAsync(
        new ConnectionQueryFilter()
        {
            ToIds = [toId],
            EnrichEntities = true,
            IncludeKeyRole = true,
            IncludeDelegation = true,
            IncludeMainUnitConnections = true,
            IncludeSubConnections = true,
            IncludePackages = true,
            IncludeResource = false,
            EnrichPackageResources = false,
            ExcludeDeleted = false
        },
        ConnectionQueryDirection.FromOthers,
        ct);

        return DtoMapper.ConvertPackages(connections);
    }
}
