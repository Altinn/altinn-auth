using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Enduser.Services;

/// <summary>
/// Service for managing connections.
/// </summary>
public class EnduserConnectionService(
    IDbAudit DbAudit,
    IPackageRepository PackageRepository,
    IRoleRepository RoleRepository,
    IConnectionPackageRepository ConnectionPackageRepository,
    IAssignmentRepository AssignmentRepository,
    IDelegationRepository DelegationRepository,
    IAssignmentPackageRepository AssignmentPackageRepository,
    IEntityRepository EntityRepository,
    IRelationService RelationService
    ) : IEnduserConnectionService
{
    /// <inheritdoc />
    public async Task<Result<List<CompactRelationDto>>> Get(Guid? fromId = null, Guid? toId = null, CancellationToken cancellationToken = default)
    {
        if (fromId is { } && fromId.Value != Guid.Empty)
        {
            var result = await RelationService.GetConnectionsToOthers(fromId.Value, toId is { } && toId != Guid.Empty ? toId : null, null, cancellationToken: cancellationToken);
            return result.ToList();
        }

        if (toId is { } && toId != Guid.Empty)
        {
            var result = await RelationService.GetConnectionsFromOthers(toId.Value, fromId is { } && fromId.Value != Guid.Empty ? fromId : null, null, cancellationToken: cancellationToken);
            return result.ToList();
        }

        Unreachable();
        return default;
    }

    /// <inheritdoc />
    public async Task<Result<Assignment>> AddAssignment(Guid fromId, Guid toId, string role, CancellationToken cancellationToken = default)
    {
        var dependencies = await GetAssignmentDependencies(fromId, toId, role, cancellationToken);
        if (ValidateAssignmentData(dependencies.EntityFrom, dependencies.EntityTo, dependencies.Roles) is var problem && problem is { })
        {
            return problem;
        }

        var roleId = dependencies.Roles.First().Id;
        var filter = AssignmentRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, fromId)
            .Equal(t => t.ToId, toId)
            .Equal(t => t.RoleId, roleId);

        var existingAssignment = await AssignmentRepository.Get(filter, callerName: SpanName("Get existing assignments"), cancellationToken: cancellationToken);
        if (existingAssignment != null && existingAssignment.Any())
        {
            return existingAssignment.First();
        }

        var assignment = new Assignment()
        {
            FromId = fromId,
            ToId = toId,
            RoleId = roleId,
        };

        var result = await AssignmentRepository.Create(assignment, DbAudit.Value, callerName: SpanName("Create assignment"), cancellationToken: cancellationToken);
        if (result == 0)
        {
            Unreachable();
        }

        return assignment;
    }

    /// <inheritdoc />
    public async Task<ValidationProblemInstance> RemoveAssignment(Guid fromId, Guid toId, string role, bool cascade, CancellationToken cancellationToken = default)
    {
        var dependencies = await GetAssignmentDependencies(fromId, toId, role, cancellationToken);
        if (ValidateAssignmentData(dependencies.EntityFrom, dependencies.EntityTo, dependencies.Roles) is var problem && problem is { })
        {
            return problem;
        }

        var roleId = dependencies.Roles.First().Id;
        var filter = AssignmentRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, fromId)
            .Equal(t => t.ToId, toId)
            .Equal(t => t.RoleId, roleId);

        var existingAssignments = await AssignmentRepository.Get(filter, callerName: SpanName("Get existing assignments"), cancellationToken: cancellationToken);
        if (existingAssignments != null && !existingAssignments.Any())
        {
            return null;
        }

        var existingAssignment = existingAssignments.First();

        if (!cascade)
        {
            var packages = await AssignmentPackageRepository.Get(f => f.AssignmentId, existingAssignment.Id, callerName: SpanName("Get existing package assignments"), cancellationToken: cancellationToken);
            var delegationsFrom = await DelegationRepository.Get(f => f.FromId, existingAssignment.Id, callerName: SpanName("Get existing package delegations (From)"), cancellationToken: cancellationToken);
            var delegationsTo = await DelegationRepository.Get(f => f.ToId, existingAssignment.Id, callerName: SpanName("Get existing package delegations (To)"), cancellationToken: cancellationToken);

            problem = EnduserValidationRules.Validate(
                EnduserValidationRules.QueryParameters.HasPackagesAssigned(packages),
                EnduserValidationRules.QueryParameters.HasDelegationsAssigned(delegationsFrom),
                EnduserValidationRules.QueryParameters.HasDelegationsAssigned(delegationsTo)
            );
            if (problem is { })
            {
                return problem;
            }
        }

        var result = await AssignmentRepository.Delete(existingAssignment.Id, DbAudit.Value, callerName: SpanName("Delete assignment"), cancellationToken: cancellationToken);
        if (result == 0)
        {
            Unreachable();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<Result<List<PackagePermission>>> GetPackages(Guid? fromId, Guid? toId, CancellationToken cancellationToken = default)
    {
        if (fromId is { } && fromId.Value != Guid.Empty)
        {
            var result = await RelationService.GetPackagePermissionsToOthers(fromId.Value, toId is { } && toId != Guid.Empty ? toId : null, null, cancellationToken);
            return result.ToList();
        }

        if (toId is { } && toId.Value != Guid.Empty)
        {
            var result = await RelationService.GetPackagePermissionsFromOthers(toId.Value, fromId is { } && fromId.Value != Guid.Empty ? fromId : null, null, cancellationToken);
            return result.ToList();
        }

        Unreachable();
        return default;
    }

    /// <inheritdoc />
    public async Task<Result<AssignmentPackage>> AddPackage(Guid fromId, Guid toId, string role, string package, CancellationToken cancellationToken = default)
    {
        package = (package.StartsWith("urn:", StringComparison.Ordinal) || package.StartsWith(':')) ? package : ":" + package;

        var filter = PackageRepository.CreateFilterBuilder()
            .Add(t => t.Urn, package, FilterComparer.EndsWith);

        var packages = await PackageRepository.Get(filter, callerName: SpanName("Get packages using URN"), cancellationToken: cancellationToken);
        var problem = EnduserValidationRules.Validate(EnduserValidationRules.QueryParameters.PackageUrnLookup(packages, package));
        if (problem is { })
        {
            return problem;
        }

        var packageResult = packages.First();
        return await AddPackage(fromId, toId, role, packageResult.Id, "package", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<AssignmentPackage>> AddPackage(Guid fromId, Guid toId, string role, Guid packageId, CancellationToken cancellationToken = default)
    {
        return await AddPackage(fromId, toId, role, packageId, "packageId", cancellationToken);
    }

    private async Task<Result<AssignmentPackage>> AddPackage(Guid fromId, Guid toId, string role, Guid packageId, string queryParamName, CancellationToken cancellationToken)
    {
        var dependencies = await GetAssignmentDependencies(fromId, toId, role, cancellationToken);
        if (ValidateAssignmentData(dependencies.EntityFrom, dependencies.EntityTo, dependencies.Roles) is var problem && problem is { })
        {
            return problem;
        }

        var roleId = dependencies.Roles.First().Id;
        var filter = AssignmentRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, fromId)
            .Equal(t => t.ToId, toId)
            .Equal(t => t.RoleId, roleId);

        var existingAssignments = await AssignmentRepository.Get(filter, callerName: SpanName("Get existing assignments"), cancellationToken: cancellationToken);
        problem = EnduserValidationRules.Validate(EnduserValidationRules.QueryParameters.VerifyAssignmentRoleExists(existingAssignments, role));
        Assignment assignment;
        if (problem is { })
        {
            var relationsResult = await Get(fromId, toId, cancellationToken: cancellationToken);
            if (relationsResult is { } && relationsResult.Value is { } && relationsResult.Value.Any())
            {
                var assignmentResult = await AddAssignment(fromId, toId, "rettighetshaver", cancellationToken);
                assignment = assignmentResult.Value;
            }
            else
            {
                return problem;
            }
        }
        else
        {
            assignment = existingAssignments.First();
        }

        var check = await CheckPackage(fromId, packageIds: [packageId], cancellationToken);
        if (check.IsProblem)
        {
            return check.Problem;
        }

        problem = EnduserValidationRules.Validate(
            EnduserValidationRules.QueryParameters.AuthorizePackageAssignment(check.Value),
            EnduserValidationRules.QueryParameters.PackageIsAssignableToRecipient(check.Value.Select(p => p.Package.Urn), dependencies.EntityTo, queryParamName)
        );

        if (problem is { })
        {
            return problem;
        }

        var assignmentPackageFilter = AssignmentPackageRepository
            .CreateFilterBuilder()
            .Equal(t => t.AssignmentId, assignment.Id)
            .Equal(t => t.PackageId, packageId);

        var existingPackageAssignment = await AssignmentPackageRepository.Get(assignmentPackageFilter, callerName: SpanName("Get existing package assignments"), cancellationToken: cancellationToken);
        if (existingPackageAssignment != null && existingPackageAssignment.Any())
        {
            return existingPackageAssignment.First();
        }

        var createResult = await AssignmentPackageRepository.Create(new AssignmentPackage() { AssignmentId = assignment.Id, PackageId = packageId }, DbAudit.Value, callerName: SpanName("Create assignment package"), cancellationToken: cancellationToken);
        if (createResult == 0)
        {
            Unreachable();
        }

        var createCheckResult = await AssignmentPackageRepository.Get(assignmentPackageFilter, callerName: SpanName("Get assignment packages"), cancellationToken: cancellationToken);
        if (createCheckResult == null || !createCheckResult.Any())
        {
            Unreachable();
        }

        return createCheckResult.First();
    }

    /// <inheritdoc/>
    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string role, string package, CancellationToken cancellationToken = default)
    {
        package = (package.StartsWith("urn:", StringComparison.Ordinal) || package.StartsWith(":", StringComparison.Ordinal)) ? package : ":" + package;

        var filter = PackageRepository.CreateFilterBuilder()
            .Add(t => t.Urn, package, FilterComparer.EndsWith);

        var packages = await PackageRepository.Get(filter, callerName: SpanName("Get packages using URN"), cancellationToken: cancellationToken);
        var problem = EnduserValidationRules.Validate(EnduserValidationRules.QueryParameters.PackageUrnLookup(packages, package));
        if (problem is { })
        {
            return problem;
        }

        var packageResult = packages.First();
        return await RemovePackage(fromId, toId, role, packageResult.Id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string role, Guid packageId, CancellationToken cancellationToken = default)
    {
        var roleResult = await RoleRepository.Get(t => t.Code, role, callerName: SpanName("Get roles using role code"), cancellationToken: cancellationToken);
        if (roleResult == null || !roleResult.Any())
        {
            return null;
        }

        var assignmentFilter = AssignmentRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, fromId)
            .Equal(t => t.ToId, toId)
            .Equal(t => t.RoleId, roleResult.First().Id);

        var result = await AssignmentRepository.Get(assignmentFilter, callerName: SpanName("Get assignments"), cancellationToken: cancellationToken);
        if (result == null || !result.Any())
        {
            return null;
        }

        var assignment = result.First();

        var connectionPackageFilter = ConnectionPackageRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, fromId)
            .Equal(t => t.ToId, toId)
            .Equal(t => t.PackageId, packageId);

        var userPackages = await ConnectionPackageRepository.GetExtended(connectionPackageFilter, callerName: SpanName("Get extended connection packages for calling user"), cancellationToken: cancellationToken);

        if (!userPackages.Any())
        {
            return null;
        }

        var packageFilter = AssignmentPackageRepository.CreateFilterBuilder()
            .Equal(t => t.AssignmentId, assignment.Id)
            .Equal(t => t.PackageId, packageId);

        var checkResult = await AssignmentPackageRepository.Get(packageFilter, callerName: SpanName("Get assigmment package"), cancellationToken: cancellationToken);
        if (checkResult == null && !checkResult.Any())
        {
            return null;
        }

        var deleteResult = await AssignmentPackageRepository.Delete(packageFilter, DbAudit.Value, callerName: SpanName("Delete assignment package"), cancellationToken: cancellationToken);
        if (deleteResult == 0)
        {
            return null;
        }

        return null;
    }

    public async Task<Result<IEnumerable<PackageDelegationCheckDto>>> CheckPackage(Guid party, IEnumerable<string> packages, IEnumerable<Guid> packageIds = null, CancellationToken cancellationToken = default)
    {
        packages = packages.Select(p =>
            p.StartsWith("urn:", StringComparison.Ordinal)
                ? p
                : (p.StartsWith(":", StringComparison.Ordinal)
                    ? $"urn:altinn:accesspackage{p}"
                    : $"urn:altinn:accesspackage:{p}"));

        var filter = PackageRepository.CreateFilterBuilder()
            .In(t => t.Urn, packages);

        var allPackages = await PackageRepository.Get(filter, callerName: SpanName("Get packages using URNs"), cancellationToken: cancellationToken);
        var problem = EnduserValidationRules.Validate(EnduserValidationRules.QueryParameters.PackageUrnLookup(allPackages, packages));
        if (problem is { })
        {
            return problem;
        }

        return await CheckPackage(party, (List<Guid>)[.. packageIds, .. allPackages.Select(p => p.Id)], cancellationToken);
    }

    public async Task<Result<IEnumerable<PackageDelegationCheckDto>>> CheckPackage(Guid party, IEnumerable<Guid>? packageIds = null, CancellationToken cancellationToken = default)
    {
        var assignablePackages = await RelationService.GetAssignablePackagePermissions(
            DbAudit.Value.ChangedBy,
            party,
            packageIds,
            cancellationToken: cancellationToken);

        return assignablePackages.GroupBy(p => p.Package.Id).Select(group =>
        {
            var firstPackage = group.First();
            return new PackageDelegationCheckDto
            {
                Package = new CompactPackageDto
                {
                    Id = firstPackage.Package.Id,
                    Urn = firstPackage.Package.Urn,
                    AreaId = firstPackage.Package.AreaId
                },
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new PackageDelegationCheckReasonDto
                {
                    Description = p.Reason.Description,
                    RoleId = p.Reason.RoleId,
                    RoleUrn = p.Reason.RoleUrn,
                    FromId = p.Reason.FromId,
                    FromName = p.Reason.FromName,
                    ToId = p.Reason.ToId,
                    ToName = p.Reason.ToName,
                    ViaId = p.Reason.ViaId,
                    ViaName = p.Reason.ViaName,
                    ViaRoleId = p.Reason.ViaRoleId,
                    ViaRoleUrn = p.Reason.ViaRoleUrn
                })
            };
        }).ToList();
    }

    private ValidationProblemInstance? ValidateAssignmentData(ExtEntity entityFrom, ExtEntity entityTo, IEnumerable<Role> roles)
    {
        var problem = EnduserValidationRules.Validate(
            EnduserValidationRules.QueryParameters.PartyExists(entityFrom, "from"),
            EnduserValidationRules.QueryParameters.PartyExists(entityTo, "to"),
            EnduserValidationRules.QueryParameters.PartyIsEntityType(entityFrom, "Organisasjon", "from"),
            EnduserValidationRules.QueryParameters.PartyIsEntityType(entityTo, "Organisasjon", "to")
        );

        if (problem is { })
        {
            return problem;
        }

        if (roles is null || !roles.Any())
        {
            Unreachable();
        }

        return null;
    }

    private async Task<(ExtEntity EntityFrom, ExtEntity EntityTo, IEnumerable<Role> Roles)> GetAssignmentDependencies(Guid from, Guid to, string roleCode, CancellationToken cancellationToken)
    {
        var entityFromTask = EntityRepository.GetExtended(from, callerName: SpanName("Get extended entities (From)"), cancellationToken: cancellationToken);
        var entityToTask = EntityRepository.GetExtended(to, callerName: SpanName("Get extended entities (To)"), cancellationToken: cancellationToken);
        var roleTask = RoleRepository.Get(t => t.Code, roleCode, callerName: SpanName("Get roles using role code"), cancellationToken: cancellationToken);
        return (await entityFromTask, await entityToTask, await roleTask);
    }

    [DoesNotReturn]
    private void Unreachable() =>
        throw new UnreachableException();

    private static string SpanName(string spanName) =>
        $"{nameof(EnduserConnectionService)}: {spanName}";
}

/// <summary>
/// Interface for managing connections.
/// </summary>
public interface IEnduserConnectionService
{
    /// <summary>
    /// Retrieves a list of external connections, optionally filtered by origin and/or destination entity IDs.
    /// </summary>
    /// <param name="fromId">ID of the originating entity to filter connections by.</param>
    /// <param name="toId">ID of the target entity to filter connections by.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="ExtConnection"/> instances matching the criteria.
    /// </returns>
    Task<Result<List<CompactRelationDto>>> Get(Guid? fromId = null, Guid? toId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a role assignment between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="role">Name of the role to assign.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the newly created <see cref="Assignment"/>.
    /// </returns>
    Task<Result<Assignment>> AddAssignment(Guid fromId, Guid toId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific role assignment between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="role">Name of the role to remove.</param>
    /// <param name="cascade">If <c>false</c>, stop if there are any dependent records.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemoveAssignment(Guid fromId, Guid toId, string role, bool cascade, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of connection packages, optionally filtered by origin and/or destination entity IDs.
    /// </summary>
    /// <param name="fromId">ID of the originating entity to filter packages by.</param>
    /// <param name="toId">ID of the target entity to filter packages by.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a list of <see cref="ConnectionPackage"/> instances.
    /// </returns>
    Task<Result<List<PackagePermission>>> GetPackages(Guid? fromId, Guid? toId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to an assignment (by package ID) based on the role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="role">Name of the role assigned.</param>
    /// <param name="packageId">Unique identifier of the package to assign.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created <see cref="AssignmentPackage"/>.
    /// </returns>
    Task<Result<AssignmentPackage>> AddPackage(Guid fromId, Guid toId, string role, Guid packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to an assignment (by package name or code) based on the role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="role">Name of the role assigned.</param>
    /// <param name="package">Urn value of the package to assign.</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created <see cref="AssignmentPackage"/>.
    /// </returns>
    Task<Result<AssignmentPackage>> AddPackage(Guid fromId, Guid toId, string role, string package, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a package (by package ID) from assignment based on a specific role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="role">Name of the role from which the package is removed.</param>
    /// <param name="packageId">Unique identifier of the package to remove.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string role, Guid packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a package (by package name or code) from assignment based on a specific role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="role">Name of the role from which the package is removed.</param>
    /// <param name="package">Urn value of the package to remove.</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string role, string package, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an authpenticated user is an access manager and has the necessary permissions to delegate a specific access package.
    /// </summary>
    /// <param name="party">ID of the person.</param>
    /// <param name="packageIds">Filter param using unique package identifiers.</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<Result<IEnumerable<PackageDelegationCheckDto>>> CheckPackage(Guid party, IEnumerable<Guid> packageIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an authpenticated user is an access manager and has the necessary permissions to delegate a specific access package.
    /// </summary>
    /// <param name="party">ID of the person.</param>
    /// <param name="packages">Filter param using urn package identifiers.</param>
    /// <param name="packageIds">Filter param using unique package identifiers.</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<Result<IEnumerable<PackageDelegationCheckDto>>> CheckPackage(Guid party, IEnumerable<string> packages, IEnumerable<Guid> packageIds = null, CancellationToken cancellationToken = default);
}
