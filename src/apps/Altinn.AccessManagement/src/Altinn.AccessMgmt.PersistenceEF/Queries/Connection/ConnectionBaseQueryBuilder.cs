using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Builds the base IQueryable for connection queries in both directions.
/// </summary>
internal class ConnectionBaseQueryBuilder
{
    // This is a workaround to prevent Postgres from choosing a bad join order when there is a filter on FromId in delegations.
    // It looks like the same join order is chosen no matter if we use a low value (1) or a very large value. A very large value is
    // chosen to ensure that the cutoff doesn't really reduce the result set.
    private const int PostgresPlanHintTakeLimit = 100_000_000;

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

    /// <summary>
    /// Builds the common filter HashSets used by both query directions.
    /// </summary>
    internal static (HashSet<Guid>? ViaSet, HashSet<Guid>? ViaRoleSet, HashSet<Guid>? RoleSet, HashSet<Guid> RoleSetExclude) BuildFilterSets(ConnectionQueryFilter filter)
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

    internal IQueryable<ConnectionQueryBaseRecord> FromOthers(AppDbContext db, ConnectionQueryFilter filter, bool doChildNesting, bool applyFromFilter)
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

        var keyroleNoIks =
            direct
                .Join(
                    db.Roles,
                    d => d.RoleId,
                    r => r.Id,
                    (d, r) => new { d, r }
                )
                .Where(x => x.r.IsKeyRole && x.r.Id != RoleConstants.ParticipantSharedResponsibility.Id)
                .Join(
                    db.Assignments,
                    x => x.d.FromId,
                    a2 => a2.ToId,
                    (x, a2) => new { x, a2 }
                )
                .Where(x => x.a2.RoleId != RoleConstants.ParticipantSharedResponsibility.Id)
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

        var keyrolePotentialIks =
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
                    z.a2.RoleId == RoleConstants.ParticipantSharedResponsibility.Id ||
                    z.x.d.RoleId == RoleConstants.ParticipantSharedResponsibility.Id)
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

        var keyrole = keyroleNoIks.Concat(keyrolePotentialIks);

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

    internal IQueryable<ConnectionQueryBaseRecord> ToOthers(AppDbContext db, ConnectionQueryFilter filter)
    {
        /* Scenario: Tilgangsstyrer i Bakerhansen Bergen BEDR (FromId) som er underenhet av Bakerhansen AS

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
}
