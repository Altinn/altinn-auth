using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class AssignmentService(
    IAssignmentRepository assignmentRepository,
    IPackageRepository packageRepository,
    IAssignmentPackageRepository assignmentPackageRepository,
    IRoleRepository roleRepository,
    IRolePackageRepository rolePackageRepository,
    IEntityRepository entityRepository,
    IEntityVariantRepository entityVariantRepository,
    IDelegationRepository delegationRepository
    ) : IAssignmentService
{
    private static readonly string RETTIGHETSHAVER = "rettighetshaver";
    private static readonly Guid PartyTypeOrganizationUuid = new Guid("8c216e2f-afdd-4234-9ba2-691c727bb33d");

    /// <inheritdoc/>
    public async Task<IEnumerable<ClientDto>> GetClients(Guid toId, string[] roles, string[] packages, CancellationToken cancellationToken = default)
    {
        // Fetch role metadata
        var roleFilter = roleRepository.CreateFilterBuilder();
        roleFilter.In(t => t.Code, roles);
        var roleResult = await roleRepository.Get(roleFilter, cancellationToken: cancellationToken);

        if (roleResult == null || !roleResult.Any() || roleResult.Count() != roles.Length)
        {
            throw new ArgumentException($"Filter: {nameof(roles)}, provided contains one or more role identifiers which cannot be found.");
        }

        var filterRoleIds = roleResult.Select(r => r.Id).ToList();

        // Fetch role-package metadata
        var rolePackFilter = rolePackageRepository.CreateFilterBuilder();
        rolePackFilter.In(t => t.RoleId, filterRoleIds);
        var rolePackageResult = await rolePackageRepository.Get(rolePackFilter, cancellationToken: cancellationToken);

        // Fetch package metadata
        var packageResult = await packageRepository.Get(cancellationToken: cancellationToken);

        if (!packages.All(p => packageResult.Select(pr => pr.Urn).Contains($"urn:altinn:accesspackage:{p}")))
        {
            throw new ArgumentException($"Filter: {nameof(packages)}, provided contains one or more package identifiers which cannot be found.");
        }

        // Fetch client assignments
        var clientFilter = assignmentRepository.CreateFilterBuilder();
        clientFilter.Equal(t => t.ToId, toId);
        clientFilter.In(t => t.RoleId, filterRoleIds);
        var clientAssignmentResult = await assignmentRepository.GetExtended(clientFilter, cancellationToken: cancellationToken);

        // Discard non-organization clients (for now). To be opened up for private individuals in the future.
        var clients = clientAssignmentResult.Where(c => c.From.TypeId == PartyTypeOrganizationUuid);

        // Fetch assignment packages
        QueryResponse<AssignmentPackage> assignmentPackageResult = null;
        if (roles.Contains(RETTIGHETSHAVER))
        {
            var rettighetshaverClients = clients.Where(c => c.RoleId == roleResult.First(r => r.Code == RETTIGHETSHAVER).Id);
            if (rettighetshaverClients.Any())
            {
                var assignmentPackageFilter = assignmentPackageRepository.CreateFilterBuilder();
                assignmentPackageFilter.In(t => t.AssignmentId, rettighetshaverClients.Select(p => p.Id));

                assignmentPackageResult = await assignmentPackageRepository.Get(assignmentPackageFilter, cancellationToken: cancellationToken);
            }
        }

        return await GetFilteredClientsFromAssignments(clients, assignmentPackageResult, roleResult, packageResult, rolePackageResult, packages, cancellationToken);
    }

    private async Task<List<ClientDto>> GetFilteredClientsFromAssignments(IEnumerable<ExtAssignment> assignments, IEnumerable<AssignmentPackage> assignmentPackages, QueryResponse<Role> roles, QueryResponse<Package> packages, QueryResponse<RolePackage> rolePackages, string[] filterPackages, CancellationToken ct)
    {
        Dictionary<Guid, ClientDto> clients = new();

        // Fetch Entity metadata
        var entityVariants = await entityVariantRepository.Get(cancellationToken: ct);

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
            if (!clients.TryGetValue(assignment.FromId, out ClientDto client))
            {
                client = new ClientDto()
                {
                    Party = new ClientDto.ClientParty
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
                client.Access.Add(new ClientDto.ClientRoleAccessPackages
                {
                    Role = roleName,
                    Packages = assignmentPackageNames
                });
            }

            // Add packages client has through role
            if (rolePackageNames.Length > 0)
            {
                client.Access.Add(new ClientDto.ClientRoleAccessPackages
                {
                    Role = roleName,
                    Packages = rolePackageNames
                });
            }
        }

        // Return only clients having all required filterpackages
        List<ClientDto> result = new();
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
    public async Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var filter = assignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.RoleId, roleId);

        var result = await assignmentRepository.Get(filter, cancellationToken: cancellationToken);
        if (result == null || !result.Any())
        {
            return null;
        }

        return result.First();
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default)
    {
        var roleResult = await roleRepository.Get(t => t.Code, roleCode, cancellationToken: cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            return null;
        }

        return await GetAssignment(fromId, toId, roleResult.First().Id);
    }

    /// <inheritdoc/>
    public async Task<int> ImportAdminAssignmentPackages(Guid toUuid, Guid fromUuid, IEnumerable<string> packages, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        int result = 0;

        if (packages == null || !packages.Any())
        {
            throw new ArgumentException("Packages cannot be null or empty", nameof(packages));
        }

        List<Guid> packageList = [];

        foreach (var package in packages)
        {
            var packageResult = await packageRepository.Get(t => t.Urn, package, cancellationToken: cancellationToken);
            if (packageResult == null || !packageResult.Any())
            {
                throw new ArgumentException($"Package with URN '{package}' not found", nameof(packages));
            }

            packageList.Add(packageResult.First().Id);
        }

        var assignment = await GetOrCreateAssignmentInternal(fromUuid, toUuid, RETTIGHETSHAVER, options, cancellationToken);
        if (assignment == null)
        {
            throw new Exception($"Assignment could not be created for fromUuid: {fromUuid} toUuid: {toUuid} roleCode: {RETTIGHETSHAVER}");
        }

        foreach (var packageId in packageList)
        {
            bool ok = await ImportAddPackageToAssignment(assignment.Id, packageId, options, cancellationToken);
            if (ok)
            {
                result++;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> RevokeAdminAssignmentPackages(Guid toUuid, Guid fromUuid, IEnumerable<string> packages, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        int result = 0;

        if (packages == null || !packages.Any())
        {
            throw new ArgumentException("Packages cannot be null or empty", nameof(packages));
        }

        List<Guid> packageList = [];
        foreach (var package in packages)
        {
            var packageResult = await packageRepository.Get(t => t.Urn, package, cancellationToken: cancellationToken);
            if (packageResult == null || !packageResult.Any())
            {
                throw new ArgumentException($"Package with URN '{package}' not found", nameof(packages));
            }

            packageList.Add(packageResult.First().Id);
        }

        var assignment = await GetAssignment(fromUuid, toUuid, RETTIGHETSHAVER, cancellationToken) ?? throw new Exception($"Assignment could not be found for fromUuid: {fromUuid} toUuid: {toUuid} roleCode: {RETTIGHETSHAVER}");
        foreach (var packageId in packageList)
        {
            var assignmentPackageFilter = assignmentPackageRepository.CreateFilterBuilder();
            assignmentPackageFilter.Equal(t => t.AssignmentId, assignment.Id);
            assignmentPackageFilter.Equal(t => t.PackageId, packageId);
            var existingAssignmentPackage = await assignmentPackageRepository.Get(assignmentPackageFilter, cancellationToken: cancellationToken);
            if (existingAssignmentPackage != null && existingAssignmentPackage.Any())
            {
                int deleteResult = await assignmentPackageRepository.Delete(existingAssignmentPackage.First().Id, options, cancellationToken: cancellationToken);
                if (deleteResult > 0)
                {
                    result++;
                }
            }
        }

        return result;
    }

    private async Task<bool> ImportAddPackageToAssignment(Guid assignmentId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var assignmentPackages = await assignmentPackageRepository.GetB(assignmentId, cancellationToken: cancellationToken);
        if (assignmentPackages != null && assignmentPackages.Count(t => t.Id == packageId) > 0)
        {
            return false;
        }

        await assignmentPackageRepository.Create(
            new AssignmentPackage()
            {
                AssignmentId = assignmentId,
                PackageId = packageId
            },
            options: options,
            cancellationToken: cancellationToken
        );

        return true;        
    }

    /// <inheritdoc/>
    public async Task<bool> AddPackageToAssignment(Guid userId, Guid assignmentId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        /*
        [X] Check if user is TS
        [X] Check if user assignment.roles has packages
        [X] Check if user assignment.assignmentpackages has package
        [?] Check if users has packages delegated?

        [ ] Check if package can be delegated
        */

        var user = await entityRepository.Get(userId, cancellationToken: cancellationToken);

        var assignment = await assignmentRepository.Get(assignmentId, cancellationToken: cancellationToken);

        /* TODO: Future Sjekk om bruker er Tilgangsstyrer */

        var package = await packageRepository.Get(packageId, cancellationToken: cancellationToken);

        var userAssignmentFilter = assignmentRepository.CreateFilterBuilder();
        userAssignmentFilter.Equal(t => t.FromId, assignment.FromId);
        userAssignmentFilter.Equal(t => t.ToId, userId);
        var userAssignments = await assignmentRepository.Get(userAssignmentFilter, cancellationToken: cancellationToken);

        bool hasPackage = false;

        foreach (var userAssignment in userAssignments)
        {
            var assignmentPackages = await assignmentPackageRepository.GetB(userAssignment.Id, cancellationToken: cancellationToken);
            if (assignmentPackages != null && assignmentPackages.Count(t => t.Id == packageId) > 0)
            {
                hasPackage = true;
                break;
            }
        }

        if (!hasPackage)
        {
            // Check if AssigmentRole=>RolePackage has package
            foreach (var roleId in userAssignments.Select(t => t.RoleId).Distinct())
            {
                var rolePackResult = await rolePackageRepository.Get(t => t.RoleId, roleId, cancellationToken: cancellationToken);
                if (rolePackResult != null && rolePackResult.Count(t => t.PackageId == packageId) > 0)
                {
                    hasPackage = true;
                    break;
                }
            }
        }

        if (!hasPackage)
        {
            throw new Exception(string.Format("User '{0}' does not have package '{1}'", user.Name, package.Name));
        }

        await assignmentPackageRepository.Create(
            new AssignmentPackage()
            {
                AssignmentId = assignmentId,
                PackageId = packageId
            },
            options: options,
            cancellationToken: cancellationToken
        );

        return true;
    }

    /// <inheritdoc/>
    public Task<bool> AddResourceToAssignment(Guid userId, Guid assignmentId, Guid resourceId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        /*
        [ ] Check if user is TS
        [ ] Check if resource can be delegated
        [ ] Check if user assignment.assignmentpackages has resources
        [ ] Check if user assignment.roles has packages
        [ ] Check if users has packages delegated?
        */

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<ProblemInstance> DeleteAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options, bool cascade = false, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = default;
        var fromEntityExt = await entityRepository.GetExtended(fromEntityId, cancellationToken: cancellationToken);
        var toEntityExt = await entityRepository.GetExtended(toEntityId, cancellationToken: cancellationToken);
        ValidatePartyIsNotNull(fromEntityId, fromEntityExt, ref errors, "$QUERY/party");
        ValidatePartyIsOrg(fromEntityExt, ref errors, "$QUERY/party");
        ValidatePartyIsNotNull(toEntityId, toEntityExt, ref errors, "$QUERY/to");
        ValidatePartyIsOrg(toEntityExt, ref errors, "$QUERY/to");

        var roleResult = await roleRepository.Get(t => t.Code, roleCode, cancellationToken: cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            Unreachable();
        }

        var roleId = roleResult.First().Id;
        var existingAssignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken);
        if (existingAssignment == null)
        {
            return null;
        }
        else
        {
            if (!cascade)
            {
                var packages = await assignmentPackageRepository.Get(f => f.AssignmentId, existingAssignment.Id, cancellationToken: cancellationToken);
                if (packages != null && packages.Any())
                {
                    errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("packages", string.Join(",", packages.Select(p => p.Id.ToString())))]);
                }

                var delegations = await delegationRepository.Get(f => f.FromId, existingAssignment.Id, cancellationToken: cancellationToken);
                if (delegations != null && delegations.Any())
                {
                    errors.Add(ValidationErrors.AssignmentHasActiveConnections, "$QUERY/cascade", [new("delegations", string.Join(",", delegations.Select(p => p.Id.ToString())))]);
                }
            }
        }

        if (errors.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        var result = await assignmentRepository.Delete(existingAssignment.Id, options, cancellationToken: cancellationToken);
        if (result == 0)
        {
            Unreachable();
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<Result<Assignment>> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errors = default;
        var fromEntityExt = await entityRepository.GetExtended(fromEntityId, cancellationToken: cancellationToken);
        var toEntityExt = await entityRepository.GetExtended(toEntityId, cancellationToken: cancellationToken);
        ValidatePartyIsNotNull(fromEntityId, fromEntityExt, ref errors, "$QUERY/party");
        ValidatePartyIsOrg(fromEntityExt, ref errors, "$QUERY/party");
        ValidatePartyIsNotNull(toEntityId, toEntityExt, ref errors, "$QUERY/to");
        ValidatePartyIsOrg(toEntityExt, ref errors, "$QUERY/to");

        var roleResult = await roleRepository.Get(t => t.Code, roleCode, cancellationToken: cancellationToken);
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

        var result = await assignmentRepository.Create(assignment, options: options, cancellationToken: cancellationToken);
        if (result == 0)
        {
            Unreachable();
        }

        return assignment;
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignmentInternal(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var roleResult = await roleRepository.Get(t => t.Code, roleCode, cancellationToken: cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            throw new Exception(string.Format("Role '{0}' not found", roleCode));
        }

        return await GetOrCreateAssignment(fromEntityId, toEntityId, roleResult.First().Id, options: options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, Guid roleId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken: cancellationToken);
        if (assignment != null)
        {
            return assignment;
        }

        var role = await roleRepository.Get(roleId, cancellationToken: cancellationToken);
        if (role == null)
        {
            throw new Exception(string.Format("Role '{0}' not found", roleId));
        }

        assignment = new Assignment()
        {
            FromId = fromEntityId,
            ToId = toEntityId,
            RoleId = role.Id
        };

        var result = await assignmentRepository.Create(assignment, options: options, cancellationToken: cancellationToken);
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
        var assignmentFilter = assignmentRepository.CreateFilterBuilder();
        assignmentFilter.Equal(t => t.Id, assignmentId);
        var assignmentResult = await assignmentRepository.GetExtended(assignmentFilter, cancellationToken: cancellationToken);

        if (!assignmentResult.Any())
        {
            return result;
        }

        var assignment = assignmentResult.First();

        // Get AssignmentPackages
        QueryResponse<AssignmentPackage> assignmentPackageResult = null;
        var assignmentPackageFilter = assignmentPackageRepository.CreateFilterBuilder();
        assignmentPackageFilter.Equal(t => t.AssignmentId, assignmentId);

        assignmentPackageResult = await assignmentPackageRepository.Get(assignmentPackageFilter, cancellationToken: cancellationToken);

        foreach (var assignmentPackage in assignmentPackageResult)
        {
            result.Add(new AssignmentOrRolePackageAccess { AssignmentId = assignment.Id, RoleId = assignment.RoleId, PackageId = assignmentPackage.PackageId, AssignmentPackageId = assignmentPackage.Id });
        }

        // Get RolePackages
        var rolePackFilter = rolePackageRepository.CreateFilterBuilder();
        rolePackFilter.Equal(t => t.RoleId, assignment.RoleId);
        var rolePackageResult = await rolePackageRepository.Get(rolePackFilter, cancellationToken: cancellationToken);

        foreach (var rolePackage in rolePackageResult)
        {
            result.Add(new AssignmentOrRolePackageAccess { AssignmentId = assignment.Id, RoleId = assignment.RoleId, PackageId = rolePackage.PackageId, RolePackageId = rolePackage.Id });
        }

        return result;
    }

    private static void ValidatePartyIsNotNull(Guid id, ExtEntity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is null)
        {
            errors.Add(ValidationErrors.EntityNotExists, param, [new("partyId", id.ToString())]);
        }
    }

    private static void ValidatePartyIsOrg(ExtEntity entity, ref ValidationErrorBuilder errors, string param)
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
