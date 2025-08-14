using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class DelegationService(
    IRoleRepository roleRepository,
    IAssignmentRepository assignmentRepository,
    IAssignmentPackageRepository assignmentPackageRepository,
    IAssignmentResourceRepository assignmentResourceRepository,
    IRolePackageRepository rolePackageRepository,
    IRoleResourceRepository roleResourceRepository,
    IDelegationRepository delegationRepository,
    IPackageRepository packageRepository,
    IResourceRepository resourceRepository,
    IDelegationPackageRepository delegationPackageRepository,
    IDelegationResourceRepository delegationResourceRepository,
    IAssignmentService assignmentService,
    IEntityRepository entityRepository,
    IConnectionRepository connectionRepository,
    IEntityTypeRepository entityTypeRepository,
    IEntityVariantRepository entityVariantRepository,
    IProviderRepository providerRepository,
    IEntityLookupRepository entityLookupRepository,
    IConnectionPackageRepository connectionPackageRepository
    ) : IDelegationService
{
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
    private readonly IAssignmentPackageRepository assignmentPackageRepository = assignmentPackageRepository;
    private readonly IAssignmentResourceRepository assignmentResourceRepository = assignmentResourceRepository;
    private readonly IRolePackageRepository rolePackageRepository = rolePackageRepository;
    private readonly IRoleResourceRepository roleResourceRepository = roleResourceRepository;
    private readonly IDelegationRepository delegationRepository = delegationRepository;
    private readonly IPackageRepository packageRepository = packageRepository;
    private readonly IResourceRepository resourceRepository = resourceRepository;
    private readonly IDelegationPackageRepository delegationPackageRepository = delegationPackageRepository;
    private readonly IDelegationResourceRepository delegationResourceRepository = delegationResourceRepository;
    private readonly IAssignmentService assignmentService = assignmentService;
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IConnectionRepository connectionRepository = connectionRepository;
    private readonly IEntityTypeRepository entityTypeRepository = entityTypeRepository;
    private readonly IEntityVariantRepository entityVariantRepository = entityVariantRepository;
    private readonly IEntityLookupRepository entityLookupRepository = entityLookupRepository;
    private readonly IConnectionPackageRepository connectionPackageRepository = connectionPackageRepository;

    private async Task<bool> CheckIfEntityHasRole(string roleCode, Guid fromId, Guid toId)
    {
        var role = (await roleRepository.Get(t => t.Code, roleCode)).First();

        var filter = assignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.RoleId, role.Id);

        var userAssignments = await assignmentRepository.Get(filter);

        if (userAssignments == null || !userAssignments.Any())
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public async Task<ExtDelegation> CreateDelgation(Guid userId, Guid fromAssignmentId, Guid toAssignmentId, ChangeRequestOptions options)
    {
        var fromAssignment = await assignmentRepository.GetExtended(fromAssignmentId);
        var toAssignment = await assignmentRepository.GetExtended(toAssignmentId);

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

        var res = await delegationRepository.Create(delegation, options: options);
        if (res == 0)
        {
            throw new Exception("Failed to create delegation");
        }

        return await delegationRepository.GetExtended(delegation.Id);
    }

    /// <inheritdoc/>
    public async Task<bool> AddPackageToDelegation(Guid userId, Guid delegationId, Guid packageId, ChangeRequestOptions options)
    {
        /* 
        [X] Check if user is DelegationAdmin on ViaId 
        [X] Check if assignment has the package
        [X] Check if the assignment role has the package
        [X] Check i Pacakge is Delegable
        */

        var package = await packageRepository.Get(packageId);

        if (!package.IsDelegable)
        {
            return false;
        }

        var delegation = await delegationRepository.GetExtended(delegationId);
        var fromAssignment = await assignmentRepository.GetExtended(delegation.FromId);
        var toAssignment = await assignmentRepository.GetExtended(delegation.ToId);

        var assignmentPackages = await assignmentPackageRepository.GetB(fromAssignment.Id);
        var rolePackages = await rolePackageRepository.Get(t => t.RoleId, fromAssignment.RoleId);

        if (assignmentPackages.Count(t => t.Id == packageId) == 0 && rolePackages.Count(t => t.Id == packageId) == 0) 
        {
            throw new Exception($"The source assignment does not have the package '{package.Name}'");
        }

        var res = await delegationPackageRepository.Create(
            new DelegationPackage() 
            { 
                DelegationId = delegationId,
                PackageId = packageId
            },
            options: options
        );

        return res > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> AddResourceToDelegation(Guid userId, Guid delegationId, Guid resourceId, ChangeRequestOptions options)
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

        var resource = await resourceRepository.Get(resourceId);

        var delegation = await delegationRepository.GetExtended(delegationId);
        var fromAssignment = await assignmentRepository.GetExtended(delegation.FromId);
        var toAssignment = await assignmentRepository.GetExtended(delegation.ToId);

        var assignmentResources = await assignmentResourceRepository.GetB(fromAssignment.Id);
        var roleResources = await roleResourceRepository.GetB(fromAssignment.RoleId);
        var rolePackages = await rolePackageRepository.Get(t => t.RoleId, fromAssignment.RoleId);
        var rolePackageResources = new Dictionary<Guid, List<Resource>>();
        foreach (var package in rolePackages)
        {
            rolePackageResources.Add(package.Id, [.. await roleResourceRepository.GetB(resourceId)]);
        }

        if (assignmentResources.Count(t => t.Id == resourceId) == 0 
            && roleResources.Count(t => t.Id == resourceId) == 0
            && rolePackageResources.SelectMany(t => t.Value).Count(t => t.Id == resourceId) == 0
            )
        {
            throw new Exception($"The source assignment does not have the resource '{resource.Name}'");
        }

        var res = await delegationResourceRepository.Create(
            new DelegationResource()
            {
                DelegationId = delegationId,
                ResourceId = resourceId
            },
            options: options
        );

        return res > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Delegation>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid facilitatorPartyId, ChangeRequestOptions options)
    {
        // Find User
        var user = (await entityRepository.Get(options.ChangedBy)) ?? throw new Exception(string.Format("Party not found '{0}' for user", options.ChangedBy));

        // Find Facilitator
        var facilitator = (await entityRepository.Get(facilitatorPartyId)) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", facilitatorPartyId));

        // Find Client
        var client = (await entityRepository.Get(request.ClientId)) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        // Create Delegation and DelegationPackage(s)
        return await CreateClientDelegations(request, client, facilitator, options);
    }

    private async Task<IEnumerable<Delegation>> CreateClientDelegations(CreateSystemDelegationRequestDto request, Entity client, Entity facilitator, ChangeRequestOptions options)
    {
        var result = new List<Delegation>();

        // Find Agent Role
        var agentRole = await GetRole(request.AgentRole) ?? throw new Exception(string.Format("Role not found '{0}'", request.AgentRole));

        // Verify Delegation Packages
        Dictionary<string, List<Package>> rolepacks = await VerifyDelegationPackages(request);

        Assignment agentAssignment = null;
        foreach (var rp in rolepacks)
        {
            // Find ClientPartyId Role
            var clientRole = (await roleRepository.Get(t => t.Code, rp.Key)).First() ?? throw new Exception(string.Format("Role not found '{0}'", rp.Key));

            // Find ClientAssignment
            var clientAssignment = await GetAssignment(client.Id, facilitator.Id, clientRole.Id) ?? throw new Exception(string.Format("Could not find client assignment '{0}' - {1} - {2}", client.Name, clientRole.Code, facilitator.Name));
            var clientPackages = await assignmentService.GetPackagesForAssignment(clientAssignment.Id);

            Delegation delegation = null;
            foreach (var package in rp.Value)
            {
                var filter = connectionPackageRepository.CreateFilterBuilder();

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
                        var agent = await GetOrCreateEntity(request.AgentId, request.AgentName, request.AgentId.ToString(), "Systembruker", "AgentSystem", options) ?? throw new Exception(string.Format("Could not find or create party '{0}' for agent", request.AgentId));

                        // Find or Create Agent Assignment
                        agentAssignment = await GetOrCreateAssignment(facilitator, agent, agentRole, options) ?? throw new Exception(string.Format("Could not find or create assignment '{0}' - {1} - {2}", facilitator.Name, agentRole.Code, agent.Name));
                    }

                    // Find or Create Delegation
                    delegation = await GetOrCreateDelegation(clientAssignment, agentAssignment, facilitator, options) ?? throw new Exception(string.Format("Could not find or create delegation '{0}' - {1} - {2}", client.Name, facilitator.Name, agentAssignment.Id));
                }

                // Find AssignmentPackageId or RolePackageId
                Guid? assignmentPackageId = clientPackage.AssignmentPackageId;
                Guid? rolePackageId = clientPackage.RolePackageId;

                // Find or Create DelegationPackage
                var delegationPackage = await GetOrCreateDelegationPackage(delegation.Id, package.Id, assignmentPackageId, rolePackageId, options);
                if (delegationPackage == null)
                {
                    throw new Exception("Unable to add package to delegation");
                }
            }

            result.Add(delegation);
        }

        return result;
    }

    private async Task<Dictionary<string, List<Package>>> VerifyDelegationPackages(CreateSystemDelegationRequestDto request)
    {
        var rolepacks = new Dictionary<string, List<Package>>();
        foreach (var role in request.RolePackages.Select(t => t.RoleIdentifier).Distinct())
        {
            rolepacks.Add(role, new List<Package>());
            foreach (var package in request.RolePackages.Where(t => t.RoleIdentifier == role).Select(t => t.PackageUrn))
            {
                // Find Package (check if exists)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
                var packageFilter = packageRepository.CreateFilterBuilder();
                packageFilter.Equal(t => t.Urn, package);
                var packages = await packageRepository.Get(packageFilter);
                if (packages == null || !packages.Any())
                {
                    throw new Exception(string.Format("Package not found '{0}'", package));
                }

                var repoPackage = packages.First();

                rolepacks[role].Add(repoPackage);
            }
        }

        return rolepacks;
    }

    private async Task<DelegationPackage> GetOrCreateDelegationPackage(Guid delegationId, Guid packageId, Guid? assignmentPackageId, Guid? rolePackageId, ChangeRequestOptions options)
    {
        var delegationPackageFilter = delegationPackageRepository.CreateFilterBuilder();
        delegationPackageFilter.Equal(t => t.DelegationId, delegationId);
        delegationPackageFilter.Equal(t => t.PackageId, packageId);

        if (assignmentPackageId != null)
        {
            delegationPackageFilter.Equal(t => t.AssignmentPackageId, assignmentPackageId);
        }

        if (rolePackageId != null)
        {
            delegationPackageFilter.Equal(t => t.RolePackageId, rolePackageId);
        }

        var delegationPackage = (await delegationPackageRepository.Get(delegationPackageFilter)).FirstOrDefault();
        if (delegationPackage == null)
        {
            var res = await delegationPackageRepository.Create(
                new DelegationPackage()
                {
                    DelegationId = delegationId,
                    PackageId = packageId,
                    AssignmentPackageId = assignmentPackageId.HasValue ? assignmentPackageId.Value : null,
                    RolePackageId = rolePackageId.HasValue ? rolePackageId.Value : null
                },
                options: options
            );

            return (await delegationPackageRepository.Get(delegationPackageFilter)).FirstOrDefault();
        }
        else
        {
            return delegationPackage;
        }
    }

    private async Task<Delegation> GetOrCreateDelegation(Assignment from, Assignment to, Entity facilitator, ChangeRequestOptions options)
    {
        var delegationFilter = delegationRepository.CreateFilterBuilder();
        delegationFilter.Equal(t => t.FromId, from.Id);
        delegationFilter.Equal(t => t.ToId, to.Id);
        delegationFilter.Equal(t => t.FacilitatorId, facilitator.Id);
        var delegation = (await delegationRepository.Get(delegationFilter)).FirstOrDefault();
        if (delegation == null)
        {
            var res = await delegationRepository.Create(
                new Delegation()
                {
                    FromId = from.Id,
                    ToId = to.Id,
                    FacilitatorId = facilitator.Id
                },
                options: options
            );

            return (await delegationRepository.Get(delegationFilter)).FirstOrDefault();
        }
        else
        {
            return delegation;
        }
    }

    private async Task<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant, ChangeRequestOptions options)
    {
        var entityType = (await entityTypeRepository.Get(t => t.Name, type)).First() ?? throw new Exception(string.Format("Type not found '{0}'", type));

        var variantFilter = entityVariantRepository.CreateFilterBuilder();
        variantFilter.Equal(t => t.TypeId, entityType.Id);
        variantFilter.Equal(t => t.Name, variant);
        var entityVariant = (await entityVariantRepository.Get(variantFilter)).First() ?? throw new Exception(string.Format("Variant not found '{0}'", type));
        
        var entity = await entityRepository.Get(id);
        if (entity != null)
        {
            if (!entity.TypeId.Equals(entityType.Id))
            {
                throw new ArgumentException(string.Format("Entity is not of desired type '{0}'", type), paramName: "Type");
            }

            if (!entity.VariantId.Equals(entityVariant.Id))
            {
                throw new ArgumentException(string.Format("Entity is not of desired variant '{0}'", variant), paramName: "Variant");
            }

            return entity;
        }

        await entityRepository.Create(
            new Entity()
            {
                Id = id,
                Name = name,
                RefId = refId,
                TypeId = entityType.Id,
                VariantId = entityVariant.Id
            },
            options: options
            );

        return await entityRepository.Get(id);
    }

    private async Task<Assignment> GetAssignment(Guid from, Guid to, Guid role)
    {
        var filter = assignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.RoleId, role);
        filter.Equal(t => t.FromId, from);
        filter.Equal(t => t.ToId, to);

        return (await assignmentRepository.Get(filter)).FirstOrDefault();
    }

    private async Task<Assignment> GetOrCreateAssignment(Entity from, Entity to, Role role, ChangeRequestOptions options)
    {
        var filter = assignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.RoleId, role.Id);
        filter.Equal(t => t.FromId, from.Id);
        filter.Equal(t => t.ToId, to.Id);
        var clientAssignment = await assignmentRepository.Get(filter);

        if (clientAssignment != null && clientAssignment.Any())
        {
            return clientAssignment.First();
        }
        else
        {
            var roleProvider = await providerRepository.Get(role.ProviderId);
            
            // Get system from token
            if (roleProvider.Code != "sys-altinn3")
            {
                throw new Exception(string.Format("You cannot create assignment with the role '{0}' ({1})", role.Name, role.Code));
            }

            var res = await assignmentRepository.Create(
                new Assignment()
                {
                    FromId = from.Id,
                    ToId = to.Id,
                    RoleId = role.Id
                }, 
                options: options
            );
        }

        return (await assignmentRepository.Get(filter)).FirstOrDefault();
    }

    private async Task<Role> GetRole(string code)
    {
        return (await roleRepository.Get(t => t.Code, code)).FirstOrDefault();
    }
}
