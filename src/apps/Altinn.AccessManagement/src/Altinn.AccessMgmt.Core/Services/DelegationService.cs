using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class DelegationService(AppDbContext db, IAssignmentService assignmentService, IRoleService roleService, IPackageService packageService, IResourceService resourceService, IEntityService entityService) : IDelegationService 
{
    public AuditValues AuditValues { get; set; } = new AuditValues(AuditDefaults.InternalApi, AuditDefaults.InternalApi, Guid.NewGuid().ToString());

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
        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);

        if (result == 0)
        {
            throw new Exception("Failed to create delegation");
        }

        return await db.Delegations.AsNoTracking().SingleAsync(t => t.Id == delegation.Id);
    }

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

        var delegation = await GetDelegation(delegationId);
        var fromAssignment = await assignmentService.GetAssignment(delegation.FromId, cancellationToken);
        var toAssignment = await assignmentService.GetAssignment(delegation.ToId, cancellationToken);
        var assignmentPackages = await assignmentService.GetPackagesForAssignment(fromAssignment.Id);
        var rolePackages = await roleService.GetPackagesForRole(fromAssignment.RoleId, cancellationToken);

        if (assignmentPackages.Count(t => t.AssignmentPackageId == packageId) == 0 && rolePackages.Count(t => t.Id == packageId) == 0)
        {
            throw new Exception($"The source assignment does not have the package '{package.Name}'");
        }

        db.DelegationPackages.Add(new DelegationPackage()
        {
            DelegationId = delegationId,
            PackageId = packageId
        });

        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);

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

        var delegation = await GetDelegation(delegationId);
        var fromAssignment = await assignmentService.GetAssignment(delegation.FromId, cancellationToken);
        var toAssignment = await assignmentService.GetAssignment(delegation.ToId, cancellationToken);

        var assignmentResources = await assignmentService.GetAssignmentResources(fromAssignment.Id, cancellationToken);
        var roleResources = await roleService.GetRoleResources(fromAssignment.RoleId, cancellationToken);
        var rolePackages = await roleService.GetPackagesForRole(fromAssignment.RoleId, cancellationToken);

        var rolePackageResources = new Dictionary<Guid, List<Resource>>();
        foreach (var package in rolePackages)
        {
            rolePackageResources.Add(package.Id, [.. await packageService.GetPackageResources(package.Id)]);
        }

        if (assignmentResources.Count(t => t.Id == resourceId) == 0
            && roleResources.Count(t => t.Id == resourceId) == 0
            && rolePackageResources.SelectMany(t => t.Value).Count(t => t.Id == resourceId) == 0
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
        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
        
        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Delegation>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid facilitatorPartyId, CancellationToken cancellationToken)
    {
        // Find User
        var user = await entityService.GetEntity(AuditValues.ChangedBy, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for user", AuditValues.ChangedBy));

        // Find Facilitator
        var facilitator = await entityService.GetEntity(facilitatorPartyId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", facilitatorPartyId));

        // Find Client
        var client = await entityService.GetEntity(request.ClientId, cancellationToken) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        // Create Delegation and DelegationPackage(s)
        return await CreateClientDelegations(request, client, facilitator, cancellationToken);
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
                        var agent = await entityService.GetOrCreateEntity(request.AgentId, request.AgentName, request.AgentId.ToString(), "Systembruker", "AgentSystem", cancellationToken) ?? throw new Exception(string.Format("Could not find or create party '{0}' for agent", request.AgentId));

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

    private async Task<DelegationPackage> GetOrCreateDelegationPackage(Guid delegationId, Guid packageId, Guid? assignmentPackageId, Guid? rolePackageId, CancellationToken cancellationToken = default)
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
                    RolePackageId = rolePackageId.HasValue ? rolePackageId.Value : null
                },
                cancellationToken
            );
            var result = await db.SaveChangesAsync(AuditValues, cancellationToken);

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

    private async Task<Delegation> GetOrCreateDelegation(Assignment from, Assignment to, Entity facilitator, CancellationToken cancellationToken = default)
    {
        var delegation = await db.Delegations
        .AsNoTracking()
        .Where(t => t.FromId == from.Id && t.ToId == to.Id && t.FacilitatorId == facilitator.Id)
        .FirstOrDefaultAsync(cancellationToken);

        if (delegation == null)
        {
            await db.Delegations.AddAsync(
                new Delegation()
                {
                    FromId = from.Id,
                    ToId = to.Id,
                    FacilitatorId = facilitator.Id
                },
                cancellationToken
            );
            var result = await db.SaveChangesAsync(AuditValues, cancellationToken);

            return await db.Delegations
            .AsNoTracking()
            .Where(t => t.FromId == from.Id && t.ToId == to.Id && t.FacilitatorId == facilitator.Id)
            .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            return delegation;
        }
    }

    private async Task<Assignment> GetOrCreateAssignment(Entity from, Entity to, Role role)
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

            return await assignmentService.GetOrCreateAssignment(from.Id, to.Id, role.Id);
        }
    }
}
