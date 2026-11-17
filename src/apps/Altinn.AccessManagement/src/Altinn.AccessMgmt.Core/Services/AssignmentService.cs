using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class AssignmentService(AppDbContext db) : IAssignmentService
{
    private const string AssignmentUniqueIndex = "ix_assignment_fromid_toid_roleid";

    /// <inheritdoc/>
    public async Task<IEnumerable<SystemuserClientDto>> GetClients(Guid toId, string[] roles, string[] packages, CancellationToken cancellationToken = default)
    {
        // Fetch role metadata
        var roleResult = QueryWrapper.WrapQueryResponse(await db.Roles.AsNoTracking().Where(t => roles.Contains(t.Code)).ToListAsync(cancellationToken));

        if (roleResult == null || !roleResult.Any() || roleResult.Count() != roles.Length)
        {
            throw new ArgumentException($"Filter: {nameof(roles)}, provided contains one or more role identifiers which cannot be found.");
        }

        var filterRoleIds = roleResult.Select(r => r.Id).ToList();

        // Fetch role-package metadata
        var rolePackageResult = QueryWrapper.WrapQueryResponse(await db.RolePackages.AsNoTracking().Where(t => filterRoleIds.Contains(t.RoleId)).ToListAsync(cancellationToken));

        // Fetch package metadata
        var packageResult = QueryWrapper.WrapQueryResponse(await db.Packages.AsNoTracking().ToListAsync(cancellationToken));

        if (!packages.All(p => packageResult.Select(pr => pr.Urn).Contains($"urn:altinn:accesspackage:{p}")))
        {
            throw new ArgumentException($"Filter: {nameof(packages)}, provided contains one or more package identifiers which cannot be found.");
        }

        // Fetch client assignments
        var clientAssignmentResult =
            await db.Assignments.AsNoTracking()
                .Include(t => t.From)
                .Where(t => t.ToId == toId && filterRoleIds.Contains(t.RoleId))
            .ToListAsync(cancellationToken);

        // Discard non-organization clients (for now). To be opened up for private individuals in the future.
        var clients = clientAssignmentResult.Where(c => c.From.TypeId == EntityTypeConstants.Organization.Entity.Id);

        // Fetch assignment packages
        QueryResponse<AssignmentPackage> assignmentPackageResult = null;
        if (roles.Contains(RoleConstants.Rightholder.Entity.Code))
        {
            var rettighetshaverClients = clients.Where(c => c.RoleId == roleResult.First(r => r.Code == RoleConstants.Rightholder.Entity.Code).Id);
            if (rettighetshaverClients.Any())
            {
                assignmentPackageResult = QueryWrapper.WrapQueryResponse(await db.AssignmentPackages.AsNoTracking().Where(t => rettighetshaverClients.Select(p => p.Id).Contains(t.AssignmentId)).ToListAsync(cancellationToken));
            }
        }

        return await GetFilteredClientsFromAssignments(clients, assignmentPackageResult, roleResult, packageResult, rolePackageResult, packages, cancellationToken);
    }

    private async Task<List<SystemuserClientDto>> GetFilteredClientsFromAssignments(IEnumerable<Assignment> assignments, IEnumerable<AssignmentPackage> assignmentPackages, QueryResponse<Role> roles, QueryResponse<Package> packages, QueryResponse<RolePackage> rolePackages, string[] filterPackages, CancellationToken cancellationToken)
    {
        Dictionary<Guid, SystemuserClientDto> clients = new();

        // Fetch Entity metadata        
        var entityVariants = await db.EntityVariants.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            var roleName = roles.First(r => r.Id == assignment.RoleId).Code;
            var assignmentPackageIds = assignmentPackages != null ? assignmentPackages.Where(ap => ap.AssignmentId == assignment.Id).Select(ap => ap.PackageId) : [];
            var assignmentPackageNames = assignmentPackageIds.Any() ? assignmentPackageIds.Select(ap => packages.First(p => p.Id == ap).Urn.Split(":").Last()).ToArray() : [];
            var rolePackageIds = rolePackages.Where(rp => rp.RoleId == assignment.RoleId && (!rp.EntityVariantId.HasValue || rp.EntityVariantId == assignment.From.VariantId)).Select(rp => rp.PackageId);
            var rolePackageNames = rolePackageIds.Select(rp => packages.First(p => p.Id == rp).Urn.Split(":").Last()).ToArray();

            // Skip client if connection provides neither assignment-packages or role-packages
            if (assignmentPackageNames.Length == 0 && rolePackageNames.Length == 0)
            {
                continue;
            }

            // Add client to dictionary if not already present
            if (!clients.TryGetValue(assignment.FromId, out SystemuserClientDto client))
            {
                client = new SystemuserClientDto()
                {
                    Party = new SystemuserClientDto.ClientParty
                    {
                        Id = assignment.FromId,
                        Name = assignment.From.Name,
                        OrganizationNumber = assignment.From.RefId,
                        UnitType = entityVariants.FirstOrDefault(ev => ev.Id == assignment.From.VariantId)?.Name
                    }
                };

                clients.Add(assignment.FromId, client);
            }

            // Add packages client has been assigned
            if (assignmentPackageNames.Length > 0)
            {
                client.Access.Add(new SystemuserClientDto.ClientRoleAccessPackages
                {
                    Role = roleName,
                    Packages = assignmentPackageNames
                });
            }

            // Add packages client has through role
            if (rolePackageNames.Length > 0)
            {
                client.Access.Add(new SystemuserClientDto.ClientRoleAccessPackages
                {
                    Role = roleName,
                    Packages = rolePackageNames
                });
            }
        }

        // Return only clients having all required filterpackages
        List<SystemuserClientDto> result = new();
        foreach (var client in clients.Keys)
        {
            var allClientPackages = clients[client].Access.SelectMany(rp => rp.Packages).Distinct();
            if (filterPackages.All(allClientPackages.Contains))
            {
                result.Add(clients[client]);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetAssignment(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Assignments.AsNoTracking()
            .Include(t => t.Role)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var result = await db.Assignments.AsNoTracking()
            .Where(t => t.FromId == fromId && t.ToId == toId && t.RoleId == roleId)
            .ToListAsync(cancellationToken);
        if (result == null || !result.Any())
        {
            return null;
        }

        return result.First();
    }

    /// <inheritdoc/>
    public async Task<List<Assignment>> GetFacilitatorAssignments(Guid fromId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roleResult = await db.Roles.AsNoTracking()
            .Where(t => t.Code == roleCode)
            .ToListAsync(cancellationToken);

        if (roleResult == null || !roleResult.Any())
        {
            return new List<Assignment>();
        }

        var result = await db.Assignments.AsNoTracking()
            .Where(t => t.FromId == fromId && t.RoleId == roleResult.First().Id)
            .ToListAsync(cancellationToken);

        if (result == null)
        {
            return new List<Assignment>();
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, bool cascade = false, CancellationToken cancellationToken = default)
    {
        ValidationProblemBuilder errors = default;
        ValidationProblemInstance errorResult = default;

        var fromEntity = await db.Entities.FirstOrDefaultAsync(t => t.Id == fromEntityId, cancellationToken);
        var toEntity = await db.Entities.FirstOrDefaultAsync(t => t.Id == toEntityId, cancellationToken);

        if (toEntity is null || fromEntity is null)
        {
            return null;
        }

        var roleResult = await db.Roles.AsNoTracking().Where(t => t.Code == roleCode).ToListAsync(cancellationToken);
        if (roleResult == null || !roleResult.Any() || roleResult.Count > 1)
        {
            Unreachable();
        }

        var role = roleResult.First();
        if (!role.IsAssignable)
        {
            errors.Add(ValidationErrors.UnableToRevokeRoleAssignment, "$QUERY/roleCode", [new("role", role.Code)]);
            if (errors.TryBuild(out errorResult))
            {
                return errorResult;
            }
        }

        var existingAssignment = await GetAssignment(fromEntityId, toEntityId, role.Id, cancellationToken: cancellationToken);
        if (existingAssignment == null)
        {
            return null;
        }
        else
        {
            if (!cascade)
            {
                errors = await CheckCascadingAssignmentRevoke(existingAssignment.Id, cancellationToken);
            }
        }

        if (errors.TryBuild(out errorResult))
        {
            return errorResult;
        }

        var assignment = await db.Assignments.SingleAsync(t => t.Id == existingAssignment.Id, cancellationToken);
        db.Assignments.Remove(assignment);
        var result = await db.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            Unreachable();
        }

        return null;
    }

    /// <summary>
    /// This does checks for dependencies on the assignment that will be revoked together with the assignment if it is removed
    /// 
    /// WARNING:    If this method is missing checks it can lead to cascading revokes of assignments and data loss that was not 
    ///             intended so this has to be updated toghether with any new feature that adds dependencies on assignments
    ///             Changes here must also be done in ConnectionService.RemoveAssignment
    /// There exist a similar test in Altinn.AccessMgmt.Core.Services.Legacy.DelegationMetadataEF.CheckCascadingAssignmentRevoke that 
    /// must be kept in sync if new connections are added.
    /// </summary>
    /// <param name="assignmentId">The id of the assignment to delete</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>dependencies on the assignment taht will be revoked together with the assignment if it is removed</returns>
    private async Task<ValidationProblemBuilder> CheckCascadingAssignmentRevoke(Guid assignmentId, CancellationToken cancellationToken)
    {
        ValidationProblemBuilder errors = default;

        var packages = await db.AssignmentPackages.AsNoTracking()
                    .Where(t => t.AssignmentId == assignmentId)
                    .ToListAsync(cancellationToken);
        if (packages != null && packages.Any())
        {
            errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("packages", string.Join(",", packages.Select(p => p.Id.ToString())))]);
        }

        var resources = await db.AssignmentResources.AsNoTracking()
            .Where(t => t.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        if (resources != null && resources.Any())
        {
            errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("resources", string.Join(",", resources.Select(p => p.Id.ToString())))]);
        }

        var instances = await db.AssignmentInstances.AsNoTracking()
            .Where(t => t.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        if (instances != null && instances.Any())
        {
            errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("resources", string.Join(",", instances.Select(p => p.Id.ToString())))]);
        }

        var delegationsFromAssingment = await db.Delegations.AsNoTracking()
            .Where(t => t.FromId == assignmentId)
            .ToListAsync(cancellationToken);
        if (delegationsFromAssingment != null && delegationsFromAssingment.Any())
        {
            errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("delegations", string.Join(",", delegationsFromAssingment.Select(p => p.Id.ToString())))]);
        }

        var delegationsToAssignment = await db.Delegations.AsNoTracking()
            .Where(t => t.ToId == assignmentId)
            .ToListAsync(cancellationToken);
        if (delegationsToAssignment != null && delegationsToAssignment.Any())
        {
            errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("delegations", string.Join(",", delegationsToAssignment.Select(p => p.Id.ToString())))]);
        }

        return errors;
    }

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteAssignment(Guid assignmentId, bool cascade = false, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        ValidationProblemBuilder errors = default;
        ValidationProblemInstance errorResult = default;

        var existingAssignment = await GetAssignment(assignmentId, cancellationToken: cancellationToken);
        if (existingAssignment == null)
        {
            return null;
        }
        else
        {
            if (!existingAssignment.Role.IsAssignable)
            {
                errors.Add(ValidationErrors.UnableToRevokeRoleAssignment, "$QUERY/assignmentId", [new("role", existingAssignment.Role.Code)]);
                if (errors.TryBuild(out errorResult))
                {
                    return errorResult;
                }
            }

            if (!cascade)
            {
                errors = await CheckCascadingAssignmentRevoke(existingAssignment.Id, cancellationToken);
            }
        }

        if (errors.TryBuild(out errorResult))
        {
            return errorResult;
        }

        db.Assignments.Remove(existingAssignment);

        int result;

        if (audit == null)
        {
            result = await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            result = await db.SaveChangesAsync(audit, cancellationToken);
        }

        if (result == 0)
        {
            Unreachable();
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, Guid roleId, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken);
        if (assignment != null)
        {
            return assignment;
        }

        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(t => t.Id == roleId, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role '{roleId}' not found");
        }

        assignment = new Assignment()
        {
            FromId = fromEntityId,
            ToId = toEntityId,
            RoleId = role.Id,
            Audit_ValidFrom = audit?.ValidFrom ?? DateTimeOffset.UtcNow,
        };

        await db.Assignments.AddAsync(assignment, cancellationToken);
        int result;

        try
        {
            if (audit == null)
            {
                result = await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                result = await db.SaveChangesAsync(audit, cancellationToken);
            }
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(AssignmentUniqueIndex))
        {
            // A concurrent request created the same assignment between the lookup and the insert; use that one.
            db.Entry(assignment).State = EntityState.Detached;
            return await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Assignment conflict detected, but the existing assignment could not be found.", ex);
        }

        if (result == 0)
        {
            Unreachable();
        }

        return assignment;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AssignmentOrRolePackageAccess>> GetPackagesForAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        List<AssignmentOrRolePackageAccess> result = new();

        // Get Assignment
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);

        // Add validation (@Isnes)
        if (assignment == null)
        {
            return result;
        }

        // Get AssignmentPackages
        QueryResponse<AssignmentPackage> assignmentPackageResult = null;
        assignmentPackageResult = QueryWrapper.WrapQueryResponse(await db.AssignmentPackages.AsNoTracking().Where(t => t.AssignmentId == assignment.Id).ToListAsync(cancellationToken));

        foreach (var assignmentPackage in assignmentPackageResult)
        {
            result.Add(new AssignmentOrRolePackageAccess { AssignmentId = assignment.Id, RoleId = assignment.RoleId, PackageId = assignmentPackage.PackageId, AssignmentPackageId = assignmentPackage.Id });
        }

        // Get RolePackages
        var rolePackageResult = await db.RolePackages.AsNoTracking().Where(t => t.RoleId == assignment.RoleId).ToListAsync(cancellationToken);

        foreach (var rolePackage in rolePackageResult)
        {
            result.Add(new AssignmentOrRolePackageAccess { AssignmentId = assignment.Id, RoleId = assignment.RoleId, PackageId = rolePackage.PackageId, RolePackageId = rolePackage.Id });
        }

        return result;
    }

    /// <summary>
    /// Removes all assignments tied to a deceased person. This includes all rightHolder assignments or migrated Altinn 2 assignments where the deadPerson is either the from or to party,
    /// as well as all agent assignments where the deadPerson is the agent. Removing the agent assignment will cascade to removing all client delegations for that agent.
    /// The bankruptcy estate administrator (konkursbo-tilgangsstyrer) role is removed as well, so the ER import does not grant the deadPerson access to new bankruptcy estates.
    /// </summary>
    public async Task ClearAssignmentsInAfterLife(Guid deadPerson, AuditValues audit, CancellationToken cancellationToken)
    {
        var currentTransaction = db.Database.CurrentTransaction is not null;
        await using var transaction = currentTransaction ? null : await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the parties the dead person administers bankruptcy estates for, before reading anything, so an ER import
        // granting estate access through one of them either commits first, and its grants are removed below, or waits and
        // then finds the administrator relationship gone. See BankruptcyEstateDelegationService.LockEstateAdministratorParties.
        var estateAdministratorParties = await db.Assignments.AsNoTracking()
           .Where(t => t.ToId == deadPerson && t.RoleId == RoleConstants.BankruptcyEstateAdministrator)
           .Select(t => t.FromId)
           .ToListAsync(cancellationToken);
        await BankruptcyEstateDelegationService.LockEstateAdministratorParties(db, estateAdministratorParties, cancellationToken);

        // Find all assignments where toId or fromId is deadPerson, and the role is one of the rightHolder roles used for delegations in Altinn, or Altinn 2 roles migrated to Altinn 3.
        List<Assignment> assignmentsToRevoke = await db.Assignments.AsNoTracking()
             .Where(t => t.ToId == deadPerson || t.FromId == deadPerson)
             .Where(t =>
                 t.RoleId == RoleConstants.Rightholder ||
                 t.RoleId == RoleConstants.AppControlledRightholder ||
                 t.Role.ProviderId == ProviderConstants.Altinn2.Id)
             .ToListAsync(cancellationToken);

        // All assignments where deadPerson is agent for an organization should be removed. Removing the agent assignment cascades to removing all client delegations for that agent.
        List<Assignment> agentAssignments = await db.Assignments.AsNoTracking()
           .Where(t => t.ToId == deadPerson && t.RoleId == RoleConstants.Agent)
           .ToListAsync(cancellationToken);

        // All assignments where deadPerson is bankruptcy estate administrator should be removed, otherwise the ER import gives deadPerson access to every new estate of the estate administrator party.
        List<Assignment> bankruptcyEstateAdministratorAssignments = await db.Assignments.AsNoTracking()
           .Where(t => t.ToId == deadPerson && t.RoleId == RoleConstants.BankruptcyEstateAdministrator)
           .ToListAsync(cancellationToken);

        if (!assignmentsToRevoke.Any() && !agentAssignments.Any() && !bankruptcyEstateAdministratorAssignments.Any())
        {
            return;
        }

        db.Assignments.RemoveRange(assignmentsToRevoke);
        db.Assignments.RemoveRange(agentAssignments);
        db.Assignments.RemoveRange(bankruptcyEstateAdministratorAssignments);
        await db.SaveChangesAsync(audit, cancellationToken);

        if (transaction is { })
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> ClearAssignmentsForDeletedSystemUser(Guid systemUserId, AuditValues audit, CancellationToken cancellationToken)
    {
        // A system user only ever receives access, so every assignment where it is the to-party is removed regardless of role.
        // Packages, resources and delegations tied to the removed assignments are removed by the existing FK cascades.
        List<Assignment> assignments = await db.Assignments.AsNoTracking()
            .Where(t => t.ToId == systemUserId)
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
        {
            return 0;
        }

        db.Assignments.RemoveRange(assignments);
        return db.SaveChanges(audit);
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
