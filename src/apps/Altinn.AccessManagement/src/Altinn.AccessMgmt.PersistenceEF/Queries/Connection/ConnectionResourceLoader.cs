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

        var assignmentResourceQuery = db.AssignmentResources
            .Where(ai => rightholderAssignmentIds.Contains(ai.AssignmentId));

        // Keep the projection covered by the (assignmentid) INCLUDE (id, resourceid) index unless change info is asked for.
        var assignmentResourceRows = filter.IncludeChangeInfo
            ? assignmentResourceQuery.Select(ai => new AssignmentRow { AssignmentId = ai.AssignmentId, Id = ai.Id, ResourceId = ai.ResourceId, ValidFrom = ai.Audit_ValidFrom, ChangedBy = ai.Audit_ChangedBy })
            : assignmentResourceQuery.Select(ai => new AssignmentRow { AssignmentId = ai.AssignmentId, Id = ai.Id, ResourceId = ai.ResourceId, ValidFrom = null, ChangedBy = null });

        var assignmentResources = await assignmentResourceRows
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ResourceId))
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                x.AssignmentId,
                r.Id,
                r.Name,
                r.RefId,
                x.ValidFrom,
                x.ChangedBy
            })
            .AsNoTracking()
            .ToListAsync(ct);

        SortedList<Guid, List<ConnectionQueryResource>> resourcesByAssignment = [];
        foreach (var ai in assignmentResources)
        {
            var resource = new ConnectionQueryResource()
            {
                Id = ai.Id,
                Name = ai.Name,
                RefId = ai.RefId,
                ChangeInfo = ai.ValidFrom.HasValue ? new ConnectionQueryChangeInfo(ai.ValidFrom.Value, ai.ChangedBy) : null
            };

            if (resourcesByAssignment.TryGetValue(ai.AssignmentId, out var list))
            {
                list.Add(resource);
            }
            else
            {
                resourcesByAssignment[ai.AssignmentId] = [resource];
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

        var assignmentInstanceQuery = db.AssignmentInstances
            .Where(ai => rightholderAssignmentIds.Contains(ai.AssignmentId));

        // Keep the projection covered by the (assignmentid) INCLUDE (id, resourceid, instanceid) index unless change info is asked for.
        var assignmentInstanceRows = filter.IncludeChangeInfo
            ? assignmentInstanceQuery.Select(ai => new AssignmentRow { AssignmentId = ai.AssignmentId, Id = ai.Id, ResourceId = ai.ResourceId, InstanceId = ai.InstanceId, ValidFrom = ai.Audit_ValidFrom, ChangedBy = ai.Audit_ChangedBy })
            : assignmentInstanceQuery.Select(ai => new AssignmentRow { AssignmentId = ai.AssignmentId, Id = ai.Id, ResourceId = ai.ResourceId, InstanceId = ai.InstanceId, ValidFrom = null, ChangedBy = null });

        var assignmentInstances = await assignmentInstanceRows
            .WhereIf(instanceSet is not null, x => instanceSet!.Contains(x.InstanceId))
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ResourceId))
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                x.AssignmentId,
                x.Id,
                x.ResourceId,
                x.InstanceId,
                ResourceName = r.Name,
                ResourceRefId = r.RefId,
                x.ValidFrom,
                x.ChangedBy
            })
            .AsNoTracking()
            .ToListAsync(ct);

        SortedList<Guid, List<ConnectionQueryInstance>> instancesByAssignment = [];
        foreach (var ai in assignmentInstances)
        {
            var instance = new ConnectionQueryInstance()
            {
                Id = ai.Id,
                ResourceId = ai.ResourceId,
                InstanceId = ai.InstanceId,
                ResourceName = ai.ResourceName,
                ResourceRefId = ai.ResourceRefId,
                ChangeInfo = ai.ValidFrom.HasValue ? new ConnectionQueryChangeInfo(ai.ValidFrom.Value, ai.ChangedBy) : null
            };

            if (instancesByAssignment.TryGetValue(ai.AssignmentId, out var list))
            {
                list.Add(instance);
            }
            else
            {
                instancesByAssignment[ai.AssignmentId] = [instance];
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

    /// <summary>
    /// Loads delegation resources for connections with a DelegationId and merges them into the corresponding records.
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> LoadDelegationResourcesByKeyAsync(List<ConnectionQueryExtendedRecord> allKeys, HashSet<Guid> delegationIds, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var resourceSet = filter.ResourceIds?.Count > 0 ? new HashSet<Guid>(filter.ResourceIds) : null;

        var delegationResources = await db.DelegationResources
            .Where(dr => delegationIds.Contains(dr.DelegationId))
            .Select(dr => new { dr.DelegationId, dr.Id, dr.ResourceId })
            .WhereIf(resourceSet is not null, x => resourceSet!.Contains(x.ResourceId))
            .Join(db.Resources, x => x.ResourceId, r => r.Id, (x, r) => new
            {
                x.DelegationId,
                r.Id,
                r.Name,
                r.RefId
            })
            .AsNoTracking()
            .ToListAsync(ct);

        SortedList<Guid, List<ConnectionQueryResource>> resourcesByDelegation = [];
        foreach (var dr in delegationResources)
        {
            if (resourcesByDelegation.TryGetValue(dr.DelegationId, out var list))
            {
                list.Add(new ConnectionQueryResource()
                {
                    Id = dr.Id,
                    Name = dr.Name,
                    RefId = dr.RefId
                });
            }
            else
            {
                resourcesByDelegation[dr.DelegationId] =
                [
                    new ConnectionQueryResource()
                    {
                        Id = dr.Id,
                        Name = dr.Name,
                        RefId = dr.RefId
                    }
                ];
            }
        }

        foreach (var key in allKeys)
        {
            if (key.DelegationId.HasValue && resourcesByDelegation.TryGetValue((Guid)key.DelegationId!, out var list))
            {
                if (key.Resources is null)
                {
                    key.Resources = list;
                }
                else
                {
                    key.Resources.AddRange(list);
                }
            }
        }

        return allKeys;
    }

    private sealed class AssignmentRow
    {
        public Guid AssignmentId { get; init; }

        public Guid Id { get; init; }

        public Guid ResourceId { get; init; }

        public string InstanceId { get; init; }

        public DateTimeOffset? ValidFrom { get; init; }

        public Guid? ChangedBy { get; init; }
    }
}
