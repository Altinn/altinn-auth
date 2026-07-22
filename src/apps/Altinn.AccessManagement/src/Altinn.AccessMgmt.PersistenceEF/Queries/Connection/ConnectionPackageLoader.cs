using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Responsible for loading packages and enriching them with resources for connection query results.
/// </summary>
internal class ConnectionPackageLoader(AppDbContext db)
{
    /// <summary>
    /// Loads packages for each connection key based on assignment packages, role packages, and delegation packages.
    /// </summary>
    public async Task<ConnectionIndex<ConnectionQueryPackage>> LoadPackagesByKeyAsync(IEnumerable<ConnectionQueryExtendedRecord> keys, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var packageSet = filter.PackageIds?.Count > 0 ? new HashSet<Guid>(filter.PackageIds) : null;
        var index = new ConnectionIndex<ConnectionQueryPackage>();

        var assignmentPackageKeys = keys.Where(k => k.AssignmentId.HasValue && k.RoleId == RoleConstants.Rightholder).Select(k => k.AssignmentId!.Value).Distinct().ToHashSet();

        SortedList<Guid, List<Guid>> assignmentPackages = [];
        SortedSet<Guid> assignmentPackageIds = [];
        if (assignmentPackageKeys.Count > 0)
        {
            var assignmentPackagesRaw = await db.AssignmentPackages.Where(a => assignmentPackageKeys.Contains(a.AssignmentId))
                .WhereIf(packageSet is not null, p => packageSet!.Contains(p.PackageId))
                .Select(ap => new { ap.PackageId, ap.AssignmentId })
                .ToListAsync(ct);

            foreach (var assignmentPackage in assignmentPackagesRaw)
            {
                assignmentPackageIds.Add(assignmentPackage.PackageId);
                if (assignmentPackages.TryGetValue(assignmentPackage.AssignmentId, out var ids))
                {
                    ids.Add(assignmentPackage.PackageId);
                }
                else
                {
                    assignmentPackages.Add(assignmentPackage.AssignmentId, [assignmentPackage.PackageId]);
                }
            }
        }

        var rolePackageKeys = keys.Where(k => k.AssignmentId.HasValue).Select(k => k.RoleId).Distinct().ToHashSet();
        var rolePackagesRaw = await db.RolePackages.Where(r => r.HasAccess && rolePackageKeys.Contains(r.RoleId))
            .WhereIf(packageSet is not null, p => packageSet!.Contains(p.PackageId))
            .Select(rp => new { rp.PackageId, rp.RoleId, rp.EntityVariantId })
            .ToListAsync(ct);
        SortedList<Guid, List<Guid>> rolePackagesForAll = [];
        SortedList<Guid, Dictionary<Guid, List<Guid>>> rolePackagesForEntity = [];
        SortedSet<Guid> rolePackageIds = [];
        foreach (var rolePackage in rolePackagesRaw)
        {
            rolePackageIds.Add(rolePackage.PackageId);
            if (rolePackage.EntityVariantId == null)
            {
                if (rolePackagesForAll.TryGetValue(rolePackage.RoleId, out var ids))
                {
                    ids.Add(rolePackage.PackageId);
                }
                else
                {
                    rolePackagesForAll.Add(rolePackage.RoleId, [rolePackage.PackageId]);
                }
            }
            else
            {
                if (rolePackagesForEntity.TryGetValue(rolePackage.RoleId, out var variantDict))
                {
                    if (variantDict.TryGetValue((Guid)rolePackage.EntityVariantId, out var ids))
                    {
                        ids.Add(rolePackage.PackageId);
                    }
                    else
                    {
                        variantDict.Add((Guid)rolePackage.EntityVariantId, [rolePackage.PackageId]);
                    }
                }
                else
                {
                    rolePackagesForEntity.Add(rolePackage.RoleId, new() { { (Guid)rolePackage.EntityVariantId, [rolePackage.PackageId] } });
                }
            }
        }

        SortedList<Guid, Guid> entityVariants = [];
        var entityKeys = keys.Where(k => k.AssignmentId.HasValue && rolePackagesForEntity.ContainsKey(k.RoleId)).Select(k => k.FromId).Distinct().ToHashSet();
        if (entityKeys.Count > 0)
        {
            var entityVariantsRaw = await db.Entities.Where(e => entityKeys.Contains(e.Id))
                .Select(e => new { e.Id, e.VariantId })
                .ToListAsync(ct);
            foreach (var entityVariant in entityVariantsRaw)
            {
                entityVariants[entityVariant.Id] = entityVariant.VariantId;
            }
        }

        var delegationIds = keys.Select(k => k.DelegationId).Where(id => id != null).Select(id => id!.Value).Distinct().ToHashSet();
        var delegationPackagesRaw = delegationIds.Count == 0 ? [] :
            await db.DelegationPackages.Where(d => delegationIds.Contains(d.DelegationId))
            .WhereIf(packageSet is not null, p => packageSet!.Contains(p.PackageId))
            .Select(d => new { d.PackageId, d.DelegationId })
            .ToListAsync(ct);
        SortedList<Guid, List<Guid>> delegationPackages = [];
        SortedSet<Guid> delegationPackageIds = [];
        foreach (var delegationPackage in delegationPackagesRaw)
        {
            delegationPackageIds.Add(delegationPackage.PackageId);
            if (delegationPackages.TryGetValue(delegationPackage.DelegationId, out var ids))
            {
                ids.Add(delegationPackage.PackageId);
            }
            else
            {
                delegationPackages.Add(delegationPackage.DelegationId, [delegationPackage.PackageId]);
            }
        }

        var packageIds = assignmentPackageIds
            .Union(rolePackageIds)
            .Union(delegationPackageIds)
            .Distinct()
            .ToList();

        var packagesRaw = await db.Packages.Where(p => packageIds.Contains(p.Id)).Select(p => new { p.Id, p.Name, p.AreaId, p.Urn }).ToListAsync(ct);
        SortedList<Guid, ConnectionQueryPackage> packages = [];
        foreach (var package in packagesRaw)
        {
            packages[package.Id] = new() { Id = package.Id, Name = package.Name, AreaId = package.AreaId, Urn = package.Urn };
        }

        foreach (var key in keys)
        {
            IEnumerable<Guid> keyPackageIds = [];

            if (!key.AssignmentId.HasValue)
            {
                // if connection record is a client delegation, we only need to consider delegationpackages, and not from assignment or role packages.
                var clientDelegationPackages = key.DelegationId.HasValue
                 ? (key.DelegationId != null && delegationPackages.ContainsKey((Guid)key.DelegationId)) ? delegationPackages[(Guid)key.DelegationId] : []
                 : [];

                keyPackageIds = clientDelegationPackages.Distinct();
            }
            else if (key.AssignmentId.HasValue && key.RoleId == RoleConstants.Rightholder.Id)
            {
                // if connection is for rightholder we only need to consider assignment packages and not role packages or client delegations
                var assPackages = key.AssignmentId.HasValue
                     ? assignmentPackages.ContainsKey((Guid)key.AssignmentId) ? assignmentPackages[(Guid)key.AssignmentId] : []
                     : [];

                keyPackageIds = assPackages.Distinct();
            }
            else
            {
                // else we need to consider check for role packages
                List<Guid> rolePackagesForEntityForKey = [];
                if (rolePackagesForEntity.TryGetValue(key.RoleId, out var entityDict)
                    && entityDict.TryGetValue(entityVariants[key.FromId], out var entityIds))
                {
                    rolePackagesForEntityForKey = entityIds;
                }

                keyPackageIds = (rolePackagesForAll.TryGetValue(key.RoleId, out List<Guid> packagesForAll) ? packagesForAll : [])
                        .Union(rolePackagesForEntityForKey).Distinct();
            }

            if (!keyPackageIds.Any())
            {
                continue;
            }

            List<ConnectionQueryPackage> keyPackages = [];
            foreach (var id in keyPackageIds)
            {
                keyPackages.Add(packages[id]);
            }

            index.AddRange(new(key.FromId, key.ToId, key.RoleId, key.AssignmentId, key.DelegationId, key.ViaId, key.ViaRoleId), keyPackages);
        }

        return index;
    }

    /// <summary>
    /// Enriches packages in the index with their associated resources.
    /// </summary>
    public async Task EnrichPackageResourcesAsync(ConnectionIndex<ConnectionQueryPackage> packageIndex, ConnectionQueryFilter filter, CancellationToken ct = default)
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
}
