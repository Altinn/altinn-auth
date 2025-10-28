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
    
    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQuery(AppDbContext db, ConnectionQueryFilter filter)
    {
        var fromSet = filter.FromIds?.Count > 0 ? new HashSet<Guid>(filter.FromIds) : null;
        var toSet = filter.ToIds?.Count > 0 ? new HashSet<Guid>(filter.ToIds) : null;
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;

        var direct = db.Assignments
            .WhereMatchIfSet(toSet, x => x.ToId)
            .WhereMatchIfSet(fromSet, x => x.FromId)
            .WhereMatchIfSet(roleSet, x => x.RoleId)
            .Select(a => new ConnectionQueryBaseRecord
            {
                FromId = a.FromId,
                ToId = a.ToId,
                RoleId = a.RoleId,
                AssignmentId = a.Id,
                DelegationId = null,
                ViaId = null,
                ViaRoleId = null,
            });

        var queries = new List<IQueryable<ConnectionQueryBaseRecord>> { direct };
        
        if (filter.IncludeKeyRole)
        {
            var directKeyRoles = db.Assignments
                .Join(db.Roles, a => a.RoleId, r => r.Id, (a, r) => new { a, r })
                .Where(x => x.r.IsKeyRole)
                .Join(db.Assignments, x => x.a.FromId, a => a.ToId, (x, fromAss) => new ConnectionQueryBaseRecord
                {
                    FromId = fromAss.FromId,
                    ToId = x.a.ToId,
                    RoleId = x.r.Id,
                    AssignmentId = fromAss.Id,
                    DelegationId = null,
                    ViaId = null,
                    ViaRoleId = null
                })
                .WhereMatchIfSet(toSet, x => x.ToId)
                .WhereMatchIfSet(fromSet, x => x.FromId)
                .WhereMatchIfSet(roleSet, x => x.RoleId);

            queries.Add(directKeyRoles);
        }

        var children = db.Assignments
            .Join(db.Entities, a => a.FromId, e => e.ParentId, (a, e) => new ConnectionQueryBaseRecord
            {
                FromId = e.Id,
                ToId = a.ToId,
                RoleId = a.RoleId,
                AssignmentId = a.Id,
                DelegationId = null,
            })
            .WhereMatchIfSet(toSet, x => x.ToId)
            .WhereMatchIfSet(fromSet, x => x.FromId)
            .WhereMatchIfSet(roleSet, x => x.RoleId)
            .Select(result => new ConnectionQueryBaseRecord
            {
                FromId = result.FromId,
                ToId = result.ToId,
                RoleId = result.RoleId,
                AssignmentId = result.AssignmentId,
                DelegationId = result.DelegationId,
                ViaId = null,
                ViaRoleId = null,
            });

        queries.Add(children);

        if (filter.IncludeKeyRole)
        {
            var childrenKeyRoles = db.Assignments
                .Join(db.Entities, a => a.FromId, e => e.ParentId, (a, e) => new { a, e })
                .Join(db.Roles, x => x.a.RoleId, r => r.Id, (x, r) => new { x.a, x.e, r })
                .Where(x => x.r.IsKeyRole)
                .Join(db.Assignments, x => x.e.Id, a => a.ToId, (x, fromAss) => new ConnectionQueryBaseRecord
                {
                    FromId = fromAss.FromId,
                    ToId = x.a.ToId,
                    RoleId = x.r.Id,
                    AssignmentId = fromAss.Id,
                    DelegationId = null,
                    ViaId = null,
                    ViaRoleId = null
                })
                .WhereMatchIfSet(toSet, x => x.ToId)
                .WhereMatchIfSet(fromSet, x => x.FromId)
                .WhereMatchIfSet(roleSet, x => x.RoleId);

            queries.Add(childrenKeyRoles);
        }

        var roleMaps = db.Assignments
            .Join(db.RoleMaps, a => a.RoleId, rm => rm.HasRoleId, (a, rm) => new ConnectionQueryBaseRecord
            {
                FromId = a.FromId,
                ToId = a.ToId,
                RoleId = rm.GetRoleId,   // NB: filtrer på GetRoleId
                AssignmentId = a.Id,
                DelegationId = null,
            })
            .WhereMatchIfSet(toSet, x => x.ToId)
            .WhereMatchIfSet(fromSet, x => x.FromId)
            .WhereMatchIfSet(roleSet, x => x.RoleId)
            .Select(result => new ConnectionQueryBaseRecord
            {
                FromId = result.FromId,
                ToId = result.ToId,
                RoleId = result.RoleId,
                AssignmentId = result.AssignmentId,
                DelegationId = result.DelegationId,
                ViaId = null,
                ViaRoleId = null,
            });

        queries.Add(roleMaps);

        if (filter.IncludeKeyRole)
        {
            var roleMapKeyRoles = db.Assignments
             .Join(db.RoleMaps, a => a.RoleId, rm => rm.HasRoleId, (a, rm) => new { a, rm })
             .Join(db.Roles, x => x.rm.GetRoleId, r => r.Id, (x, r) => new { x.a, r })
             .Where(x => x.r.IsKeyRole)
             .Join(db.Assignments, x => x.a.FromId, a => a.ToId, (x, fromAss) => new ConnectionQueryBaseRecord
             {
                 FromId = fromAss.FromId,
                 ToId = x.a.ToId,
                 RoleId = x.r.Id,
                 AssignmentId = fromAss.Id,
                 DelegationId = null,
                 ViaId = null,
                 ViaRoleId = null
             })
            .WhereMatchIfSet(toSet, x => x.ToId)
            .WhereMatchIfSet(fromSet, x => x.FromId)
            .WhereMatchIfSet(roleSet, x => x.RoleId);

            queries.Add(roleMapKeyRoles);
        }

        if (filter.IncludeDelegation)
        {
            var delegation = db.Delegations
                .Join(db.Assignments, d => d.FromId, a => a.Id, (d, fromAss) => new { d, fromAss })
                .Join(db.Assignments, x => x.d.ToId, a => a.Id, (x, toAss) => new { x.d, x.fromAss, toAss })
                .WhereMatchIfSet(toSet, x => x.toAss.ToId)
                .WhereMatchIfSet(fromSet, x => x.fromAss.FromId)
                //// Filter for Roles ??
                .Select(x => new ConnectionQueryBaseRecord
                {
                    FromId = x.fromAss.FromId,
                    ToId = x.toAss.ToId,
                    RoleId = Guid.Empty, // delegasjoner har ikke direkte rolle
                    AssignmentId = null,
                    DelegationId = x.d.Id,
                    ViaId = x.d.FacilitatorId,
                    ViaRoleId = null
                });

            queries.Add(delegation);

            if (filter.IncludeKeyRole)
            {
                var delegationKeyRoles = db.Delegations
                    .Join(db.Assignments, d => d.FromId, a => a.Id, (d, fromAss) => new { d, fromAss })
                    .Join(db.Assignments, x => x.d.ToId, a => a.Id, (x, toAss) => new { x.d, x.fromAss, toAss })
                    .Join(db.Roles, _ => true, r => r.IsKeyRole, (x, r) => new { x.fromAss, x.toAss, r }) // matcher alle key roles
                    .Join(db.Assignments, x => x.fromAss.FromId, a => a.ToId, (x, fromAss) => new { x, fromAss })
                    .WhereMatchIfSet(toSet, x => x.x.toAss.ToId)
                    .WhereMatchIfSet(fromSet, x => x.fromAss.FromId)
                    .WhereMatchIfSet(roleSet, x => x.x.r.Id)
                    .Select(x => new ConnectionQueryBaseRecord
                    {
                        FromId = x.fromAss.FromId,
                        ToId = x.x.toAss.ToId,
                        RoleId = x.x.r.Id,
                        AssignmentId = x.fromAss.Id,
                        DelegationId = x.x.fromAss.Id,
                        ViaId = null,
                        ViaRoleId = null
                    });

                queries.Add(delegationKeyRoles);
            }
        }

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
            .SelectMany(x => db.Roles.Where(r => r.Id == x.c.RoleId).DefaultIfEmpty(), (x, r) => new { x.c, x.f, x.t, r })
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
                ViaRole = x.viaRole
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
                Name = z.Package.Name
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
                Name = z.Resource.Name
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
        ViaRole = x.ViaRole
    };

    private static ConnectionQueryExtendedRecord ToDtoEmpty(ConnectionQueryBaseRecord x) => new()
    {
        FromId = x.FromId,
        ToId = x.ToId,
        RoleId = x.RoleId,
        AssignmentId = x.AssignmentId,
        DelegationId = x.DelegationId,
        ViaId = x.ViaId,
        ViaRoleId = x.ViaRoleId
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
