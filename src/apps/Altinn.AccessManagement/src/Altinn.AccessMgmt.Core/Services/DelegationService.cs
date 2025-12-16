using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class DelegationService(AppDbContext db, IAssignmentService assignmentService, IRoleService roleService, IPackageService packageService, IResourceService resourceService, IEntityService entityService) : IDelegationService
{
    private async Task<bool> CheckIfEntityHasRole(string roleCode, Guid fromId, Guid toId, CancellationToken cancellationToken)
    {
        var assignment = await assignmentService.GetAssignment(fromId, toId, roleCode, cancellationToken);
        if (assignment == null)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<Delegation> CreateDelgation(Guid userId, Guid fromAssignmentId, Guid toAssignmentId, CancellationToken cancellationToken)
    {
        var fromAssignment = await assignmentService.GetAssignment(fromAssignmentId, cancellationToken);
        var toAssignment = await assignmentService.GetAssignment(toAssignmentId, cancellationToken);

        // Sjekk om from og to deler en felles entitet
        if (fromAssignment.ToId != toAssignment.FromId)
        {
            throw new InvalidOperationException("Assignments are not connected. FromAssignment.ToId != ToAssignment.FromId");
        }

        /* TODO: Future Sjekk om bruker er Tilgangsstyrer */
        var delegation = new Delegation()
        {
            FromId = fromAssignmentId,
            ToId = toAssignmentId
        };

        await db.Delegations.AddAsync(delegation, cancellationToken);
        var result = await db.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            throw new Exception("Failed to create delegation");
        }

        return await db.Delegations.AsNoTracking().SingleAsync(t => t.Id == delegation.Id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Delegation> GetDelegation(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Delegations.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> AddPackageToDelegation(Guid userId, Guid delegationId, Guid packageId, CancellationToken cancellationToken = default)
    {
        /* 
        [X] Check if user is DelegationAdmin on ViaId 
        [X] Check if assignment has the package
        [X] Check if the assignment role has the package
        [X] Check i Pacakge is Delegable
        */

        var package = await packageService.GetPackage(packageId);
        if (!package.IsDelegable)
        {
            return false;
        }

        var delegation = await GetDelegation(delegationId, cancellationToken);
        var fromAssignment = await assignmentService.GetAssignment(delegation.FromId, cancellationToken);
        var toAssignment = await assignmentService.GetAssignment(delegation.ToId, cancellationToken);
        var assignmentPackages = await assignmentService.GetPackagesForAssignment(fromAssignment.Id, cancellationToken);
        var rolePackages = await roleService.GetRolePackages(fromAssignment.RoleId, variantId: null, includeResources: false, cancellationToken: cancellationToken);

        if (assignmentPackages.Count(t => t.AssignmentPackageId == packageId) == 0 && rolePackages.Count(t => t.Id == packageId) == 0)
        {
            throw new Exception($"The source assignment does not have the package '{package.Name}'");
        }

        db.DelegationPackages.Add(new DelegationPackage()
        {
            DelegationId = delegationId,
            PackageId = packageId
        });

        var result = await db.SaveChangesAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> AddResourceToDelegation(Guid userId, Guid delegationId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        /*
        [ ] Check i Package is Delegable (?)
        [ ] Check i Resource is Delegable
        */

        /* 
        [X] Check if user is DelegationAdmin on FromId 
        [X] Check if assignment has the resource
        [X] Check if the assignment role has the resource
        [X] Check if the assignment packages has the resource
        */

        var resource = await resourceService.GetResource(resourceId, cancellationToken);

        var delegation = await GetDelegation(delegationId, cancellationToken);
        var fromAssignment = await assignmentService.GetAssignment(delegation.FromId, cancellationToken);
        var toAssignment = await assignmentService.GetAssignment(delegation.ToId, cancellationToken);

        var assignmentResources = await assignmentService.GetAssignmentResources(fromAssignment.Id, cancellationToken);

        // TODO: Add Assignment Variant to include RoleVariantPackages
        var roleResources = await roleService.GetRoleResources(fromAssignment.RoleId, variantId: null, includePackageResoures: true, cancellationToken);

        if (assignmentResources.Count(t => t.Id == resourceId) == 0
            && roleResources.Count(t => t.Id == resourceId) == 0
            )
        {
            throw new Exception($"The source assignment does not have the resource '{resource.Name}'");
        }

        await db.DelegationResources.AddAsync(
            new DelegationResource()
            {
                DelegationId = delegationId,
                ResourceId = resourceId
            }, 
            cancellationToken
        );
        var result = await db.SaveChangesAsync(cancellationToken);
        
        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CreateDelegationResponseDto>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid facilitatorPartyId, CancellationToken cancellationToken)
    {
        // Find Facilitator
        var facilitator = await entityService.GetEntity(facilitatorPartyId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", facilitatorPartyId));

        // Find Client
        var client = await entityService.GetEntity(request.ClientId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        // Create Delegation and DelegationPackage(s)
        var delegations = await CreateClientDelegations(request, client, facilitator, cancellationToken);

        // Map to DTO and return
        return DtoMapper.Convert(delegations);
    }

    /// <inheritdoc />
    public async Task<int> RevokeImportedClientDelegation(ImportClientDelegationRequestDto request, AuditValues audit, bool onlyRemoveA2Packages = true, CancellationToken cancellationToken = default)
    {
        // Find Client
        var client = await entityService.GetEntity(request.ClientId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        Entity facilitator = null;
        if (request.Facilitator.HasValue)
        {
            // Find Facilitator
            facilitator = await entityService.GetEntity(request.Facilitator.Value, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", request.Facilitator.Value));

            // Delete Delegation
            return await RevokeImportedClientDelegations(request, client, facilitator, audit, onlyRemoveA2Packages, cancellationToken);
        }
        else
        {
            List<Assignment> assignments = await assignmentService.GetFacilitatorAssignments(client.Id, request.RolePackages.First().RoleIdentifier);
            int count = 0;
            foreach (Assignment assignment in assignments)
            {
                facilitator = await entityService.GetEntity(assignment.ToId, cancellationToken);
                if (facilitator != null)
                {
                    count += await RevokeImportedClientDelegations(request, client, facilitator, audit, onlyRemoveA2Packages, cancellationToken);
                }
            }

            return count;
        }
    }

    private async Task<int> RevokeImportedClientDelegations(ImportClientDelegationRequestDto request, Entity client, Entity facilitator, AuditValues audit, bool onlyRemoveA2Packages = true, CancellationToken cancellationToken = default)
    {
        int packagesRevoked = 0;
        
        // Find Agent Role
        var agentRole = await db.Roles.AsNoTracking().FirstOrDefaultAsync(t => string.Equals(t.Code.ToLower(), request.AgentRole.ToLower()), cancellationToken) ?? throw new Exception(string.Format("Role not found '{0}'", request.AgentRole));

        // Verify Delegation Packages
        Dictionary<string, List<PackageDto>> rolepacks = await VerifyDelegationPackages(request);

        foreach (var rp in rolepacks)
        {
            // Find ClientPartyId Role
            var clientRole = (await roleService.GetByCode(rp.Key)).First();
            if (clientRole == null)
            {
                return 0;
            }

            // Find ClientAssignment
            var clientAssignment = await assignmentService.GetAssignment(client.Id, facilitator.Id, clientRole.Id, cancellationToken);
            if (clientAssignment == null)
            {
                return 0;
            }

            var clientPackages = await assignmentService.GetPackagesForAssignment(clientAssignment.Id);

            var agent = await entityService.GetEntity(request.AgentId, cancellationToken) ?? throw new Exception(string.Format("Could not find party '{0}'", request.AgentId));
            var agentAssignment = await assignmentService.GetAssignment(facilitator.Id, agent.Id, agentRole.Id, cancellationToken);
            if (agentAssignment == null)
            {
                return 0;
            }

            var delegation = await db.Delegations.Where(t => t.FromId == clientAssignment.Id && t.ToId == agentAssignment.Id && t.FacilitatorId == facilitator.Id).FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (delegation != null)
            {

                foreach (var package in rp.Value)
                {
                    // TODO: Add "&& t.CanAssign" when data is ready
                    if (!clientPackages.Any(t => t.PackageId == package.Id))
                    {
                        continue;
                    }

                    // Revoke DelegationPackage
                    var revoked = await RevokeDelegationPackage(delegation.Id, package.Id, audit, onlyRemoveA2Packages, cancellationToken);

                    if (revoked)
                    {
                        packagesRevoked++;
                    }
                }

                // if no packages left remove delegation
                var remainingPackages = await db.DelegationPackages.AsNoTracking().Where(t => t.DelegationId == delegation.Id).ToListAsync(cancellationToken);
                if (remainingPackages.Count == 0)
                {
                    db.Delegations.Remove(delegation);
                    await db.SaveChangesAsync(audit, cancellationToken);
                }
            }

            if (!onlyRemoveA2Packages || agentAssignment.Audit_ChangedBySystem == SystemEntityConstants.Altinn2RoleImportSystem.Id)
            {
                await assignmentService.DeleteAssignment(agentAssignment.Id, false, audit, cancellationToken);
            }
        }
        
        return packagesRevoked;
    }

    private async Task<bool> RevokeDelegationPackage(Guid delegationId, Guid packageId, AuditValues audit, bool onlyRemoveA2, CancellationToken cancellationToken = default)
    {
        DelegationPackage delegationPackage = null;
        
        delegationPackage = await db.DelegationPackages
            .AsTracking()
            .Where(t => t.DelegationId == delegationId && t.PackageId == packageId)
            .Where(t => !onlyRemoveA2 || t.Audit_ChangedBySystem == SystemEntityConstants.Altinn2RoleImportSystem.Id) // Remove only A2 packages if flag is set
            .FirstOrDefaultAsync(cancellationToken);
        
        if (delegationPackage != null)
        {
            db.DelegationPackages.Remove(delegationPackage);
            var result = await db.SaveChangesAsync(audit, cancellationToken);

            return result > 0;
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CreateDelegationResponseDto>> ImportClientDelegation(ImportClientDelegationRequestDto request, AuditValues audit, CancellationToken cancellationToken)
    {
        // Find Facilitator
        var facilitator = await entityService.GetEntity(request.Facilitator.Value, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", request.Facilitator.Value));

        // Find Client
        var client = await entityService.GetEntity(request.ClientId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        // Create Delegation and DelegationPackage(s)
        var delegations = await ImportClientDelegations(request, client, facilitator, audit, cancellationToken);

        // Map to DTO and return
        return DtoMapper.Convert(delegations);
    }

    private async Task<IEnumerable<Delegation>> ImportClientDelegations(ImportClientDelegationRequestDto request, Entity client, Entity facilitator, AuditValues audit, CancellationToken cancellationToken = default)
    {
        var result = new List<Delegation>();

        // Find Agent Role
        var agentRole = await db.Roles.AsNoTracking().FirstOrDefaultAsync(t => t.Code == request.AgentRole, cancellationToken) ?? throw new Exception(string.Format("Role not found '{0}'", request.AgentRole));

        // Verify Delegation Packages
        Dictionary<string, List<PackageDto>> rolepacks = await VerifyDelegationPackages(request);

        Assignment agentAssignment = null;
        foreach (var rp in rolepacks)
        {
            // Find ClientPartyId Role
            var clientRole = (await roleService.GetByCode(rp.Key)).First() ?? throw new Exception(string.Format("Role not found '{0}'", rp.Key));

            // Find ClientAssignment
            var clientAssignment = await assignmentService.GetAssignment(client.Id, facilitator.Id, clientRole.Id, cancellationToken) ?? throw new Exception(string.Format("Could not find client assignment '{0}' - {1} - {2}", client.Name, clientRole.Code, facilitator.Name));
            var clientPackages = await assignmentService.GetPackagesForAssignment(clientAssignment.Id);

            Delegation delegation = null;
            foreach (var package in rp.Value)
            {
                // TODO: Add "&& t.CanAssign" when data is ready
                var clientPackage = clientPackages.FirstOrDefault(t => t.PackageId == package.Id);
                if (clientPackage == null)
                {
                    throw new Exception(string.Format("Party does not have the package '{0}'", package.Urn));
                }

                if (delegation == null)
                {
                    if (agentAssignment == null)
                    {
                        // Find Agent Entity
                        var agent = await entityService.GetEntity(request.AgentId, cancellationToken) ?? throw new Exception(string.Format("Could not find party '{0}'", request.AgentId));

                        // Find or Create Agent Assignment
                        agentAssignment = await GetOrCreateAssignment(facilitator, agent, agentRole, audit, cancellationToken) ?? throw new Exception(string.Format("Could not find or create assignment '{0}' - {1} - {2}", facilitator.Name, agentRole.Code, agent.Name));
                    }

                    // Find or Create Delegation
                    delegation = await GetOrCreateDelegation(clientAssignment, agentAssignment, facilitator, audit, cancellationToken) ?? throw new Exception(string.Format("Could not find or create delegation '{0}' - {1} - {2}", client.Name, facilitator.Name, agentAssignment.Id));
                }

                // Find AssignmentPackageId or RolePackageId
                Guid? assignmentPackageId = clientPackage.AssignmentPackageId;
                Guid? rolePackageId = clientPackage.RolePackageId;

                // Find or Create DelegationPackage
                var delegationPackage = await GetOrCreateDelegationPackage(delegation.Id, package.Id, assignmentPackageId, rolePackageId, audit, cancellationToken);
                if (delegationPackage == null)
                {
                    throw new Exception("Unable to add package to delegation");
                }
            }

            result.Add(delegation);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteDelegation(Guid delegationId, CancellationToken cancellationToken)
    {
        ValidationErrorBuilder errors = default;

        var existingDelegation = await GetDelegation(delegationId, cancellationToken: cancellationToken);
        if (existingDelegation == null)
        {
            return null;
        }

        db.Delegations.Remove(existingDelegation);
        var result = await db.SaveChangesAsync(cancellationToken);

        if (result == 0)
        {
            errors.Add(ValidationErrors.UnableToCompleteOperation, "$QUERY/delegationId");
        }

        return null;
    }

    private async Task<IEnumerable<Delegation>> CreateClientDelegations(CreateSystemDelegationRequestDto request, Entity client, Entity facilitator, CancellationToken cancellationToken = default)
    {
        var result = new List<Delegation>();

        // Find Agent Role
        var agentRole = await db.Roles.AsNoTracking().FirstOrDefaultAsync(t => t.Code == request.AgentRole, cancellationToken) ?? throw new Exception(string.Format("Role not found '{0}'", request.AgentRole));

        // Verify Delegation Packages
        Dictionary<string, List<PackageDto>> rolepacks = await VerifyDelegationPackages(request);

        Assignment agentAssignment = null;
        foreach (var rp in rolepacks)
        {
            // Find ClientPartyId Role
            var clientRole = (await roleService.GetByCode(rp.Key)).First() ?? throw new Exception(string.Format("Role not found '{0}'", rp.Key));

            // Find ClientAssignment
            var clientAssignment = await assignmentService.GetAssignment(client.Id, facilitator.Id, clientRole.Id, cancellationToken) ?? throw new Exception(string.Format("Could not find client assignment '{0}' - {1} - {2}", client.Name, clientRole.Code, facilitator.Name));
            var clientPackages = await assignmentService.GetPackagesForAssignment(clientAssignment.Id);

            Delegation delegation = null;
            foreach (var package in rp.Value)
            {
                // var filter = connectionPackageRepository.CreateFilterBuilder(); // not used?

                // TODO: Add "&& t.CanAssign" when data is ready
                var clientPackage = clientPackages.FirstOrDefault(t => t.PackageId == package.Id);
                if (clientPackage == null)
                {
                    throw new Exception(string.Format("Party does not have the package '{0}'", package.Urn));
                }

                if (delegation == null)
                {
                    if (agentAssignment == null)
                    {
                        // Find or Create Agent Entity
                        var agent = await entityService.GetOrCreateEntity(request.AgentId, request.AgentName, request.AgentId.ToString(), EntityTypeConstants.SystemUser.Entity.Name, EntityVariantConstants.AgentSystem.Entity.Name, cancellationToken) ?? throw new Exception(string.Format("Could not find or create party '{0}' for agent", request.AgentId));

                        // Handle existing agent entity found with different type or variant
                        if (agent.TypeId != EntityTypeConstants.SystemUser.Id || agent.VariantId != EntityVariantConstants.AgentSystem.Id)
                        {
                            throw new Exception(string.Format("An entity with id '{0}' already exists and is not a valid agent SystemUser", request.AgentId));
                        }

                        // Find or Create Agent Assignment
                        agentAssignment = await GetOrCreateAssignment(facilitator, agent, agentRole) ?? throw new Exception(string.Format("Could not find or create assignment '{0}' - {1} - {2}", facilitator.Name, agentRole.Code, agent.Name));
                    }

                    // Find or Create Delegation
                    delegation = await GetOrCreateDelegation(clientAssignment, agentAssignment, facilitator) ?? throw new Exception(string.Format("Could not find or create delegation '{0}' - {1} - {2}", client.Name, facilitator.Name, agentAssignment.Id));
                }

                // Find AssignmentPackageId or RolePackageId
                Guid? assignmentPackageId = clientPackage.AssignmentPackageId;
                Guid? rolePackageId = clientPackage.RolePackageId;

                // Find or Create DelegationPackage
                var delegationPackage = await GetOrCreateDelegationPackage(delegation.Id, package.Id, assignmentPackageId, rolePackageId);
                if (delegationPackage == null)
                {
                    throw new Exception("Unable to add package to delegation");
                }
            }

            result.Add(delegation);
        }

        return result;
    }

    private async Task<Dictionary<string, List<PackageDto>>> VerifyDelegationPackages(CreateSystemDelegationRequestDto request)
    {
        var rolepacks = new Dictionary<string, List<PackageDto>>();
        foreach (var role in request.RolePackages.Select(t => t.RoleIdentifier).Distinct())
        {
            rolepacks.Add(role, new List<PackageDto>());
            foreach (var packageUrn in request.RolePackages.Where(t => t.RoleIdentifier == role).Select(t => t.PackageUrn))
            {
                var package = await packageService.GetPackageByUrnValue(packageUrn);
                if (package == null)
                {
                    throw new Exception(string.Format("Package not found '{0}'", packageUrn));
                }

                rolepacks[role].Add(package);
            }
        }

        return rolepacks;
    }

    private async Task<Dictionary<string, List<PackageDto>>> VerifyDelegationPackages(ImportClientDelegationRequestDto request)
    {
        var rolepacks = new Dictionary<string, List<PackageDto>>();
        foreach (var role in request.RolePackages.Select(t => t.RoleIdentifier).Distinct())
        {
            rolepacks.Add(role, new List<PackageDto>());
            foreach (var packageUrn in request.RolePackages.Where(t => t.RoleIdentifier == role).Select(t => t.PackageUrn))
            {
                var package = await packageService.GetPackageByUrnValue(packageUrn);
                if (package == null)
                {
                    throw new Exception(string.Format("Package not found '{0}'", packageUrn));
                }

                rolepacks[role].Add(package);
            }
        }

        return rolepacks;
    }

    private async Task<DelegationPackage> GetOrCreateDelegationPackage(Guid delegationId, Guid packageId, Guid? assignmentPackageId, Guid? rolePackageId, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        var delegationPackage = await db.DelegationPackages
        .AsNoTracking()
        .Where(t => t.DelegationId == delegationId && t.PackageId == packageId)
        .WhereIf(assignmentPackageId.HasValue, t => t.AssignmentPackageId == assignmentPackageId.Value)
        .WhereIf(rolePackageId.HasValue, t => t.RolePackageId == rolePackageId.Value)
        .FirstOrDefaultAsync(cancellationToken);

        if (delegationPackage == null)
        {
            await db.DelegationPackages.AddAsync(
                new DelegationPackage()
                {
                    DelegationId = delegationId,
                    PackageId = packageId,
                    AssignmentPackageId = assignmentPackageId.HasValue ? assignmentPackageId.Value : null,
                    RolePackageId = rolePackageId.HasValue ? rolePackageId.Value : null,
                    Audit_ValidFrom = audit?.ValidFrom ?? DateTime.UtcNow
                },
                cancellationToken
            );
            
            if (audit == null)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await db.SaveChangesAsync(audit, cancellationToken);
            }

            return await db.DelegationPackages
            .AsNoTracking()
            .Where(t => t.DelegationId == delegationId && t.PackageId == packageId)
            .WhereIf(assignmentPackageId.HasValue, t => t.AssignmentPackageId == assignmentPackageId.Value)
            .WhereIf(rolePackageId.HasValue, t => t.RolePackageId == rolePackageId.Value)
            .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            return delegationPackage;
        }
    }

    private async Task<Delegation> GetOrCreateDelegation(Assignment from, Assignment to, Entity facilitator, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        var delegation = await db.Delegations
        .AsNoTracking()
        .Include(t => t.From)
        .Where(t => t.FromId == from.Id && t.ToId == to.Id && t.FacilitatorId == facilitator.Id)
        .FirstOrDefaultAsync(cancellationToken);

        if (delegation == null)
        {
            await db.Delegations.AddAsync(
                new Delegation()
                {
                    FromId = from.Id,
                    ToId = to.Id,
                    FacilitatorId = facilitator.Id,
                    Audit_ValidFrom = audit?.ValidFrom ?? DateTimeOffset.UtcNow
                },
                cancellationToken
            );

            if (audit == null)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await db.SaveChangesAsync(audit, cancellationToken);
            }

            return await db.Delegations
            .AsNoTracking()
            .Include(t => t.From)
            .Where(t => t.FromId == from.Id && t.ToId == to.Id && t.FacilitatorId == facilitator.Id)
            .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            return delegation;
        }
    }

    private async Task<Delegation> GetDelegation(Assignment from, Assignment to, Entity facilitator, CancellationToken cancellationToken = default)
    {
        return await db.Delegations
        .AsNoTracking()
        .Include(t => t.From)
        .Where(t => t.FromId == from.Id && t.ToId == to.Id && t.FacilitatorId == facilitator.Id)
        .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Assignment> GetOrCreateAssignment(Entity from, Entity to, Role role, AuditValues audit = null, CancellationToken cancellationToken = default)
    {
        var clientAssignment = await assignmentService.GetAssignment(from.Id, to.Id, role.Id);

        if (clientAssignment != null)
        {
            return clientAssignment;
        }
        else
        {
            var roleProvider = await db.Providers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == role.ProviderId);
            
            // Get system from token
            if (roleProvider.Code != "sys-altinn3")
            {
                throw new Exception(string.Format("You cannot create assignment with the role '{0}' ({1})", role.Name, role.Code));
            }

            return await assignmentService.GetOrCreateAssignment(from.Id, to.Id, role.Id, audit, cancellationToken);
        }
    }    
}
