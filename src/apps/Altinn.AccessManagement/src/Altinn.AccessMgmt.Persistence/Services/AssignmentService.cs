using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class AssignmentService(
    IInheritedAssignmentRepository inheritedAssignmentRepository,
    IAssignmentRepository assignmentRepository,
    IPackageRepository packageRepository,
    IAssignmentPackageRepository assignmentPackageRepository,
    IRoleRepository roleRepository,
    IRolePackageRepository rolePackageRepository,
    IEntityRepository entityRepository,
    IDelegationRepository delegationRepository
    ) : IAssignmentService
{
    private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
    private readonly IInheritedAssignmentRepository inheritedAssignmentRepository = inheritedAssignmentRepository;
    private readonly IPackageRepository packageRepository = packageRepository;
    private readonly IAssignmentPackageRepository assignmentPackageRepository = assignmentPackageRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IRolePackageRepository rolePackageRepository = rolePackageRepository;
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IDelegationRepository delegationRepository = delegationRepository;

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
    public async Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode)
    {
        var roleResult = await roleRepository.Get(t => t.Code, roleCode);
        if (roleResult == null || !roleResult.Any())
        {
            return null;
        }

        return await GetAssignment(fromId, toId, roleResult.First().Id);
    }

    /// <inheritdoc/>
    public async Task<bool> AddPackageToAssignment(Guid userId, Guid assignmentId, Guid packageId, ChangeRequestOptions options)
    {
        /*
        [X] Check if user is TS
        [X] Check if user assignment.roles has packages
        [X] Check if user assignment.assignmentpackages has package
        [?] Check if users has packages delegated?

        [ ] Check if package can be delegated
        */

        var user = await entityRepository.Get(userId);

        var assignment = await assignmentRepository.Get(assignmentId);

        /* TODO: Future Sjekk om bruker er Tilgangsstyrer */

        var package = await packageRepository.Get(packageId);

        var userAssignmentFilter = assignmentRepository.CreateFilterBuilder();
        userAssignmentFilter.Equal(t => t.FromId, assignment.FromId);
        userAssignmentFilter.Equal(t => t.ToId, userId);
        var userAssignments = await assignmentRepository.Get(userAssignmentFilter);

        bool hasPackage = false;

        foreach (var userAssignment in userAssignments)
        {
            var assignmentPackages = await assignmentPackageRepository.GetB(userAssignment.Id);
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
                var rolePackResult = await rolePackageRepository.Get(t => t.RoleId, roleId);
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
            options: options
        );

        return true;
    }

    /// <inheritdoc/>
    public Task<bool> AddResourceToAssignment(Guid userId, Guid assignmentId, Guid resourceId, ChangeRequestOptions options)
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
        var existingAssignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken);
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
                    errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("packages", string.Join(",", packages.Select(p => p.Id.ToString())))]);
                }

                var delegations = await delegationRepository.Get(f => f.FromId, existingAssignment.Id, cancellationToken: cancellationToken);
                if (delegations != null && delegations.Any())
                {
                    errors.Add(ValidationErrors.AssignmentIsActiveInOneOrMoreDelegations, "$QUERY/cascade", [new("delegations", string.Join(",", delegations.Select(p => p.Id.ToString())))]);
                }
            }
        }

        if (errors.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        var result = await assignmentRepository.Delete(existingAssignment.Id, options, cancellationToken);
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
        var existingAssignment = await GetAssignment(fromEntityId, toEntityId, roleId, cancellationToken);
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

        var result = await assignmentRepository.Create(assignment, options: options, cancellationToken);
        if (result == 0)
        {
            Unreachable();
        }

        return assignment;
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options)
    {
        var roleResult = await roleRepository.Get(t => t.Name, roleCode);
        if (roleResult == null || !roleResult.Any())
        {
            throw new Exception(string.Format("Role '{0}' not found", roleCode));
        }

        return await GetOrCreateAssignment(fromEntityId, toEntityId, roleResult.First().Id, options: options);
    }

    /// <inheritdoc/>
    public async Task<Assignment> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, Guid roleId, ChangeRequestOptions options)
    {
        var assignment = await GetAssignment(fromEntityId, toEntityId, roleId);
        if (assignment != null)
        {
            return assignment;
        }

        var role = await roleRepository.Get(roleId);
        if (role == null)
        {
            throw new Exception(string.Format("Role '{0}' not found", roleId));
        }

        var inheritedAssignments = await GetInheritedAssignment(fromEntityId, toEntityId, role.Id);
        if (inheritedAssignments != null && inheritedAssignments.Any())
        {
            if (inheritedAssignments.Count() == 1)
            {
                throw new Exception(string.Format("An inheirited assignment exists From:'{0}.FromName' Via:'{0}.ViaName' To:'{}.ToName'. Use Force = true to create anyway.", inheritedAssignments.First()));
            }

            throw new Exception(string.Format("Multiple inheirited assignment exists. Use Force = true to create anyway."));
        }

        await assignmentRepository.Create(
            new Assignment()
            {
                FromId = fromEntityId,
                ToId = toEntityId,
                RoleId = role.Id
            },
            options: options
        );

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, Guid roleId)
    {
        var filter = inheritedAssignmentRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.RoleId, roleId);

        return await inheritedAssignmentRepository.Get(filter);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, string roleCode)
    {
        var roleResult = await roleRepository.Get(t => t.Code, roleCode);
        if (roleResult == null || !roleResult.Any())
        {
            throw new Exception(string.Format("Role not found '{0}'", roleCode));
        }

        var roleId = roleResult.First().Id;
        return await GetInheritedAssignment(fromId, toId, roleId);
    }

    private static void ValidatePartyIsNotNull(Guid id, ExtEntity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is null)
        {
            errors.Add(ValidationErrors.MissingPartyInDb, param, [new("partyId", id.ToString())]);
        }
    }

    private static void ValidatePartyIsOrg(ExtEntity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is not null && !entity.Type.Name.Equals("Organisasjon", StringComparison.InvariantCultureIgnoreCase))
        {
            errors.Add(ValidationErrors.InvalidPartyType, param, [new("partyId", $"expected party of type 'Organisasjon' got '{entity.Type.Name}'.")]);
        }
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
