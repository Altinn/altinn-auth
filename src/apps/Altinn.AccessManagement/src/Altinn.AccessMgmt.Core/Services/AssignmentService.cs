using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class AssignmentService(AppDbContext db, ConnectionQuery connectionQuery) : IAssignmentService
{
    /// <inheritdoc/>
    public async Task<List<Authorization.Api.Contracts.AccessManagement.AssignmentPackageDto>> ImportAssignmentPackages(Guid fromId, Guid toId, List<string> packageUrns, AuditValues values = null, CancellationToken cancellationToken = default)
    {
        var packageIds = await db.Packages
            .AsNoTracking()
            .Where(p => packageUrns.Contains(p.Urn))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var assignment = await db.Assignments
            .AsNoTracking()
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            assignment = new Assignment()
            {
                FromId = fromId,
                ToId = toId,
                RoleId = RoleConstants.Rightholder,
                Audit_ValidFrom = values?.ValidFrom ?? DateTimeOffset.UtcNow,
            };
            await db.Assignments.AddAsync(assignment, cancellationToken);
            await db.SaveChangesAsync(values, cancellationToken);
        }

        var existingAssignmentPackages = await db.AssignmentPackages
            .AsNoTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => packageIds.Contains(a.PackageId))
            .ToListAsync(cancellationToken);

        List<Authorization.Api.Contracts.AccessManagement.AssignmentPackageDto> result = new();

        foreach (var existing in existingAssignmentPackages)
        {
            result.Add(DtoMapper.Convert(existing));
        }

        if (result.Count == packageIds.Count)
        {
            return result;
        }

        foreach (var packageId in packageIds.Except(existingAssignmentPackages.Select(p => p.PackageId)))
        {
            AssignmentPackage newAssignmentPackage = new AssignmentPackage()
            {
                AssignmentId = assignment.Id,
                PackageId = packageId,
                Audit_ValidFrom = values?.ValidFrom ?? DateTimeOffset.UtcNow,
            };

            await db.AssignmentPackages.AddAsync(newAssignmentPackage, cancellationToken);
            result.Add(DtoMapper.Convert(newAssignmentPackage));
        }

        await db.SaveChangesAsync(values, cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> RevokeImportedAssignmentPackages(Guid fromId, Guid toId, List<string> packageUrns, AuditValues values = null, bool onlyRemoveA2Packages = true, CancellationToken cancellationToken = default)
    {
        var packageIds = await db.Packages
            .AsNoTracking()
            .Where(p => packageUrns.Contains(p.Urn))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var assignment = await db.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .Include(a => a.To)
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return 0;
        }

        var existingAssignmentPackages = await db.AssignmentPackages
            .AsNoTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => packageIds.Contains(a.PackageId))
            .Where(a => !onlyRemoveA2Packages || a.Audit_ChangedBySystem == SystemEntityConstants.Altinn2RoleImportSystem.Id)
            .ToListAsync(cancellationToken);

        bool removeditem = false;
        foreach (var item in existingAssignmentPackages)
        {
            db.Remove(item);
            removeditem = true;
        }

        int result = 0;
        if (removeditem)
        {
            result = await db.SaveChangesAsync(values, cancellationToken);
        }

        if (!onlyRemoveA2Packages || assignment.Audit_ChangedBySystem == SystemEntityConstants.Altinn2RoleImportSystem.Id)
        {
            ValidationErrorBuilder errors = await CheckCascadingAssignmentRevoke(assignment.Id, cancellationToken);

            // if no existing dependencies remove assignment
            if (errors.IsEmpty)
            {
                db.Assignments.Remove(assignment);
                await db.SaveChangesAsync(values, cancellationToken);
            }
        }

        return result;
    }

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
    public async Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roleResult = await db.Roles.AsNoTracking()
            .Where(t => t.Code == roleCode)
            .ToListAsync(cancellationToken);

        if (roleResult == null || !roleResult.Any())
        {
            return null;
        }

        return await GetAssignment(fromId, toId, roleResult.First().Id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Assignment>> GetKeyRoleAssignments(Guid toId, CancellationToken cancellationToken = default)
    {
        return await db.Assignments.AsNoTracking()
            .Where(t => t.ToId == toId)
            .Include(t => t.Role)
            .Where(t => t.Role.IsKeyRole)
            .Include(t => t.From)
            .ToListAsync(cancellationToken);
    }

    #region Packages, Resources and Instances

    /// <inheritdoc/>
    public async Task<IEnumerable<AssignmentPackage>> GetAssignmentPackages(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await db.AssignmentPackages.AsNoTracking().Where(t => t.AssignmentId == assignmentId).Include(t => t.Package).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AssignmentResource>> GetAssignmentResources(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await db.AssignmentResources.AsNoTracking().Where(t => t.AssignmentId == assignmentId).Include(t => t.Resource).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AssignmentInstance>> GetAssignmentInstances(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        return await db.AssignmentInstances.AsNoTracking().Where(t => t.AssignmentId == assignmentId).Include(t => t.Resource).Include(t => t.Assignment).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AssignmentInstance>> GetAssignmentInstances(Guid assignmentId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        return await db.AssignmentInstances.AsNoTracking().Where(t => t.AssignmentId == assignmentId && t.ResourceId == resourceId).Include(t => t.Resource).Include(t => t.Assignment).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> AddAssignmentPackage(Guid userId, Guid assignmentId, Guid packageId, CancellationToken cancellationToken = default)
    {
        /*
        [X] Check if user is TS
        [X] Check if user assignment.roles has packages
        [X] Check if user assignment.assignmentpackages has package
        [?] Check if users has packages delegated?

        [ ] Check if package can be delegated
        */

        /* TODO: Future Sjekk om bruker er Tilgangsstyrer */
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var package = await db.Packages.SingleAsync(t => t.Id == packageId, cancellationToken);

        var userAssignments = await db.Assignments.AsNoTracking()
            .Where(t => t.FromId == assignment.FromId && t.ToId == userId)
            .ToListAsync(cancellationToken);

        if (!await HasPackage(assignment.FromId, userId, package.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have package '{1}'", user.Name, package.Name));
        }

        await db.AssignmentPackages.AddAsync(
            new AssignmentPackage()
            {
                AssignmentId = assignmentId,
                PackageId = packageId
            }, 
            cancellationToken
            );
        
        var result = await db.SaveChangesAsync(cancellationToken);
        if (result == 0)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> AddAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }
       
        db.AssignmentResources.Add(new AssignmentResource()
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignment.Id,
            ResourceId = resource.Id,
            PolicyPath = policyPath,
            PolicyVersion = policyVersion,
        });

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc />
    public async Task<bool> AddAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }

        db.AssignmentInstances.Add(new AssignmentInstance()
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignment.Id,
            ResourceId = resource.Id,
            InstanceId = instanceId,
            PolicyPath = policyPath,
            PolicyVersion = policyVersion,
        });

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UpsertAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }

        var res = await db.AssignmentResources.AsTracking().FirstOrDefaultAsync(t => t.AssignmentId == assignmentId && t.ResourceId == resource.Id, cancellationToken);

        if (res != null)
        {
            res.PolicyPath = policyPath;
            res.PolicyVersion = policyVersion;
        }
        else
        {
            res = new AssignmentResource()
            {
                Id = Guid.CreateVersion7(),
                AssignmentId = assignment.Id,
                ResourceId = resource.Id,
                PolicyPath = policyPath,
                PolicyVersion = policyVersion,
            };
            db.AssignmentResources.Add(res);
        }

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc />
    public async Task<bool> UpsertAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }

        var res = await db.AssignmentInstances.AsTracking().FirstOrDefaultAsync(t => t.AssignmentId == assignmentId && t.ResourceId == resource.Id && t.InstanceId == instanceId, cancellationToken);

        if (res != null)
        {
            res.PolicyPath = policyPath;
            res.PolicyVersion = policyVersion;
        }
        else
        {
            res = new AssignmentInstance()
            {
                Id = Guid.CreateVersion7(),
                AssignmentId = assignment.Id,
                ResourceId = resource.Id,
                InstanceId = instanceId,
                PolicyPath = policyPath,
                PolicyVersion = policyVersion,
            };
            db.AssignmentInstances.Add(res);
        }

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAssignmentPackage(Guid userId, Guid assignmentId, Guid packageId, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var package = await db.Packages.AsNoTracking().SingleAsync(t => t.Id == packageId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the package
        if (await HasPackage(assignment.Id, user.Id, package.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, package.Name, assignment.FromId));
        }

        var obj = await db.AssignmentPackages.SingleAsync(t => t.AssignmentId == assignment.Id && t.PackageId == package.Id, cancellationToken);
        db.AssignmentPackages.Remove(obj);

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }

        var obj = await db.AssignmentResources.SingleAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id, cancellationToken);
        db.AssignmentResources.Remove(obj);

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, CancellationToken cancellationToken = default)
    {
        var user = await db.Entities.AsNoTracking().SingleAsync(t => t.Id == userId, cancellationToken);
        var assignment = await db.Assignments.AsNoTracking().SingleAsync(t => t.Id == assignmentId, cancellationToken);
        var resource = await db.Resources.AsNoTracking().SingleAsync(t => t.Id == resourceId, cancellationToken);

        // Check if user has AccessManager (Tilgangsstyrer) role
        if (await HasRole(assignment.Id, user.Id, RoleConstants.AccessManager, cancellationToken))
        {
            throw new Exception("User does not have permission to add resource to assignment");
        }

        // Check if user has access to the resource
        if (await HasResource(assignment.Id, user.Id, resource.Id, cancellationToken))
        {
            throw new Exception(string.Format("User '{0}' does not have resource '{1}' for '{2}'", user.Name, resource.Name, assignment.FromId));
        }

        var obj = await db.AssignmentInstances.SingleAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id && t.InstanceId == instanceId, cancellationToken);
        db.AssignmentInstances.Remove(obj);

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    private async Task<bool> HasRole(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken)
    {
        return await db.Assignments.AsNoTracking().AnyAsync(t => t.FromId == fromId && t.ToId == toId && t.RoleId == roleId, cancellationToken);
    }

    private async Task<bool> HasPackage(Guid fromId, Guid toId, Guid packageId, CancellationToken cancellationToken)
    {
        // Filter needs to be tested
        var result = await connectionQuery.GetConnectionsFromOthersAsync(new ConnectionQueryFilter()
        {
            FromIds = [fromId],
            ToIds = [toId],
            PackageIds = [packageId],
            IncludePackages = true,
        });

        return result.Any();
    }

    private async Task<bool> HasResource(Guid fromId, Guid toId, Guid resourceId, CancellationToken cancellationToken)
    {
        // Filter needs to be tested
        var result = await connectionQuery.GetConnectionsFromOthersAsync(new ConnectionQueryFilter()
        {
            FromIds = [fromId],
            ToIds = [toId],
            ResourceIds = [resourceId],
            IncludeResources = true,
        });

        return result.Any();
    }

    #endregion

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, bool cascade = false, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = default;
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
    /// </summary>
    /// <param name="assignmentId">The id of the assignment to delete</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>dependencies on the assignment taht will be revoked together with the assignment if it is removed</returns>
    public async Task<ValidationErrorBuilder> CheckCascadingAssignmentRevoke(Guid assignmentId, CancellationToken cancellationToken)
    {
        ValidationErrorBuilder errors = default;

        var packages = await db.AssignmentPackages.AsNoTracking()
                    .Where(t => t.AssignmentId == assignmentId)
                    .ToListAsync(cancellationToken);
        if (packages != null && packages.Any())
        {
            errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("packages", string.Join(",", packages.Select(p => p.Id.ToString())))]);
        }

        var resources = await db.AssignmentResources.AsNoTracking()
            .Where(t => t.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);
        if (resources != null && resources.Any())
        {
            errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("resources", string.Join(",", resources.Select(p => p.Id.ToString())))]);
        }

        var delegationsFromAssingment = await db.Delegations.AsNoTracking()
            .Where(t => t.FromId == assignmentId)
            .ToListAsync(cancellationToken);
        if (delegationsFromAssingment != null && delegationsFromAssingment.Any())
        {
            errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("delegations", string.Join(",", delegationsFromAssingment.Select(p => p.Id.ToString())))]);
        }

        var delegationsToAssignment = await db.Delegations.AsNoTracking()
            .Where(t => t.ToId == assignmentId)
            .ToListAsync(cancellationToken);
        if (delegationsToAssignment != null && delegationsToAssignment.Any())
        {
            errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("delegations", string.Join(",", delegationsToAssignment.Select(p => p.Id.ToString())))]);
        }

        return errors;
    }

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteAssignment(Guid assignmentId, bool cascade = false, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = default;
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
    public async Task<Result<Assignment>> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = default;

        var fromEntity = await db.Entities.SingleAsync(t => t.Id == fromEntityId, cancellationToken);
        var toEntity = await db.Entities.SingleAsync(t => t.Id == toEntityId, cancellationToken);
        ValidatePartyIsNotNull(fromEntityId, fromEntity, ref errors, "$QUERY/party");
        ValidatePartyIsOrg(fromEntity, ref errors, "$QUERY/party");
        ValidatePartyIsNotNull(toEntityId, toEntity, ref errors, "$QUERY/to");
        ValidatePartyIsOrg(toEntity, ref errors, "$QUERY/to");

        var roleResult = await db.Roles.AsNoTracking().Where(t => t.Code == roleCode).ToListAsync(cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            Unreachable();
        }

        var roleId = roleResult.First().Id;
        var existingAssignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken);
        if (existingAssignment != null)
        {
            return existingAssignment;
        }

        if (errors.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        var assignment = new Assignment
        {
            FromId = fromEntityId,
            ToId = toEntityId,
            RoleId = roleId,
        };

        await db.Assignments.AddAsync(assignment, cancellationToken);
        var result = await db.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            Unreachable();
        }

        return assignment;
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignmentInternal(Guid fromEntityId, Guid toEntityId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roleResult = await db.Roles.AsNoTracking().Where(t => t.Code == roleCode).ToListAsync(cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            throw new Exception(string.Format("Role '{0}' not found", roleCode));
        }

        return await GetOrCreateAssignment(fromEntityId, toEntityId, roleResult.First().Id, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, Guid roleId, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken);
        if (assignment != null)
        {
            return assignment;
        }

        var role = await db.Roles.AsNoTracking().SingleAsync(t => t.Id == roleId, cancellationToken);
        if (role == null)
        {
            throw new Exception(string.Format("Role '{0}' not found", roleId));
        }

        /*
        var inheritedAssignments = await GetInheritedAssignment(fromEntityId, toEntityId, role.Id, cancellationToken: cancellationToken);
        if (inheritedAssignments != null && inheritedAssignments.Any())
        {
            if (inheritedAssignments.Count() == 1)
            {
                throw new Exception(string.Format("An inheirited assignment exists From:'{0}.FromName' Via:'{0}.ViaName' To:'{}.ToName'. Use Force = true to create anyway.", inheritedAssignments.First()));
            }

            throw new Exception(string.Format("Multiple inheirited assignment exists. Use Force = true to create anyway."));
        }
        */

        assignment = new Assignment()
        {
            FromId = fromEntityId,
            ToId = toEntityId,
            RoleId = role.Id,
            Audit_ValidFrom = audit?.ValidFrom ?? DateTimeOffset.UtcNow,
        };
        
        await db.Assignments.AddAsync(assignment, cancellationToken);
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

        return assignment;
    }

    /*
    /// <inheritdoc/>
    public async Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var filter = inheritedAssignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.RoleId, roleId);

        return await inheritedAssignmentRepository.Get(filter, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roleResult = await roleRepository.Get(t => t.Code, roleCode, cancellationToken: cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            throw new Exception(string.Format("Role not found '{0}'", roleCode));
        }

        var roleId = roleResult.First().Id;
        return await GetInheritedAssignment(fromId, toId, roleId, cancellationToken: cancellationToken);
    }
    */

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

    private static void ValidatePartyIsNotNull(Guid id, Entity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is null)
        {
            errors.Add(ValidationErrors.EntityNotExists, param, [new("partyId", id.ToString())]);
        }
    }

    private static void ValidatePartyIsOrg(Entity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is not null && !entity.Type.Name.Equals("Organisasjon", StringComparison.InvariantCultureIgnoreCase))
        {
            errors.Add(ValidationErrors.InvalidQueryParameter, param, [new("partyId", $"expected party of type 'Organisasjon' got '{entity.Type.Name}'.")]);
        }
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
