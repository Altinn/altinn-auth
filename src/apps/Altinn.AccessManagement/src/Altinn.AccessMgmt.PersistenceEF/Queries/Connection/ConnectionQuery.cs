using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// A query based on assignments and delegations
/// </summary>
public class ConnectionQuery(AppDbContext db)
{
    /// <summary>
    /// Returns connections between to entities based on assignments and delegations
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        try
        {
            var baseQuery = BuildBaseQuery(db, filter);
            List<ConnectionQueryExtendedRecord> result;

            if (filter.EnrichEntities || filter.ExcludeDeleted)
            {
                var query = EnrichEntities(filter, baseQuery);
                var data = await query.AsNoTracking().ToListAsync(ct);
                result = data.Select(ToDtoEmpty).ToList();
            }
            else
            {
                var data = await baseQuery.AsNoTracking().ToListAsync(ct);
                result = data.Select(ToDtoEmpty).ToList();
            }

            try
            {
                if (filter.IncludePackages || filter.EnrichPackageResources)
                {
                    var pkgs = await LoadPackagesByKeyAsync(baseQuery, filter, ct);
                    if (filter.EnrichPackageResources)
                    {
                        await EnrichPackageResourcesAsync(pkgs, filter, ct);
                    }

                    result = Attach(result, pkgs, p => p.Id, (dto, list) => dto.Packages = list);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to include packages", ex);
            }

            try
            {
                if (filter.IncludeResource)
                {
                    var res = await LoadResourcesByKeyAsync(baseQuery, filter, ct);
                    result = Attach(result, res, r => r.Id, (dto, list) => dto.Resources = list);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to include resources", ex);
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get connections with filter: {JsonSerializer.Serialize(filter)}", ex);
        }
    }

    /// <summary>
    /// Returns connections between to entities based on assignments and delegations
    /// </summary>
    public string GenerateDebugQuery(ConnectionQueryFilter filter)
    {

        if (filter.EnrichEntities || filter.ExcludeDeleted)
        {
            var baseQuery = BuildBaseQuery(db, filter);
            return EnrichEntities(filter, baseQuery).ToQueryString();
        }
        else
        {
            return BuildBaseQuery(db, filter).ToQueryString();
        }
    }

    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQuery(AppDbContext db, ConnectionQueryFilter filter)
    {
        var fromSet = filter.FromIds?.Count > 0 ? new HashSet<Guid>(filter.FromIds) : null;
        var toSet = filter.ToIds?.Count > 0 ? new HashSet<Guid>(filter.ToIds) : null;
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;

        var queries = new List<IQueryable<ConnectionQueryBaseRecord>>();

        #region Direct
        var direct = 
            from assignment in db.Assignments
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = assignment.Id,
                FromId = assignment.FromId,
                ToId = assignment.ToId,
                RoleId = assignment.RoleId,
                ViaId = null,
                ViaRoleId = null,
                DelegationId = null,
                Reason = ConnectionReason.Assignment,
            };

        direct = direct.AsNoTracking()
            .ToIdContains(toSet)
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);

        queries.Add(direct);

        if (filter.IncludeKeyRole)
        {
            var directKeyRole = 
                from assignment in db.Assignments
                join role in db.Roles on assignment.RoleId equals role.Id
                join keyRoleAssignment in db.Assignments on assignment.FromId equals keyRoleAssignment.ToId
                where role.IsKeyRole
                select new ConnectionQueryBaseRecord()
                {
                    AssignmentId = keyRoleAssignment.Id,
                    FromId = keyRoleAssignment.FromId,
                    ToId = assignment.ToId,
                    ViaId = keyRoleAssignment.ToId,
                    RoleId = assignment.RoleId,
                    ViaRoleId = null,
                    DelegationId = null,
                    Reason = ConnectionReason.KeyRole,
                };

            directKeyRole = directKeyRole.AsNoTracking()
                .ToIdContains(toSet)
                .FromIdContains(fromSet)
                .RoleIdContains(roleSet);

            queries.Add(directKeyRole);
        }
        
        #endregion

        #region Children

        var children =
            from assignment in db.Assignments
            join child in db.Entities on assignment.FromId equals child.ParentId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = assignment.Id,
                FromId = child.Id,
                ToId = assignment.ToId,
                RoleId = assignment.RoleId,
                ViaId = assignment.FromId,
                ViaRoleId = null,
                DelegationId = null,
                Reason = ConnectionReason.Hierarchy,
            };

        children = children
            .ToIdContains(toSet)
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);

        queries.Add(children);

        if (filter.IncludeKeyRole)
        {
            var childrenKeyRole =
                from assignment in db.Assignments
                join role in db.Roles on assignment.RoleId equals role.Id
                join keyRoleAssignment in db.Assignments on assignment.FromId equals keyRoleAssignment.ToId
                where role.IsKeyRole
                join child in db.Entities on assignment.FromId equals child.ParentId
                select new ConnectionQueryBaseRecord()
                {
                    AssignmentId = assignment.Id,
                    FromId = child.Id,
                    ToId = keyRoleAssignment.ToId,
                    RoleId = assignment.RoleId,
                    ViaId = assignment.FromId,
                    ViaRoleId = null,
                    DelegationId = null,
                    Reason = ConnectionReason.KeyRole,
                };

            childrenKeyRole = childrenKeyRole
                .ToIdContains(toSet)
                .FromIdContains(fromSet)
                .RoleIdContains(roleSet);

            queries.Add(childrenKeyRole);
        }

        #endregion

        #region RoleMap

        var roleMaps =
            from assignment in db.Assignments
            join rolemap in db.RoleMaps on assignment.RoleId equals rolemap.HasRoleId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = assignment.Id,
                FromId = assignment.FromId,
                ToId = assignment.ToId,
                RoleId = rolemap.GetRoleId,
                ViaId = null,
                ViaRoleId = null,
                DelegationId = null,
                Reason = ConnectionReason.RoleMap,
            };

        roleMaps = roleMaps
            .ToIdContains(toSet)
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);

        queries.Add(roleMaps);

        if (filter.IncludeKeyRole)
        {
            var roleMapKeyRoles =
                from assignment in db.Assignments
                join role in db.Roles on assignment.RoleId equals role.Id
                where role.IsKeyRole
                join keyRoleAssignment in db.Assignments on assignment.FromId equals keyRoleAssignment.ToId
                join rolemap in db.RoleMaps on assignment.RoleId equals rolemap.HasRoleId
                select new ConnectionQueryBaseRecord()
                {
                    AssignmentId = assignment.Id,
                    FromId = assignment.FromId,
                    ToId = keyRoleAssignment.ToId,
                    RoleId = rolemap.GetRoleId,
                    ViaId = null,
                    ViaRoleId = null,
                    DelegationId = null,
                    Reason = ConnectionReason.KeyRole,
                };

            roleMapKeyRoles = roleMapKeyRoles
            .ToIdContains(toSet)
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);

            queries.Add(roleMapKeyRoles);
        }

        #endregion

        #region Delegation

        if (filter.IncludeDelegation)
        {
            var delegations = 
                from delgation in db.Delegations
                join fromAssignment in db.Assignments on delgation.FromId equals fromAssignment.Id
                join toAssignment in db.Assignments on delgation.ToId equals toAssignment.Id
                select new ConnectionQueryBaseRecord()
                {
                    DelegationId = delgation.Id,
                    FromId = fromAssignment.FromId,
                    ToId = toAssignment.ToId,
                    ViaId = fromAssignment.ToId,
                    ViaRoleId = null,
                    AssignmentId = null,
                    RoleId = Guid.Empty,
                    Reason = ConnectionReason.Delegation,
                };

            delegations = delegations
                .ToIdContains(toSet)
                .FromIdContains(fromSet)
                .RoleIdContains(roleSet);

            queries.Add(delegations);

            if (filter.IncludeKeyRole)
            {
                var delegationKeyRoles =
                    from delgation in db.Delegations
                    join fromAssignment in db.Assignments on delgation.FromId equals fromAssignment.Id
                    join toAssignment in db.Assignments on delgation.ToId equals toAssignment.Id
                    join role in db.Roles on toAssignment.RoleId equals role.Id
                    join keyRoleAssignment in db.Assignments on toAssignment.FromId equals keyRoleAssignment.ToId
                    where role.IsKeyRole
                    select new ConnectionQueryBaseRecord()
                    {
                        DelegationId = delgation.Id,
                        FromId = fromAssignment.FromId,
                        ToId = toAssignment.ToId,
                        ViaId = fromAssignment.ToId,
                        ViaRoleId = null,
                        AssignmentId = null,
                        RoleId = Guid.Empty,
                        Reason = ConnectionReason.KeyRole,
                    };

                delegationKeyRoles = delegationKeyRoles
               .ToIdContains(toSet)
               .FromIdContains(fromSet)
               .RoleIdContains(roleSet);

                queries.Add(delegationKeyRoles);
            }
        }

        #endregion

        if (filter.OnlyUniqueResults)
        {
            return queries.Aggregate((current, next) => current.Union(next));
        }
        else
        {
            return queries.Aggregate((current, next) => current.Concat(next));
        }
    }

    private IQueryable<ConnectionQueryRecord> EnrichEntities(ConnectionQueryFilter filter, IQueryable<ConnectionQueryBaseRecord> allKeys)
    {
        var entities = db.Entities.AsQueryable();

        var query = allKeys
            .Join(entities, c => c.FromId, e => e.Id, (c, f) => new { c, f })
            .Join(entities, x => x.c.ToId, t => t.Id, (x, t) => new { x.c, x.f, t })
            .SelectMany(x => db.Roles.Include(r => r.Provider).ThenInclude(p => p.Type).Where(r => r.Id == x.c.RoleId).DefaultIfEmpty(), (x, r) => new { x.c, x.f, x.t, r })
            .SelectMany(x => db.Entities.Where(v => v.Id == x.c.ViaId).DefaultIfEmpty(), (x, via) => new { x.c, x.f, x.t, x.r, via })
            .SelectMany(x => db.Roles.Where(vr => vr.Id == x.c.ViaRoleId).DefaultIfEmpty(), (x, viaRole) => new { x.c, x.f, x.t, x.r, x.via, viaRole })
            .WhereIf(filter.ExcludeDeleted, x => !x.f.IsDeleted)
            .WhereIf(filter.ExcludeDeleted, x => !x.t.IsDeleted)
            .WhereIf(filter.ExcludeDeleted, x => x.via == null || !x.via.IsDeleted)
            .Select(x => new ConnectionQueryRecord
            {
                FromId = x.c.FromId,
                ToId = x.c.ToId,
                RoleId = x.c.RoleId,
                AssignmentId = x.c.AssignmentId,
                DelegationId = x.c.DelegationId,
                ViaId = x.c.ViaId,
                ViaRoleId = x.c.ViaRoleId,
                From = x.f,
                To = x.t,
                Role = x.r,
                Via = x.via,
                ViaRole = x.viaRole,
                Reason = x.c.Reason,
            });

        return query;
    }

    private async Task<ConnectionIndex<ConnectionQueryPackage>> LoadPackagesByKeyAsync(IQueryable<ConnectionQueryBaseRecord> allKeys, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var packageSet = filter.PackageIds?.Count > 0 ? new HashSet<Guid>(filter.PackageIds) : null;

        var assignmentPackages = allKeys
            .Join(db.AssignmentPackages, c => c.AssignmentId, ap => ap.AssignmentId, (c, ap) => new { c, ap })
            .WhereIf(packageSet is not null, x => packageSet!.Contains(x.ap.PackageId));

        var rolePackages = allKeys
            .Join(db.RolePackages, c => c.RoleId, rp => rp.RoleId, (c, rp) => new { c, rp })
            .WhereIf(packageSet is not null, x => packageSet!.Contains(x.rp.PackageId));

        var delegationPackages = allKeys
            .Join(db.DelegationPackages, c => c.DelegationId, dp => dp.DelegationId, (c, dp) => new { c, dp })
            .WhereIf(packageSet is not null, x => packageSet!.Contains(x.dp.PackageId));

        var flat = filter.OnlyUniqueResults
            ? assignmentPackages
                .Select(x => new { x.c, x.ap.PackageId })
                .Union(rolePackages.Select(x => new { x.c, x.rp.PackageId }))
                .Union(delegationPackages.Select(x => new { x.c, x.dp.PackageId }))
            : assignmentPackages
                .Select(x => new { x.c, x.ap.PackageId })
                .Concat(rolePackages.Select(x => new { x.c, x.rp.PackageId }))
                .Concat(delegationPackages.Select(x => new { x.c, x.dp.PackageId }));

        var rows = await flat
               .Join(db.Packages, x => x.PackageId, p => p.Id, (x, p) => new
               {
                   Key = new ConnectionCompositeKey(x.c.FromId, x.c.ToId, x.c.RoleId, x.c.AssignmentId, x.c.DelegationId, x.c.ViaId, x.c.ViaRoleId),
                   Package = p
               })
               .AsNoTracking()
               .ToListAsync(ct);

        var index = new ConnectionIndex<ConnectionQueryPackage>();

        foreach (var g in rows.GroupBy(x => x.Key))
        {
            var mapped = g.Select(z => new ConnectionQueryPackage
            {
                Id = z.Package.Id,
                Name = z.Package.Name,
                AreaId = z.Package.AreaId,
                Urn = z.Package.Urn
            }).DistinctBy(p => p.Id);

            index.AddRange(g.Key, mapped);
        }

        return index;
    }

    private async Task<ConnectionIndex<ConnectionQueryResource>> LoadResourcesByKeyAsync(IQueryable<ConnectionQueryBaseRecord> allKeys, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var resourceSet = filter.ResourceIds?.Count > 0 ? new HashSet<Guid>(filter.ResourceIds) : null;

        // Assignment → Resource
        var assignmentResources = allKeys
            .Join(db.AssignmentResources, c => c.AssignmentId, ar => ar.AssignmentId, (c, ar) => new { c, ar })
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ar.ResourceId));

        // Role → Resource
        var roleResources = allKeys
            .Join(db.RoleResources, c => c.RoleId, rr => rr.RoleId, (c, rr) => new { c, rr })
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.rr.ResourceId));

        // Delegation → Resource
        var delegationResources = allKeys
            .Join(db.DelegationResources, c => c.DelegationId, dr => dr.DelegationId, (c, dr) => new { c, dr })
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.dr.ResourceId));

        var flat = filter.OnlyUniqueResults
           ? assignmentResources
            .Select(x => new { x.c, x.ar.ResourceId })
            .Union(roleResources.Select(x => new { x.c, x.rr.ResourceId }))
            .Union(delegationResources.Select(x => new { x.c, x.dr.ResourceId }))
           : assignmentResources
            .Select(x => new { x.c, x.ar.ResourceId })
            .Concat(roleResources.Select(x => new { x.c, x.rr.ResourceId }))
            .Concat(delegationResources.Select(x => new { x.c, x.dr.ResourceId }));

        var rows = await flat
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                Key = new ConnectionCompositeKey(x.c.FromId, x.c.ToId, x.c.RoleId, x.c.AssignmentId, x.c.DelegationId, x.c.ViaId, x.c.ViaRoleId),
                Resource = r
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var index = new ConnectionIndex<ConnectionQueryResource>();

        foreach (var g in rows.GroupBy(x => x.Key))
        {
            var mapped = g.Select(z => new ConnectionQueryResource
            {
                Id = z.Resource.Id,
                Name = z.Resource.Name,
                RefId = z.Resource.RefId,
            }).DistinctBy(p => p.Id);

            index.AddRange(g.Key, mapped);
        }

        return index;
    }

    private async Task EnrichPackageResourcesAsync(ConnectionIndex<ConnectionQueryPackage> packageIndex, ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        var packageIds = packageIndex.Pairs
            .SelectMany(kv => kv.Value)
            .Select(p => p.Id)
            .Distinct()
            .ToList();

        if (packageIds.Count == 0)
        {
            return;
        }

        var resourceSet = filter.ResourceIds is { Count: > 0 }
            ? new HashSet<Guid>(filter.ResourceIds!)
            : null;

        var rows = await db.PackageResources
            .Where(pr => packageIds.Contains(pr.PackageId))
            .WhereIf(resourceSet is not null, pr => resourceSet!.Contains(pr.ResourceId))
            .Join(db.Resources, pr => pr.ResourceId, r => r.Id, (pr, r) => new
            {
                pr.PackageId,
                Resource = new ConnectionQueryResource { Id = r.Id, Name = r.Name }
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var resourcesByPackage = rows
            .GroupBy(x => x.PackageId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(z => z.Resource).DistinctBy(r => r.Id).ToList()
            );

        foreach (var kv in packageIndex.Pairs)
        {
            var packages = kv.Value;

            foreach (var pkg in packages.ToList())
            {
                if (resourcesByPackage.TryGetValue(pkg.Id, out var list))
                {
                    pkg.Resources = list.ToList();
                }
                else
                {
                    pkg.Resources = new List<ConnectionQueryResource>(capacity: 0);
                }
            }

            // Fjern tomme pakker hvis IncludePackages er false
            if (!filter.IncludePackages)
            {
                kv.Value.RemoveAll(p => p.Resources.Count == 0);
            }
        }
    }

    private static List<ConnectionQueryExtendedRecord> Attach<T>(IEnumerable<ConnectionQueryExtendedRecord> results, ConnectionIndex<T> index, Func<T, Guid> idSelector, Action<ConnectionQueryExtendedRecord, List<T>> assign)
    {
        foreach (var dto in results)
        {
            var vals = index.Get(dto.CompositeKey()).DistinctBy(idSelector).ToList();
            assign(dto, vals);
        }

        return results is List<ConnectionQueryExtendedRecord> list ? list : results.ToList();
    }

    private static ConnectionQueryExtendedRecord ToDtoEmpty(ConnectionQueryRecord x) => new()
    {
        FromId = x.FromId,
        ToId = x.ToId,
        RoleId = x.RoleId,
        AssignmentId = x.AssignmentId,
        DelegationId = x.DelegationId,
        ViaId = x.ViaId,
        ViaRoleId = x.ViaRoleId,
        From = x.From,
        To = x.To,
        Role = x.Role,
        Via = x.Via,
        ViaRole = x.ViaRole,
        Reason = x.Reason,
    };

    private static ConnectionQueryExtendedRecord ToDtoEmpty(ConnectionQueryBaseRecord x) => new()
    {
        FromId = x.FromId,
        ToId = x.ToId,
        RoleId = x.RoleId,
        AssignmentId = x.AssignmentId,
        DelegationId = x.DelegationId,
        ViaId = x.ViaId,
        ViaRoleId = x.ViaRoleId,
        Reason = x.Reason
    };
}

internal sealed class ConnectionIndex<T>
{
    private readonly Dictionary<ConnectionCompositeKey, List<T>> map = new();

    public void Add(ConnectionCompositeKey key, T item)
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = list = new List<T>(4);
        }

        list.Add(item);
    }

    public void AddRange(ConnectionCompositeKey key, IEnumerable<T> items)
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = list = new List<T>();
        }

        list.AddRange(items);
    }

    public IReadOnlyList<T> Get(ConnectionCompositeKey key) =>
        map.TryGetValue(key, out var list) ? list : Array.Empty<T>();

    public IEnumerable<KeyValuePair<ConnectionCompositeKey, List<T>>> Pairs => map;
}

internal static class ConnectionQueryExtensions
{
    internal static IQueryable<ConnectionQueryBaseRecord> ToIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ToId == id);
        }

        return query.Where(t => ids.Contains(t.ToId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> FromIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.FromId == id);
        }

        return query.Where(t => ids.Contains(t.FromId));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> RoleIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.RoleId == id);
        }

        return query.Where(t => ids.Contains(t.RoleId));
    }

}
