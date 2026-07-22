using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// A query based on assignments and delegations
/// </summary>
public class ConnectionQuery(AppDbContext db)
{
    // This is a workaround to prevent Postgres from choosing a bad join order when there is a filter on FromId in delegations.
    // It looks like the same join order is chosen no matter if we use a low value (1) or a very large value. A very large value is
    // chosen to ensure that the cutoff doesn't really reduce the result set.
    private const int PostgresPlanHintTakeLimit = 100_000_000;
    private readonly ConnectionResourceLoader _resourceLoader = new(db);
    private readonly ConnectionPackageLoader _packageLoader = new(db);

    /// <summary>
    /// Shared set of role IDs for accountant/auditor roles used in innehaver connection lookups.
    /// </summary>
    private static readonly HashSet<Guid> ReviRegnRoleSet =
    [
        RoleConstants.Accountant.Id,
        RoleConstants.Auditor.Id,
        RoleConstants.AccountantWithoutSigningRights.Id,
        RoleConstants.AccountantWithSigningRights.Id,
        RoleConstants.AccountantSalary.Id,
        RoleConstants.AssistantAuditor.Id,
        RoleConstants.AuditorInCharge.Id
    ];
    
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsFromOthersAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, ct);
    }

    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsToOthersAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.ToOthers, ct);
    }

    /// <summary>
    /// Checks if connection exists between to parties
    /// Returns first result only (Assignment => Delegation => Hierarchy => KeyRole)
    /// </summary>
    /// <param name="fromId">From identifier</param>
    /// <param name="toId">To identifier</param>
    /// <returns></returns>
    public async Task<(bool Result, ConnectionReason? Reason)> HasConnection(Guid fromId, Guid toId)
    {
        return await HasConnection(fromId, toId, [ConnectionReason.Assignment, ConnectionReason.Delegation, ConnectionReason.Hierarchy, ConnectionReason.KeyRole]);
    }

    /// <summary>
    /// Checks if connection exists between to parties
    /// Returns first result only (Assignment => Delegation => Hierarchy => KeyRole)
    /// <param name="fromId">From identifier</param>
    /// <param name="toId">To identifier</param>
    /// <param name="reasons">Reasons to check</param>
    /// </summary>
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
    /// Returns connections between to entities based on assignments and delegations
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
                ? BuildBaseQueryFromOthers(
                        db,
                        filter,
                        filter.IncludeSubConnections && !delayChildNesting,
                        !filter.IncludeSubConnections || !delayFromFilter)
                : BuildBaseQueryToOthers(db, filter);

            var result = baseQuery.Select(ToDtoEmpty).ToList();
            if (filter.IncludePackages || filter.EnrichPackageResources)
            {
                try
                {
                    var pkgs = await _packageLoader.LoadPackagesByKeyAsync(result, filter, ct);
                    if (filter.EnrichPackageResources)
                    {
                        await _packageLoader.EnrichPackageResourcesAsync(pkgs, filter, ct);
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
                var rightholderAssignments = ConnectionResourceLoader.GetRightholderAssignments(result, filter);

                try
                {
                    if (filter.IncludeResources)
                    {
                        var rightholderAssignmentIds = rightholderAssignments.Select(a => (Guid)a.AssignmentId).Distinct().ToHashSet();
                        if (rightholderAssignmentIds.Count > 0)
                        {
                            result = await _resourceLoader.LoadResourcesByKeyAsync(result, rightholderAssignmentIds, filter, ct);
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
                            result = await _resourceLoader.LoadInstancesByKeyAsync(result, rightholderAssignmentIds, filter, ct);
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
                result = await EnrichEntities(
                    result,
                    direction,
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

    /// <summary>
    /// Builds the common filter HashSets used by both query directions.
    /// </summary>
    private static (HashSet<Guid>? ViaSet, HashSet<Guid>? ViaRoleSet, HashSet<Guid>? RoleSet, HashSet<Guid> RoleSetExclude) BuildFilterSets(ConnectionQueryFilter filter)
    {
        var viaSet = filter.ViaIds?.Count > 0 ? new HashSet<Guid>(filter.ViaIds) : null;
        var viaRoleSet = filter.ViaRoleIds?.Count > 0 ? new HashSet<Guid>(filter.ViaRoleIds) : null;
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;
        var roleSetExclude = filter.ExcludeRoleIds?.Count > 0 ? new HashSet<Guid>(filter.ExcludeRoleIds) : [];
        roleSetExclude.Add(RoleConstants.Supplier.Id); // Supplier role should never be included in results as it is only used for maskinporten schemas.
        if (!filter.IncludeAppControlledInstances)
        {
            roleSetExclude.Add(RoleConstants.AppControlledRightholder.Id); // App-controlled instance access should be excluded if not explicitly requested.
        }

        return (viaSet, viaRoleSet, roleSet, roleSetExclude);
    }

    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQueryFromOthers(AppDbContext db, ConnectionQueryFilter filter, bool doChildNesting, bool applyFromFilter)
    {
        var toId = filter.ToIds.First();
        var fromSet = filter.FromIds?.Count > 0 ? new HashSet<Guid>(filter.FromIds) : null;
        var fromSetForDelegation = fromSet != null && filter.IncludeDelegation ? new HashSet<Guid>(fromSet) : [];
        var (viaSet, viaRoleSet, roleSet, roleSetExclude) = BuildFilterSets(filter);

        if (fromSet != null && filter.IncludeDelegation)
        {
            var parentIds = db.Entities
                .Where(e => fromSet.Distinct().Contains(e.Id) && e.ParentId != null)
                .AsNoTracking()
                .Select(e => e.ParentId.Value)
                .Distinct()
                .ToList();
            fromSetForDelegation.UnionWith(parentIds);

            var enksOfInnh = db.Assignments
                .AsNoTracking()
                .Where(a => fromSet.Distinct().Contains(a.ToId))
                .Where(a => a.RoleId == RoleConstants.Innehaver.Id)
                .Select(a => a.FromId)
                .Distinct()
                .ToList();
            fromSetForDelegation.UnionWith(enksOfInnh);
        }

        var direct =
            db.Assignments
                .Where(a1 => a1.ToId == toId)
                .Select(a1 => new ConnectionQueryBaseRecord
                {
                    AssignmentId = a1.Id,
                    DelegationId = null,
                    FromId = a1.FromId,
                    ToId = a1.ToId,
                    RoleId = a1.RoleId,
                    ViaId = null,
                    ViaRoleId = null,
                    Reason = ConnectionReason.Assignment,
                    IsKeyRoleAccess = false,
                    IsMainUnitAccess = false,
                    IsRoleMap = false,
                });

        var keyrole =
            direct
                .Join(
                    db.Roles,
                    d => d.RoleId,
                    r => r.Id,
                    (d, r) => new { d, r }
                )
                .Where(x => x.r.IsKeyRole)
                .Join(
                    db.Assignments,
                    x => x.d.FromId,
                    a2 => a2.ToId,
                    (x, a2) => new { x, a2 }
                )
                .Where(z => 
                    !(z.a2.From.VariantId == EntityVariantConstants.IKS.Id && z.a2.RoleId == RoleConstants.ParticipantSharedResponsibility.Id) &&
                    !(z.a2.To.VariantId == EntityVariantConstants.IKS.Id && z.x.d.RoleId == RoleConstants.ParticipantSharedResponsibility.Id))
                .Select(z => new ConnectionQueryBaseRecord
                {
                    AssignmentId = z.a2.Id,
                    DelegationId = null,
                    FromId = z.a2.FromId,
                    ToId = z.x.d.ToId,
                    RoleId = z.a2.RoleId,
                    ViaId = z.x.d.FromId,
                    ViaRoleId = z.x.d.RoleId,
                    Reason = ConnectionReason.KeyRole,
                    IsKeyRoleAccess = true,
                    IsMainUnitAccess = false,
                    IsRoleMap = false,
                });

        var a1 = filter.IncludeKeyRole
            ? direct.Concat(keyrole)
            : direct;

        var rolemap =
            a1
                .Join(
                    db.RoleMaps,
                    dkr => dkr.RoleId,
                    rm => rm.HasRoleId,
                    (dkr, rm) => new ConnectionQueryBaseRecord
                    {
                        AssignmentId = dkr.AssignmentId,
                        DelegationId = null,
                        FromId = dkr.FromId,
                        ToId = dkr.ToId,
                        RoleId = rm.GetRoleId,
                        ViaId = dkr.ViaId,
                        ViaRoleId = null,
                        Reason = ConnectionReason.RoleMap,
                        IsKeyRoleAccess = dkr.IsKeyRoleAccess,
                        IsMainUnitAccess = false,
                        IsRoleMap = true,
                    });

        var delegations =
            db.Assignments
                .WhereIf(fromSetForDelegation.Count > 0, x => fromSetForDelegation.Contains(x.FromId))
                .Join(
                    db.Delegations,
                    fa => fa.Id,
                    d => d.FromId,
                    (fa, d) => new { fa, d }
                )
                //// Generate a postgres limit to force a better execution plan when there's a party filter. Ref. comment on constant PostgresPlanHintTakeLimit.
                .TakeIf(fromSetForDelegation.Count > 0, PostgresPlanHintTakeLimit)
                .Join(
                    db.Assignments,
                    x => x.d.ToId,
                    dkr => dkr.Id,
                    (x, dkr) => new { x, dkr }
                )
                .Where(t =>
                    t.dkr.ToId == toId &&
                    t.dkr.RoleId == RoleConstants.Agent.Id
                )
                .Select(t => new ConnectionQueryBaseRecord
                {
                    AssignmentId = null,
                    DelegationId = t.x.d.Id,
                    FromId = t.x.fa.FromId,
                    ToId = t.dkr.ToId,
                    RoleId = t.x.fa.RoleId,
                    ViaId = t.x.fa.ToId,
                    ViaRoleId = t.dkr.RoleId,
                    Reason = ConnectionReason.Delegation,
                    IsKeyRoleAccess = false,
                    IsMainUnitAccess = false,
                    IsRoleMap = false,
                });

        var a2 = filter.IncludeDelegation
            ? a1.Concat(rolemap).Concat(delegations)
            : a1.Concat(rolemap);

        var fromChildren = !doChildNesting ? a2 :
        a2
        .Join(
            db.Entities,
            c => c.FromId,
            e => e.ParentId,
            (c, e) => new ConnectionQueryBaseRecord
            {
                AssignmentId = c.AssignmentId,
                DelegationId = c.DelegationId,
                FromId = e.Id,
                ToId = c.ToId,
                RoleId = c.RoleId,
                ViaId = c.FromId,
                ViaRoleId = c.ViaRoleId,
                Reason = ConnectionReason.Hierarchy,
                IsKeyRoleAccess = c.IsKeyRoleAccess,
                IsMainUnitAccess = true,
                IsRoleMap = c.IsRoleMap,
            });

        var innehaverConnections =
            from reviRegnConnection in a2
            join innehaverConnection in db.Assignments on reviRegnConnection.FromId equals innehaverConnection.FromId
            join innehaver in db.Entities on innehaverConnection.ToId equals innehaver.Id
            join enk in db.Entities on innehaverConnection.FromId equals enk.Id
            where ReviRegnRoleSet.Contains(reviRegnConnection.RoleId)
                && innehaverConnection.RoleId == RoleConstants.Innehaver.Id
                && enk.VariantId == EntityVariantConstants.ENK.Id
                && innehaver.DateOfDeath == null
                && (!enk.IsDeleted || (enk.DeletedAt != null && enk.DeletedAt.Value.AddYears(2) > DateTime.UtcNow))
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = reviRegnConnection.AssignmentId,
                DelegationId = reviRegnConnection.DelegationId,
                FromId = innehaverConnection.ToId,
                ToId = reviRegnConnection.ToId,
                RoleId = reviRegnConnection.RoleId,
                ViaId = innehaverConnection.FromId,
                ViaRoleId = innehaverConnection.RoleId,
                IsKeyRoleAccess = reviRegnConnection.IsKeyRoleAccess,
                IsRoleMap = reviRegnConnection.IsRoleMap,
                IsMainUnitAccess = reviRegnConnection.IsMainUnitAccess,
                Reason = ConnectionReason.Hierarchy,
            };

        var query = doChildNesting
            ? filter.IncludeSubConnections ? a2.Concat(fromChildren).Concat(innehaverConnections) : a2.Concat(fromChildren)
            : filter.IncludeSubConnections ? a2.Concat(innehaverConnections) : a2;

        return
            query
            .FromIdContains(fromSet, applyFromFilter)
            .ViaIdContains(viaSet)
            .ViaRoleIdContains(viaRoleSet)
            .RoleIdContains(roleSet)
            .RoleIdExcludes(roleSetExclude);
    }

    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQueryToOthers(AppDbContext db, ConnectionQueryFilter filter)
    {
        /* Senario: Tilgangsstyrer i Bakerhansen Bergen BEDR (FromId) som er underenhet av Bakerhansen AS

       Oppslag skal finne:
           - Direkte tilganger gitt til BDO AS fra Bakerhansen Bergen BEDR
           - Direkte tilganger gitt til BDO AS fra hovedenheten Bakerhansen AS 
           - Andre direkte tilganger gjennom Rettighetshaver, ER-roller eller Altinn 2-roller

           - Nøkkelroller tilganger: Daglig leder i BDO AS arve tilganger gitt til BDO AS
               - Personer med nøkkelrolle skal returneres som sub-connections

           - Klientdelegeringer: Agent for BDO AS som har mottatt klientdelegeringer:
               - Direkte fra Bakerhansen Bergen BEDR til Agent
               - Fra Hovedenhet Bakerhansen AS til Agent

           Scenario: Innehaver av Enk (For 1. mars og tilgangsstyringsside for privatpersoner)
           - Revisor/Regnskapsfører forhold via ENK skal også dukke opp med tilgang til personen som er innehaver
               - Selve Revisor/Regnskapsfører org
               - Nøkkelrolle personer for Revi/regn
               - Agenter med mottatt klientdelegering for Enk
       */

        var fromId = filter.FromIds.First();
        var toSet = filter.ToIds?.Count > 0 ? new HashSet<Guid>(filter.ToIds) : null;
        var (viaSet, viaRoleSet, roleSet, roleSetExclude) = BuildFilterSets(filter);

        /*
        Direct Assignments
        */

        var direct =
            from childAss in db.Assignments
            where childAss.FromId == fromId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = childAss.Id,
                DelegationId = null,
                FromId = childAss.FromId,
                ToId = childAss.ToId,
                RoleId = childAss.RoleId,
                ViaId = null,
                ViaRoleId = null,
                IsRoleMap = false,
                IsKeyRoleAccess = false,
                IsMainUnitAccess = false,
                Reason = ConnectionReason.Assignment
            };

        /*
        If FromId is a subunit, this will get mainunit assignments
        */
        var mainAssignments =
            from e in db.Entities
            where e.Id == fromId
            join ass in db.Assignments on e.ParentId equals ass.FromId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = ass.Id,
                DelegationId = null,
                FromId = e.Id,      // Subunit (from-party)
                ToId = ass.ToId,    // BDO / mottaker av tilgang fra hovedenhet
                RoleId = ass.RoleId,// Regnskapsfører / rolle-tilgang gitt fra hovedenheten
                ViaId = ass.FromId, // Hovedenheten til from-party
                ViaRoleId = null,
                IsRoleMap = false,
                IsKeyRoleAccess = false,
                IsMainUnitAccess = true,
                Reason = ConnectionReason.Hierarchy
            };

        /*
        Combine direct and mainunit assignments based on filter request
        */
        var allAssignments = filter.IncludeMainUnitConnections ? direct.Union(mainAssignments) : direct;

        /*
        Add RoleMap roles to allAssignments 
        */
        var roleMapAssignments =
           from assignment in allAssignments
           join rolemap in db.RoleMaps on assignment.RoleId equals rolemap.HasRoleId
           select new ConnectionQueryBaseRecord()
           {
               AssignmentId = assignment.AssignmentId,
               DelegationId = null,
               FromId = assignment.FromId,
               ToId = assignment.ToId,
               RoleId = rolemap.GetRoleId,
               ViaId = assignment.ViaId,
               ViaRoleId = assignment.ViaRoleId,
               IsRoleMap = true,
               IsKeyRoleAccess = assignment.IsKeyRoleAccess,
               IsMainUnitAccess = assignment.IsMainUnitAccess,
               Reason = ConnectionReason.RoleMap
           };

        /*
        Add Delegations from AllAssignments
        */
        var clientDelegations =
           from delegation in db.Delegations
           join fromAssignment in allAssignments on delegation.FromId equals fromAssignment.AssignmentId
           join toAssignment in db.Assignments on delegation.ToId equals toAssignment.Id
           select new ConnectionQueryBaseRecord()
           {
               AssignmentId = null,
               DelegationId = delegation.Id,
               FromId = fromAssignment.FromId,
               ToId = toAssignment.ToId,
               RoleId = fromAssignment.RoleId,
               ViaId = fromAssignment.ToId,
               ViaRoleId = toAssignment.RoleId,
               IsRoleMap = false,
               IsKeyRoleAccess = false,
               IsMainUnitAccess = false,
               Reason = ConnectionReason.Delegation
           };

        /*
        Add KeyRoles on allAssignments
        */
        var keyRoleAssignments =
            from all in allAssignments.Concat(roleMapAssignments) // Must include RoleMap assignments
            join fromEntity in db.Entities on all.FromId equals fromEntity.Id
            join toEntity in db.Entities on all.ToId equals toEntity.Id
            join keyRoleAssignment in db.Assignments on all.ToId equals keyRoleAssignment.FromId            
            join role in db.Roles on keyRoleAssignment.RoleId equals role.Id
            where 
                role.IsKeyRole && 
                !(fromEntity.VariantId == EntityVariantConstants.IKS.Id && all.RoleId == RoleConstants.ParticipantSharedResponsibility.Id) &&
                !(toEntity.VariantId == EntityVariantConstants.IKS.Id && keyRoleAssignment.RoleId == RoleConstants.ParticipantSharedResponsibility.Id)
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = all.AssignmentId,
                DelegationId = null,
                FromId = all.FromId,
                ToId = keyRoleAssignment.ToId,
                RoleId = all.RoleId,
                ViaId = keyRoleAssignment.FromId,
                ViaRoleId = keyRoleAssignment.RoleId,
                IsKeyRoleAccess = true,
                IsRoleMap = all.IsRoleMap,
                IsMainUnitAccess = all.IsMainUnitAccess,
                Reason = ConnectionReason.KeyRole
            };

        /*
        Combine everything
        */
        IQueryable<ConnectionQueryBaseRecord> allCombined = allAssignments.Union(roleMapAssignments);
        if (filter.IncludeDelegation)
        {
            allCombined = allCombined.Union(clientDelegations);
        }

        if (filter.IncludeKeyRole)
        {
            allCombined = allCombined.Union(keyRoleAssignments);
        }

        return allCombined
            .ToIdContains(toSet)
            .ViaIdContains(viaSet)
            .ViaRoleIdContains(viaRoleSet)
            .RoleIdContains(roleSet)
            .RoleIdExcludes(roleSetExclude);
    }

    private async Task<List<ConnectionQueryExtendedRecord>> EnrichEntities(List<ConnectionQueryExtendedRecord> allKeys, ConnectionQueryDirection direction, ConnectionQueryFilter filter, bool doChildNesting, bool applyFromFilter, CancellationToken ct)
    {
        SortedSet<Guid> parties = [];
        foreach (var item in allKeys)
        {
            if (!parties.Contains(item.FromId))
            {
                parties.Add(item.FromId);
            }

            if (!parties.Contains(item.ToId))
            {
                parties.Add(item.ToId);
            }

            if (item.ViaId != null && !parties.Contains((Guid)item.ViaId))
            {
                parties.Add((Guid)item.ViaId);
            }
        }

        SortedList<Guid, Entity> entityDict = [];
        var entitites = await db
            .Entities
            .AsNoTracking()
            .Where(e => parties.Contains(e.Id))
            .Include(t => t.Parent)
            .Select(e => new Entity()
            {
                Id = e.Id,
                Name = e.Name,
                OrganizationIdentifier = e.OrganizationIdentifier,
                ParentId = e.ParentId,
                PersonIdentifier = e.PersonIdentifier,
                DateOfBirth = e.DateOfBirth,
                DateOfDeath = e.DateOfDeath,
                PartyId = e.PartyId,
                IsDeleted = e.IsDeleted,
                DeletedAt = e.DeletedAt,
                UserId = e.UserId,
                Username = e.Username,
                TypeId = e.TypeId,
                VariantId = e.VariantId,
                EmailIdentifier = e.EmailIdentifier,
                Parent = e.Parent != null ? new Entity()
                {
                    Id = e.Parent.Id,
                    Name = e.Parent.Name,
                    OrganizationIdentifier = e.Parent.OrganizationIdentifier,
                    ParentId = e.Parent.ParentId,
                    PersonIdentifier = e.Parent.PersonIdentifier,
                    DateOfBirth = e.Parent.DateOfBirth,
                    DateOfDeath = e.Parent.DateOfDeath,
                    PartyId = e.Parent.PartyId,
                    IsDeleted = e.Parent.IsDeleted,
                    DeletedAt = e.Parent.DeletedAt,
                    UserId = e.Parent.UserId,
                    Username = e.Parent.Username,
                    TypeId = e.Parent.TypeId,
                    VariantId = e.Parent.VariantId,
                    EmailIdentifier = e.Parent.EmailIdentifier
                }
                : null
            })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var entity in entitites)
        {
            entityDict.Add(entity.Id, entity);
        }

        SortedList<Guid, List<Entity>> childrenDict = [];
        if (doChildNesting)
        {
            var allChildren = await db
                .Entities
                .AsNoTracking()
                .Where(e => e.ParentId != null && entityDict.Keys.Contains((Guid)e.ParentId))
                .Select(e => new Entity()
                {
                    Id = e.Id,
                    Name = e.Name,
                    OrganizationIdentifier = e.OrganizationIdentifier,
                    ParentId = e.ParentId,
                    Parent = entityDict[(Guid)e.ParentId],
                    PersonIdentifier = e.PersonIdentifier,
                    DateOfBirth = e.DateOfBirth,
                    DateOfDeath = e.DateOfDeath,
                    PartyId = e.PartyId,
                    IsDeleted = e.IsDeleted,
                    DeletedAt = e.DeletedAt,
                    UserId = e.UserId,
                    Username = e.Username,
                    TypeId = e.TypeId,
                    VariantId = e.VariantId
                })
                .Distinct()
                .AsNoTracking()
                .ToListAsync(ct);

            if (applyFromFilter && filter.FromIds != null && filter.FromIds.Count > 0)
            {
                allChildren = allChildren.Where(c => filter.FromIds.Contains(c.Id)).ToList();
            }

            foreach (var child in allChildren)
            {
                if (!childrenDict.ContainsKey((Guid)child.ParentId))
                {
                    childrenDict.Add((Guid)child.ParentId, [child]);
                }
                else
                {
                    childrenDict[(Guid)child.ParentId].Add(child);
                }
            }
        }

        // Could be cached
        var roles = await db.Roles.Include(r => r.Provider).ThenInclude(p => p.Type).AsNoTracking().ToListAsync(ct);
        SortedList<string, Role> sortedRoles = [];
        foreach (var role in roles)
        {
            sortedRoles.Add(role.Id.ToString(), role);
        }

        List<ConnectionQueryExtendedRecord> keysWithChildren = [];
        foreach (var c in allKeys)
        {
            c.From = entityDict[c.FromId];
            c.To = entityDict[c.ToId];
            c.Via = c.ViaId != null ? entityDict[(Guid)c.ViaId] : null;
            c.Role = c.RoleId != Guid.Empty ? sortedRoles[c.RoleId.ToString()] : null;
            c.ViaRole = c.ViaRoleId != null && c?.ViaRoleId != Guid.Empty ? sortedRoles[c.ViaRoleId.ToString()] : null;
            keysWithChildren.Add(c);

            if (doChildNesting && c.Reason != ConnectionReason.Hierarchy && childrenDict.TryGetValue(c.From.Id, out List<Entity> childrenForKey))
            {
                foreach (var child in childrenForKey)
                {
                    keysWithChildren.Add(new()
                    {
                        AssignmentId = c.AssignmentId,
                        FromId = child.Id,
                        From = child,
                        To = c.To,
                        ToId = c.ToId,
                        ViaId = c.FromId,
                        Via = c.From,
                        RoleId = c.RoleId,
                        Role = c.Role,
                        ViaRoleId = c.ViaRoleId,
                        ViaRole = c.ViaRole,

                        DelegationId = c.DelegationId,
                        IsKeyRoleAccess = c.IsKeyRoleAccess,
                        IsMainUnitAccess = true,
                        IsRoleMap = c.IsRoleMap,
                        Reason = ConnectionReason.Hierarchy,

                        Packages = c.Packages,
                        Resources = c.Resources
                    });
                }
            }
        }

        if (applyFromFilter && filter.FromIds != null && filter.FromIds.Count > 0)
        {
            keysWithChildren = keysWithChildren.Where(c => filter.FromIds.Contains(c.FromId)).ToList();
        }

        return keysWithChildren;
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

    internal static IQueryable<ConnectionQueryBaseRecord> FromIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids, bool applyFromFilter = true)
    {
        if (!applyFromFilter || ids is null || ids.Count == 0)
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

    internal static IQueryable<ConnectionQueryBaseRecord> ViaIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ViaId.HasValue && t.ViaId.Value == id);
        }

        return query.Where(t => t.ViaId.HasValue && ids.Contains(t.ViaId.Value));
    }

    internal static IQueryable<ConnectionQueryBaseRecord> RoleIdExcludes(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.RoleId != id);
        }

        return query.Where(t => !ids.Contains(t.RoleId));
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

    internal static IQueryable<ConnectionQueryBaseRecord> ViaRoleIdContains(this IQueryable<ConnectionQueryBaseRecord> query, HashSet<Guid> ids)
    {
        if (ids is null || ids.Count == 0)
        {
            return query;
        }

        if (ids.Count == 1)
        {
            var id = ids.First();
            return query.Where(t => t.ViaRoleId.HasValue && t.ViaRoleId == id);
        }

        return query.Where(t => t.ViaRoleId.HasValue && ids.Contains(t.ViaRoleId.Value));
    }
}

public enum ConnectionQueryDirection
{
    FromOthers,
    ToOthers
}
