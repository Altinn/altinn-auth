using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class DelegationService(
    IRoleRepository roleRepository,
    IInheritedAssignmentRepository inheritedAssignmentRepository,
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
    private readonly IInheritedAssignmentRepository inheritedAssignmentRepository = inheritedAssignmentRepository;
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

        var filter = inheritedAssignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.RoleId, role.Id);

        var userAssignments = await assignmentRepository.Get();

        if (userAssignments == null || !userAssignments.Any())
        {
            return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public async Task<ExtDelegation> CreateDelgation(Guid userId, Guid fromAssignmentId, Guid toAssignmentId)
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
            Id = Guid.NewGuid(),
            FromId = fromAssignmentId,
            ToId = toAssignmentId
        };

        var res = await delegationRepository.Create(delegation);
        if (res == 0)
        {
            throw new Exception("Failed to create delegation");
        }

        return await delegationRepository.GetExtended(delegation.Id);
    }

    /// <inheritdoc/>
    public async Task<bool> AddPackageToDelegation(Guid userId, Guid delegationId, Guid packageId)
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

        var res = await delegationPackageRepository.Create(new DelegationPackage() { 
            Id = Guid.NewGuid(),
            DelegationId = delegationId,
            PackageId = packageId
        });

        return res > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> AddResourceToDelegation(Guid userId, Guid delegationId, Guid resourceId)
    {
        /*
        [ ] Check i Pacakge is Delegable (?)
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


        var res = await delegationResourceRepository.Create(new DelegationResource()
        {
            Id = Guid.NewGuid(),
            DelegationId = delegationId,
            ResourceId = resourceId
        });

        return res > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Delegation>> CreateClientDelegation(CreateSystemDelegationRequestDto request, Guid userId, Guid facilitatorPartyId)
    {
        // Find user : Fredrik
        var user = (await entityRepository.Get(userId)) ?? throw new Exception(string.Format("Party not found '{0}' for user", userId));

        // Find Facilitator : Regnskapsfolk
        var facilitator = (await entityRepository.Get(facilitatorPartyId)) ?? throw new Exception(string.Format("Party not found '{0}' for facilitator", facilitatorPartyId));

        // Find admin role : Tilgangstyrer eller KlientAdmin

        // Find Agent Role : AGENT
        var agentRole = await GetRole(request.AgentRole) ?? throw new Exception(string.Format("Role not found '{0}'", request.AgentRole));

        // Find Agent
        var agent = await GetOrCreateEntity(request.AgentId, request.AgentName, request.AgentId.ToString(), "Systembruker", "System") ?? throw new Exception(string.Format("Could not find or create party '{0}' for agent", request.AgentId));
        
        // Find ClientId : Bakeriet
        var client = (await entityRepository.Get(request.ClientId)) ?? throw new Exception(string.Format("Party not found '{0}' for client", request.ClientId));

        // Find or Create Agent Assignment : Regnskapsfolk - AGENT - SystemBruker01
        var agentAssignment = await GetOrCreateAssignment(facilitator, agent, agentRole) ?? throw new Exception(string.Format("Could not find or create assignment '{0}' - {1} - {2}", facilitator.Name, agentRole.Code, agent.Name));


        return await CreateClientDelegations(request.RolePackages, client, facilitator, agentAssignment);
    }

    private async Task<IEnumerable<Delegation>> CreateClientDelegations(List<CreateSystemDelegationRolePackageDto> rolepackages, Entity client, Entity facilitator, Assignment agentAssignment)
    {
        var result = new List<Delegation>();

        var rolepacks = new Dictionary<string, List<string>>();
        foreach (var role in rolepackages.Select(t => t.RoleIdentifier).Distinct())
        {
            rolepacks.Add(role, rolepackages.Where(t => t.RoleIdentifier == role).Select(t => t.PackageUrn).ToList());
        }

        foreach (var rp in rolepacks)
        {
            // Find ClientPartyId Role : REGN
            var clientRole = (await roleRepository.Get(t => t.Code, rp.Key)).First() ?? throw new Exception(string.Format("Role not found '{0}'", rp.Key));

            // Find ClientAssignment : Bakeriet - (REGN) - Regnskapsfolk 
            var clientAssignment = await GetOrCreateAssignment(client, facilitator, clientRole) ?? throw new Exception(string.Format("Could not find or create assignment '{0}' - {1} - {2}", client.Name, clientRole.Code, facilitator.Name));

            // Find or create Delegation : Bakeriet - Regnskapsfolka - SystemBruker01
            var delegation = await GetOrCreateDelegation(clientAssignment, agentAssignment, facilitator) ?? throw new Exception(string.Format("Could not find or create delegation '{0}' - {1} - {2}", client.Name, facilitator.Name, agentAssignment.Id));

            foreach (var package in rp.Value)
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

                // Find AssignmentPackage
                var clientPackages = await connectionPackageRepository.GetB(clientAssignment.Id);
                var assignmentPackage = clientPackages.FirstOrDefault(t => t.Urn == package);
                if (assignmentPackage == null)
                {
                    throw new Exception(string.Format("ClientPartyId assignment does not have the package '{0}'", package));
                }

                // Find or Create DelegationPackage
                var delegationPackage = await GetOrCreateDelegationPackage(delegation.Id, repoPackage.Id);
                if (delegationPackage == null)
                {
                    throw new Exception("Unable to add package to delegation");
                }
            }

            result.Add(delegation);
        }

        return result;
    }

    private async Task<DelegationPackage> GetOrCreateDelegationPackage(Guid delegationId, Guid packageId)
    {
        var delegationPackageFilter = delegationPackageRepository.CreateFilterBuilder();
        delegationPackageFilter.Equal(t => t.DelegationId, delegationId);
        delegationPackageFilter.Equal(t => t.PackageId, packageId);
        var delegationPackage = (await delegationPackageRepository.Get(delegationPackageFilter)).FirstOrDefault();
        if (delegationPackage == null)
        {
            var res = await delegationPackageRepository.Create(new DelegationPackage()
            {
                Id = Guid.CreateVersion7(),
                DelegationId = delegationId,
                PackageId = packageId
            });
            return (await delegationPackageRepository.Get(delegationPackageFilter)).FirstOrDefault();
        }
        else
        {
            return delegationPackage;
        }
    }

    private async Task<Delegation> GetOrCreateDelegation(Assignment from, Assignment to, Entity facilitator)
    {
        var delegationFilter = delegationRepository.CreateFilterBuilder();
        delegationFilter.Equal(t => t.FromId, from.Id);
        delegationFilter.Equal(t => t.ToId, to.Id);
        delegationFilter.Equal(t => t.FacilitatorId, facilitator.Id);
        var delegation = (await delegationRepository.Get(delegationFilter)).FirstOrDefault();
        if (delegation == null)
        {
            var res = await delegationRepository.Create(new Delegation()
            {
                Id = Guid.CreateVersion7(),
                FromId = from.Id,
                ToId = to.Id,
                FacilitatorId = facilitator.Id
            });

            return (await delegationRepository.Get(delegationFilter)).FirstOrDefault();
        }
        else
        {
            return delegation;
        }
    }

    private async Task<Entity> GetOrCreateEntity(Guid id, string name, string refId, string type, string variant)
    {
        var entity = await entityRepository.Get(id);
        if (entity != null)
        {
            return entity;
        }

        var entityType = (await entityTypeRepository.Get(t => t.Name, type)).First() ?? throw new Exception(string.Format("Type not found '{0}'", type));
        var variantFilter = entityVariantRepository.CreateFilterBuilder();
        variantFilter.Equal(t => t.TypeId, entityType.Id);
        variantFilter.Equal(t => t.Name, variant);
        var entityVariant = (await entityVariantRepository.Get(variantFilter)).First() ?? throw new Exception(string.Format("Variant not found '{0}'", type));

        await entityRepository.Create(new Entity()
        {
            Id = id,
            Name = name,
            RefId = refId,
            TypeId = entityType.Id,
            VariantId = entityVariant.Id
        });

        return await entityRepository.Get(id);
    }

    private async Task<Assignment> GetOrCreateAssignment(Entity from, Entity to, Role role)
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
            if (roleProvider.Name != "Digitaliseringsdirektoratet") // Get system from token
            {
                throw new Exception(string.Format("You cannot create assignment with the role '{0}' ({1})", role.Name, role.Code));
            }

            var res = await assignmentRepository.Create(new Assignment()
            {
                Id = Guid.NewGuid(),
                FromId = from.Id,
                ToId = to.Id,
                RoleId = role.Id
            });
        }

        return (await assignmentRepository.Get(filter)).FirstOrDefault();
    }

    private async Task<Role> GetRole(string code)
    {
        return (await roleRepository.Get(t => t.Code, code)).FirstOrDefault();
    }
}
