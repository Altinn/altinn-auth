using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Constants;
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
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsFromOthersAsync(ConnectionQueryFilter filter, bool useNewQuery = true, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.FromOthers, useNewQuery, ct);
    }

    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsToOthersAsync(ConnectionQueryFilter filter, bool useNewQuery = true, CancellationToken ct = default)
    {
        return await GetConnectionsAsync(filter, ConnectionQueryDirection.ToOthers, useNewQuery, ct);
    }

    /// <summary>
    /// Returns connections between to entities based on assignments and delegations
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> GetConnectionsAsync(ConnectionQueryFilter filter, ConnectionQueryDirection direction, bool useNewQuery = true, CancellationToken ct = default)
    {
        try
        {
            var baseQuery = direction == ConnectionQueryDirection.FromOthers 
                ? useNewQuery ? BuildBaseQueryFromOthersNew(db, filter) : BuildBaseQueryFromOthers(db, filter)
                : BuildBaseQueryToOthers(db, filter);

            var queryString = baseQuery.ToQueryString();

            List<ConnectionQueryExtendedRecord> result;

            if (filter.EnrichEntities || filter.ExcludeDeleted || filter.IncludePackages || filter.EnrichPackageResources)
            {
                var query = EnrichEntities(filter, baseQuery);
                var data = await query.AsNoTracking().ToListAsync(ct);
                result = data.Select(ToDtoEmpty).ToList();

                try
                {
                    if (filter.IncludePackages || filter.EnrichPackageResources)
                    {
                        var pkgs = await LoadPackagesByKeyAsync(query, filter, ct);
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
            }
            else
            {
                var data = await baseQuery.AsNoTracking().ToListAsync(ct);
                result = data.Select(ToDtoEmpty).ToList();
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
    /// Slightly optimized connection packages lookup for PIP API
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> GetPipConnectionPackagesAsync(ConnectionQueryFilter filter, CancellationToken ct = default)
    {
        try
        {
            var baseQuery = BuildBaseQueryFromOthersNew(db, filter);
            var queryString = baseQuery.ToQueryString();

            var query = EnrichFromEntities(filter, baseQuery);
            var data = await query.AsNoTracking().ToListAsync(ct);
            var result = data.Select(ToDtoEmpty).ToList();

            var pkgs = await LoadPackagesByKeyAsync(query, filter, ct);
            return Attach(result, pkgs, p => p.Id, (dto, list) => dto.Packages = list);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get pip connection packages with filter: {JsonSerializer.Serialize(filter)}", ex);
        }
    }

    /// <summary>
    /// Returns connections between to entities based on assignments and delegations
    /// </summary>
    public string GenerateDebugQuery(ConnectionQueryFilter filter, ConnectionQueryDirection direction, bool useNewQuery = true)
    {
        var baseQuery = direction == ConnectionQueryDirection.FromOthers
                ? useNewQuery ? BuildBaseQueryFromOthersNew(db, filter) : BuildBaseQueryFromOthers(db, filter)
                : BuildBaseQueryToOthers(db, filter);

        if (filter.EnrichEntities || filter.ExcludeDeleted)
        {
            return EnrichEntities(filter, baseQuery).ToQueryString();
        }
        else
        {
            return baseQuery.ToQueryString();
        }
    }

    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQueryFromOthersNew(AppDbContext db, ConnectionQueryFilter filter)
    {
        var toId = filter.ToIds.First();
        var fromSet = filter.FromIds?.Count > 0 ? new HashSet<Guid>(filter.FromIds) : null;
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;
        var reviRegnRoleSet = new HashSet<Guid>
        {
            RoleConstants.Accountant.Id,
            RoleConstants.Auditor.Id,
            RoleConstants.AccountantWithoutSigningRights.Id,
            RoleConstants.AccountantWithSigningRights.Id,
            RoleConstants.AccountantSalary.Id,
            RoleConstants.AssistantAuditor,
            RoleConstants.A0237.Id
        };

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
                    IsRoleMap = false
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
                    (x, a2) => new ConnectionQueryBaseRecord
                    {
                        AssignmentId = a2.Id,
                        DelegationId = null,
                        FromId = a2.FromId,
                        ToId = x.d.ToId,
                        RoleId = a2.RoleId,
                        ViaId = x.d.FromId,
                        ViaRoleId = x.d.RoleId,
                        Reason = ConnectionReason.KeyRole,
                        IsKeyRoleAccess = true,
                        IsMainUnitAccess = false,
                        IsRoleMap = false
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
                        IsRoleMap = true
                    });

        var delegations =
            db.Assignments
                .Where(t => t.ToId == toId)   
                .Where(t => t.RoleId == RoleConstants.Agent.Id)
                .Join(
                    db.Delegations,
                    dkr => dkr.Id,
                    d => d.ToId,
                    (dkr, d) => new { dkr, d }
                )
                .Join(
                    db.Assignments,
                    x => x.d.FromId,
                    fa => fa.Id,
                    (x, fa) => new ConnectionQueryBaseRecord
                    {
                        AssignmentId = null,
                        DelegationId = x.d.Id,
                        FromId = fa.FromId,
                        ToId = x.dkr.ToId,
                        RoleId = x.dkr.RoleId,
                        ViaId = fa.ToId,
                        ViaRoleId = fa.RoleId,
                        Reason = ConnectionReason.Delegation,
                        IsKeyRoleAccess = false,
                        IsMainUnitAccess = false,
                        IsRoleMap = false
                    });

        var a2 = filter.IncludeDelegation
            ? a1.Concat(rolemap).Concat(delegations)
            : a1.Concat(rolemap);

        var fromChildren =
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
                        IsRoleMap = c.IsRoleMap
                    });

        var innehaverConnections =
            from reviRegnConnection in a2
            join innehaverConnection in db.Assignments on reviRegnConnection.FromId equals innehaverConnection.FromId
            join innehaver in db.Entities on innehaverConnection.ToId equals innehaver.Id
            join enk in db.Entities on innehaverConnection.FromId equals enk.Id
            where reviRegnRoleSet.Contains(reviRegnConnection.RoleId)
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
                Reason = ConnectionReason.Hierarchy
            };

        /*
        // Gir timeouts i YT        
        var query = filter.OnlyUniqueResults
            ? a2.Union(fromChildren).Union(innehaverConnections)
            : a2.Concat(fromChildren).Concat(innehaverConnections);
        */

        var query = a2.Concat(fromChildren).Concat(innehaverConnections);

        return
            query
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);
    }

    private IQueryable<ConnectionQueryBaseRecord> BuildBaseQueryFromOthers(AppDbContext db, ConnectionQueryFilter filter)
    {
        /* Scenario: Ansatt X i BDO AS (ToId)
            Oppslag skal finne:
                - Direkte tilganger BDO AS 
                    - Tilganger skal arves til underenheter av BDO OSLO BEDR, BDO BERGEN BEDR (som skal inkluderes som subconnections)

                - Nøkkelrolle tilganger: Dersom X er Daglig leder i BDO AS
                    - Skal man også arve alle tilganger gitt til BDO AS
                    - Skal man også arve tilganger gitt til BDO BDO BERGEN BEDR (nøkkelrolle for hovedenhet gjelder også for underenheter)
                    - Dersom tilgangen er for ett Enkeltpersonforetak gjennom Revisor/Regnskapsfører forhold skal tilgangen også gjelde innehaver gitt at innehaver ikke er død eller enkeltpersonforetaket er slettet for mer enn 2 år siden

                - Klientdelegeringer: Dersom X er Agent for BDO AS
                    - Skal alle tilganger gjennom klientdelegeringer fra klienter av BDO AS som agenten har mottatt
                    - Dersom klienten er en hovedenhet, skal klientdelegeringen også gjelde alle underenheter til klienten
                    - Dersom klienten er ett Enkeltpersonforetak gjennom Revisor/Regnskapsfører forhold skal tilgangen også gjelde innehaver gitt at innehaver ikke er død eller enkeltpersonforetaket er slettet for mer enn 2 år siden
        */

        if (filter.ToIds.Count != 1)
        {
            throw new NotSupportedException("BuildBaseQueryFromOthers only supports lookups in context of a single ToId.");
        }

        var toSet = new HashSet<Guid>(filter.ToIds);
        var fromSet = filter.FromIds?.Count > 0 ? new HashSet<Guid>(filter.FromIds) : null;
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;

        var queries = new List<IQueryable<ConnectionQueryBaseRecord>>();

        #region Find all direct KeyRole assignments
        var keyRoleAssignments =
            from keyRoleAssignment in db.Assignments
            join role in db.Roles on keyRoleAssignment.RoleId equals role.Id
            where role.IsKeyRole
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = keyRoleAssignment.Id,
                DelegationId = null,
                FromId = keyRoleAssignment.FromId,
                ToId = keyRoleAssignment.ToId,
                RoleId = keyRoleAssignment.RoleId,
                ViaId = keyRoleAssignment.FromId,
                ViaRoleId = keyRoleAssignment.RoleId,
                IsKeyRoleAccess = false,
                IsRoleMap = false,
                IsMainUnitAccess = false,
                Reason = ConnectionReason.Assignment
            };

        keyRoleAssignments = keyRoleAssignments.AsNoTracking()
            .ToIdContains(toSet);
        #endregion

        #region Find all subunit KeyRole assignments
        var subunitKeyRoleAssignments =
            from keyRoleAssignment in keyRoleAssignments
            join subunit in db.Entities on keyRoleAssignment.FromId equals subunit.ParentId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = keyRoleAssignment.AssignmentId,
                DelegationId = null,
                FromId = subunit.Id,
                ToId = keyRoleAssignment.ToId,
                RoleId = keyRoleAssignment.RoleId,
                ViaId = keyRoleAssignment.ToId,
                ViaRoleId = keyRoleAssignment.RoleId,
                IsKeyRoleAccess = false,
                IsRoleMap = false,
                IsMainUnitAccess = true,
                Reason = ConnectionReason.Hierarchy
            };
        #endregion

        #region Find all KeyRole assignments
        var allKeyRoleAssignments = filter.IncludeSubConnections ?
            keyRoleAssignments.Union(subunitKeyRoleAssignments) :
            keyRoleAssignments;
        #endregion

        #region Find KeyRole assignments to ToParty
        var inheritedKeyRoleAssignments =
            from keyRoleAssignment in allKeyRoleAssignments
            join inheritedAssignment in db.Assignments on keyRoleAssignment.FromId equals inheritedAssignment.ToId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = inheritedAssignment.Id,
                DelegationId = null,
                FromId = inheritedAssignment.FromId,
                ToId = keyRoleAssignment.ToId,
                RoleId = inheritedAssignment.RoleId,
                ViaId = keyRoleAssignment.FromId,
                ViaRoleId = keyRoleAssignment.RoleId,
                IsKeyRoleAccess = true,
                IsRoleMap = false,
                IsMainUnitAccess = keyRoleAssignment.IsMainUnitAccess,
                Reason = ConnectionReason.KeyRole,
            };
        #endregion

        #region Find direct assignments to ToParty
        var directAssignments =
            from assignments in db.Assignments
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = assignments.Id,
                DelegationId = null,
                FromId = assignments.FromId,
                ToId = assignments.ToId,
                ViaId = assignments.ToId,
                RoleId = assignments.RoleId,
                ViaRoleId = null,
                IsKeyRoleAccess = false,
                IsRoleMap = false,
                IsMainUnitAccess = false,
                Reason = ConnectionReason.Assignment,
            };

        directAssignments = directAssignments.AsNoTracking()
            .ToIdContains(toSet);
        #endregion

        #region Combine to all assignments
        var allAssignments = filter.IncludeKeyRole ?
            directAssignments.Union(inheritedKeyRoleAssignments) :
            directAssignments;
        #endregion

        #region Find all RoleMap roles for Assignments
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
                IsKeyRoleAccess = assignment.IsKeyRoleAccess,
                IsRoleMap = true,
                IsMainUnitAccess = assignment.IsKeyRoleAccess,
                Reason = ConnectionReason.RoleMap
            };
        #endregion

        #region Find all Delegations to ToParty
        ////join toAssignment in db.Assignments on delegation.ToId equals toAssignment.Id
        var delegations =
            from toAssignment in allAssignments
            join delegation in db.Delegations on toAssignment.AssignmentId equals delegation.ToId
            join fromAssignment in db.Assignments on delegation.FromId equals fromAssignment.Id
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = null,
                DelegationId = delegation.Id,
                FromId = fromAssignment.FromId,
                ToId = toAssignment.ToId,
                RoleId = Guid.Empty,
                ViaId = fromAssignment.ToId,
                ViaRoleId = null,
                IsKeyRoleAccess = false,
                IsRoleMap = false,
                IsMainUnitAccess = false,
                Reason = ConnectionReason.Delegation
            };
        #endregion

        #region Combine to get all connections
        var allBaseConnections = filter.IncludeDelegation ?
            allAssignments.Union(roleMapAssignments).Union(delegations) :
            allAssignments.Union(roleMapAssignments);
        #endregion

        #region Include all subunit connections through hierarchy
        var subunitConnections =
            from connection in allBaseConnections
            join child in db.Entities on connection.FromId equals child.ParentId
            select new ConnectionQueryBaseRecord()
            {
                AssignmentId = connection.AssignmentId,
                DelegationId = connection.DelegationId,
                FromId = child.Id,
                ToId = connection.ToId,
                RoleId = connection.RoleId,
                ViaId = connection.FromId,
                ViaRoleId = null,
                IsKeyRoleAccess = connection.IsKeyRoleAccess,
                IsRoleMap = connection.IsRoleMap,
                IsMainUnitAccess = true,
                Reason = ConnectionReason.Hierarchy
            };
        #endregion

        #region Include all Innehavere connections through Revisor/Regnskapsfører connections to Enkeltpersonforetak
        var innehaverConnections =
            from reviRegnConnection in allBaseConnections
            join innehaverConnection in db.Assignments on reviRegnConnection.FromId equals innehaverConnection.FromId
            join innehaver in db.Entities on innehaverConnection.ToId equals innehaver.Id
            join enk in db.Entities on innehaverConnection.FromId equals enk.Id
            where (reviRegnConnection.RoleId == RoleConstants.Accountant.Id || reviRegnConnection.RoleId == RoleConstants.Auditor.Id)
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
                Reason = ConnectionReason.Hierarchy
            };
        #endregion

        var allConnections = filter.IncludeSubConnections ?
            allBaseConnections.Union(subunitConnections).Union(innehaverConnections) :
            allBaseConnections;

        return allConnections
            .FromIdContains(fromSet)
            .RoleIdContains(roleSet);
    }

    public IQueryable<ConnectionQueryBaseRecord> BuildBaseQueryToOthers(AppDbContext db, ConnectionQueryFilter filter)
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
        var roleSet = filter.RoleIds?.Count > 0 ? new HashSet<Guid>(filter.RoleIds) : null;

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
        Combine main and subunit assignments 
        */
        var allAssignments = direct.Union(mainAssignments);

        /*
        Add RoleMpa roles to allAssignments 
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
        var directDelegations =
           from delegation in db.Delegations
           join fromAssignment in allAssignments on delegation.FromId equals fromAssignment.AssignmentId
           join toAssignment in db.Assignments on delegation.ToId equals toAssignment.Id
           select new ConnectionQueryBaseRecord()
           {
               AssignmentId = null,
               DelegationId = delegation.Id,
               FromId = fromAssignment.FromId,
               ToId = toAssignment.ToId,
               RoleId = Guid.Empty,
               ViaId = fromAssignment.ToId,
               ViaRoleId = null,
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
            join keyRoleAssignment in db.Assignments on all.ToId equals keyRoleAssignment.FromId
            join role in db.Roles on keyRoleAssignment.RoleId equals role.Id
            where role.IsKeyRole
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
        return allAssignments
            .Union(roleMapAssignments)
            .Union(directDelegations)
            .Union(keyRoleAssignments)
            .ToIdContains(toSet)
            .RoleIdContains(roleSet);
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

    private IQueryable<ConnectionQueryRecord> EnrichFromEntities(ConnectionQueryFilter filter, IQueryable<ConnectionQueryBaseRecord> allKeys)
    {
        var entities = db.Entities.AsQueryable();

        var query = allKeys
            .Join(entities, c => c.FromId, e => e.Id, (c, f) => new { c, f })
            .WhereIf(filter.ExcludeDeleted, x => !x.f.IsDeleted)
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
                Reason = x.c.Reason,
            });

        return query;
    }

    private async Task<ConnectionIndex<ConnectionQueryPackage>> LoadPackagesByKeyAsync(IQueryable<ConnectionQueryRecord> allKeys, ConnectionQueryFilter filter, CancellationToken ct)
    {
        var packageSet = filter.PackageIds?.Count > 0 ? new HashSet<Guid>(filter.PackageIds) : null;

        var assignmentPackages = allKeys
            .Join(db.AssignmentPackages, c => c.AssignmentId, ap => ap.AssignmentId, (c, ap) => new { c, ap })
            .WhereIf(packageSet is not null, x => packageSet!.Contains(x.ap.PackageId));

        var rolePackages = allKeys
            .Join(db.RolePackages, c => c.RoleId, rp => rp.RoleId, (c, rp) => new { c, rp })
            .Where(t => t.rp.HasAccess && (t.rp.EntityVariantId == null || t.rp.EntityVariantId == t.c.From.VariantId))
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

public enum ConnectionQueryDirection
{
    FromOthers,
    ToOthers
}
