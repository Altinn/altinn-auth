using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Responsible for loading resources and instances for connection query results.
/// </summary>
internal class ConnectionResourceLoader(AppDbContext db)
{
    /// <summary>
    /// Returns the subset of keys that are rightholder assignments based on filter settings.
    /// </summary>
    public static List<ConnectionQueryExtendedRecord> GetRightholderAssignments(List<ConnectionQueryExtendedRecord> allKeys, ConnectionQueryFilter filter)
    {
        HashSet<Guid> rightholderRoles = new() { RoleConstants.Rightholder.Id };
        if (filter.IncludeAppControlledInstances)
        {
            rightholderRoles.Add(RoleConstants.AppControlledRightholder.Id);
        }

        return allKeys.Where(a => a.AssignmentId.HasValue && rightholderRoles.Contains(a.RoleId)).ToList();
    }

    /// <summary>
    /// Loads resources for each rightholder assignment key and attaches them to the corresponding records.
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> LoadResourcesByKeyAsync(List<ConnectionQueryExtendedRecord> allKeys, HashSet<Guid> rightholderAssignmentIds, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var resourceSet = filter.ResourceIds?.Count > 0 ? new HashSet<Guid>(filter.ResourceIds) : null;

        var assignmentResources = await db.AssignmentResources
            .Where(ai => rightholderAssignmentIds.Contains(ai.AssignmentId))
            .Select(ai => new { ai.AssignmentId, ai.Id, ai.ResourceId })
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ResourceId))
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                x.AssignmentId,
                r.Id,
                r.Name,
                r.RefId
            })
            .AsNoTracking()
            .ToListAsync(ct);

        SortedList<Guid, List<ConnectionQueryResource>> resourcesByAssignment = [];
        foreach (var ai in assignmentResources)
        {
            if (resourcesByAssignment.TryGetValue(ai.AssignmentId, out var list))
            {
                list.Add(new ConnectionQueryResource()
                {
                    Id = ai.Id,
                    Name = ai.Name,
                    RefId = ai.RefId
                });
            }
            else
            {
                resourcesByAssignment[ai.AssignmentId] =
                [
                    new ConnectionQueryResource()
                    {
                        Id = ai.Id,
                        Name = ai.Name,
                        RefId = ai.RefId
                    }
                ];
            }
        }

        foreach (var key in allKeys)
        {
            if (key.AssignmentId.HasValue && resourcesByAssignment.TryGetValue((Guid)key.AssignmentId!, out var list))
            {
                key.Resources = list;
            }
        }

        return allKeys;
    }

    /// <summary>
    /// Loads instances for each rightholder assignment key and attaches them to the corresponding records.
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> LoadInstancesByKeyAsync(List<ConnectionQueryExtendedRecord> allKeys, HashSet<Guid> rightholderAssignmentIds, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var instanceSet = filter.InstanceIds?.Count > 0 ? new HashSet<string>(filter.InstanceIds) : null;
        var resourceSet = filter.ResourceIds?.Count > 0 ? new HashSet<Guid>(filter.ResourceIds) : null;

        var assignmentInstances = await db.AssignmentInstances
            .Where(ai => rightholderAssignmentIds.Contains(ai.AssignmentId))
            .Select(ai => new { ai.AssignmentId, ai.Id, ai.ResourceId, ai.InstanceId })
            .WhereIf(instanceSet is not null, x => instanceSet!.Contains(x.InstanceId))
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ResourceId))
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                x.AssignmentId,
                x.Id,
                x.ResourceId,
                x.InstanceId,
                ResourceName = r.Name,
                ResourceRefId = r.RefId
            })
            .AsNoTracking()
            .ToListAsync(ct);

        SortedList<Guid, List<ConnectionQueryInstance>> instancesByAssignment = [];
        foreach (var ai in assignmentInstances)
        {
            if (instancesByAssignment.TryGetValue(ai.AssignmentId, out var list))
            {
                list.Add(new ConnectionQueryInstance()
                {
                    Id = ai.Id,
                    ResourceId = ai.ResourceId,
                    InstanceId = ai.InstanceId,
                    ResourceName = ai.ResourceName,
                    ResourceRefId = ai.ResourceRefId
                });
            }
            else
            {
                instancesByAssignment[ai.AssignmentId] =
                [
                    new ConnectionQueryInstance()
                    {
                        Id = ai.Id,
                        ResourceId = ai.ResourceId,
                        InstanceId = ai.InstanceId,
                        ResourceName = ai.ResourceName,
                        ResourceRefId = ai.ResourceRefId
                    }
                ];
            }
        }

        foreach (var key in allKeys.Where(k => k.AssignmentId.HasValue && rightholderAssignmentIds.Contains((Guid)k.AssignmentId) && k.Reason != ConnectionReason.Hierarchy && !k.IsMainUnitAccess))
        {
            if (key.AssignmentId.HasValue && instancesByAssignment.TryGetValue((Guid)key.AssignmentId!, out var list))
            {
                key.Instances = list;
            }
        }

        return allKeys;
    }
}
