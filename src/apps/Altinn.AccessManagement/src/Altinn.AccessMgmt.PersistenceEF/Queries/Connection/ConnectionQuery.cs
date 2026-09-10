using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// The ConnectionQuery class provides methods for querying connections between entities based on assignments, delegations, and other relationships.
/// It supports filtering, enrichment of results with related data, and checking for the existence of connections between two parties.
/// </summary>
public class ConnectionQuery(AppDbContext db)
{
    private readonly ConnectionBaseQueryBuilder _baseQueryBuilder = new();
    
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsFromOthersAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, ct);
    }

    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsToOthersAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.ToOthers, ct);
    }

    /// <summary>
    /// Checks if connection exists between two parties
    /// Returns first result only (Assignment => Delegation => Hierarchy => KeyRole)
    ///  
    /// Note: Incomplete implementation. Missing support for combined reasons (e.g. Delegation + Hierarchy, KeyRole + Hierarchy or Innehaver of Enk through Revi/Regn).
    /// Current use-case is however limited to validating whether to allow the fromId to create an access request to the toId, where these connections might not be valid.
    /// But beware if considering use of this method for other purposes.
    /// </summary>
    /// <param name="fromId">From identifier</param>
    /// <param name="toId">To identifier</param>
    /// <returns>Tuple indicating if connection exists and the reason</returns>
    public async Task<(bool Result, ConnectionReason? Reason)> HasConnection(Guid fromId, Guid toId)
    {
        return await HasConnection(fromId, toId, [ConnectionReason.Assignment, ConnectionReason.Delegation, ConnectionReason.Hierarchy, ConnectionReason.KeyRole]);
    }

    /// <summary>
    /// Checks if connection exists between two parties
    /// Returns first result only (Assignment => Delegation => Hierarchy => KeyRole)
    /// 
    /// Note: Incomplete implementation. Missing support for combined reasons (e.g. Delegation + Hierarchy, KeyRole + Hierarchy or Innehaver of Enk through Revi/Regn).
    /// Current use-case is however limited to validating whether to allow the fromId to create an access request to the toId, where these connections might not be valid.
    /// But beware if considering use of this method for other purposes.
    /// </summary>
    /// <param name="fromId">From identifier</param>
    /// <param name="toId">To identifier</param>
    /// <param name="reasons">Reasons to check</param>
    /// <returns>Tuple indicating if connection exists and the reason</returns>
    public async Task<(bool Result, ConnectionReason? Reason)> HasConnection(Guid fromId, Guid toId, ConnectionReason[] reasons)
    {
        if (reasons == null || reasons.Length == 0)
        {
            return (false, null);
        }

        if (reasons.Contains(ConnectionReason.Assignment))
        {
            var assignments = db.Assignments.AsNoTracking()
                .Where(t => t.FromId == fromId && t.ToId == toId);

            if (await assignments.AnyAsync())
            {
                return (true, ConnectionReason.Assignment);
            }
        }

        if (reasons.Contains(ConnectionReason.Delegation))
        {
            var delegations = db.Delegations.AsNoTracking()
                .Where(t => t.From.FromId == fromId && t.To.ToId == toId);

            if (await delegations.AnyAsync())
            {
                return (true, ConnectionReason.Delegation);
            }
        }

        if (reasons.Contains(ConnectionReason.Hierarchy))
        {
            var hierarchy =
            from a in db.Assignments.AsNoTracking()
            join e in db.Entities.AsNoTracking() on a.FromId equals e.ParentId
            where a.ToId == toId && e.Id == fromId
            select 1;

            if (await hierarchy.AnyAsync())
            {
                return (true, ConnectionReason.Hierarchy);
            }
        }

        if (reasons.Contains(ConnectionReason.KeyRole))
        {
            var keyRoles =
            from a in db.Assignments.AsNoTracking()
            join k in db.Assignments.AsNoTracking() on a.ToId equals k.FromId            
            join kr in db.Roles.AsNoTracking() on k.RoleId equals kr.Id
            where
                a.FromId == fromId && 
                kr.IsKeyRole == true && 
                k.ToId == toId && 
                (a.RoleId != RoleConstants.ParticipantSharedResponsibility.Id || a.From.VariantId != EntityVariantConstants.IKS.Id) &&
                (k.RoleId != RoleConstants.ParticipantSharedResponsibility.Id || a.To.VariantId != EntityVariantConstants.IKS.Id)
            select 1;

            if (await keyRoles.AnyAsync())
            {
                return (true, ConnectionReason.KeyRole);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Returns connections between two entities based on assignments and delegations
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsAsync(ConnectionQueryFilter filter, ConnectionQueryDirection direction, CancellationToken ct = default)
    {
        try
        {
            bool delayChildNesting = true;
            bool delayFromFilter = true;
            if (direction == ConnectionQueryDirection.ToOthers || (filter.FromIds?.Count > 0 && filter.FromIds?.Count <= 20))
            {
                delayChildNesting = false;
                delayFromFilter = false;
            }

            var baseQuery = direction == ConnectionQueryDirection.FromOthers
                ? _baseQueryBuilder.FromOthers(
                        db,
                        filter,
                        filter.IncludeSubConnections && !delayChildNesting,
                        !filter.IncludeSubConnections || !delayFromFilter)
                : _baseQueryBuilder.ToOthers(db, filter);

            var result = baseQuery.Select(ToDtoEmpty).ToList();
            if (filter.IncludePackages || filter.EnrichPackageResources)
            {
                try
                {
                    var packageLoader = new ConnectionPackageLoader(db);
                    var pkgs = await packageLoader.LoadPackagesByKeyAsync(result, filter, ct);
                    if (filter.EnrichPackageResources)
                    {
                        await packageLoader.EnrichPackageResourcesAsync(pkgs, filter, ct);
                    }

                    result = Attach(result, pkgs, p => p.Id, (dto, list) => dto.Packages = list);

                    // Remove connections where no packages were found if filtering on specific packages
                    if (filter.PackageIds != null)
                    {
                        result.RemoveAll(t => t.Packages.Count == 0);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to include packages", ex);
                }
            }

            if (filter.IncludeResources || filter.IncludeInstances)
            {
                var resourceLoader = new ConnectionResourceLoader(db);
                var rightholderAssignments = ConnectionResourceLoader.GetRightholderAssignments(result, filter);

                try
                {
                    if (filter.IncludeResources)
                    {
                        var rightholderAssignmentIds = rightholderAssignments.Select(a => (Guid)a.AssignmentId).Distinct().ToHashSet();
                        if (rightholderAssignmentIds.Count > 0)
                        {
                            result = await resourceLoader.LoadResourcesByKeyAsync(result, rightholderAssignmentIds, filter, ct);
                        }

                        if (filter.IncludeDelegation && filter.IncludeDelegationResources)
                        {
                            var delegationIds = result.Where(r => r.DelegationId.HasValue).Select(r => (Guid)r.DelegationId!).Distinct().ToHashSet();
                            if (delegationIds.Count > 0)
                            {
                                result = await resourceLoader.LoadDelegationResourcesByKeyAsync(result, delegationIds, filter, ct);
                            }
                        }

                        // Remove connections where no resources were found if filtering on specific resources.
                        // Must run after the delegation resource merge; only resources on the record itself count.
                        if (filter.ResourceIds is { Count: > 0 })
                        {
                            result.RemoveAll(t => t.Resources.Count == 0);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to include resources", ex);
                }

                try
                {
                    if (filter.IncludeInstances)
                    {
                        var rightholderAssignmentIds = rightholderAssignments.Where(a => a.Reason != ConnectionReason.Hierarchy && !a.IsMainUnitAccess).Select(a => (Guid)a.AssignmentId).Distinct().ToHashSet();
                        if (rightholderAssignmentIds.Count > 0)
                        {
                            result = await resourceLoader.LoadInstancesByKeyAsync(result, rightholderAssignmentIds, filter, ct);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to include instances", ex);
                }
            }

            if (filter.EnrichEntities)
            {
                var enricher = new ConnectionEntityEnricher(db);
                result = await enricher.EnrichAsync(
                    result,
                    filter,
                    filter.IncludeSubConnections && delayChildNesting,
                    filter.IncludeSubConnections && delayFromFilter,
                    ct);
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get connections with filter: {JsonSerializer.Serialize(filter)}", ex);
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

    private static ConnectionQueryExtendedRecord ToDtoEmpty(ConnectionQueryBaseRecord x) => new()
    {
        FromId = x.FromId,
        ToId = x.ToId,
        RoleId = x.RoleId,
        AssignmentId = x.AssignmentId,
        DelegationId = x.DelegationId,
        ViaId = x.ViaId,
        ViaRoleId = x.ViaRoleId,
        Reason = x.Reason,
        IsKeyRoleAccess = x.IsKeyRoleAccess,
        IsMainUnitAccess = x.IsMainUnitAccess,
        IsRoleMap = x.IsRoleMap
    };
}
