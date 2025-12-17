using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Assignment service
/// </summary>
public interface IAssignmentService
{
    /// <summary>
    /// Removes packages from the assignment between the two parties.
    /// This method is created for the Altinn 2 import scenario where we need to remove packages from an existing assignment and also revoke the assignment when last package is revoked.
    /// </summary>
    /// <returns></returns>
    Task<int> RevokeImportedAssignmentPackages(Guid fromId, Guid toId, List<string> packageUrns, AuditValues values = null, bool onlyRemoveA2Packages = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add packages to an assignment (creates the assignment if it does not exist) between the two parties.
    /// </summary>
    /// <returns></returns>
    Task<List<Authorization.Api.Contracts.AccessManagement.AssignmentPackageDto>> ImportAssignmentPackages(Guid fromId, Guid toId, List<string> packageUrns, AuditValues values = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exists.
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignmentInternal(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Result<Assignment>> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an assignment of the given role if found between the parties.
    /// </summary>
    /// <returns></returns>
    Task<ProblemInstance> DeleteAssignment(Guid fromId, Guid toId, string roleCode, bool cascade = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// NB: It is the callers responsibility to ensure that the assignment is allowed to be deleted.
    /// Deletes an assignment by the assignment id.
    /// </summary>
    /// <returns></returns>
    Task<ProblemInstance> DeleteAssignment(Guid assignmentId, bool cascade = false, AuditValues audit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether revoking the specified assignment will result in cascading revocations of related assignments. 
    /// If three are cascading effects, these are captured as validation errors in the returned ValidationErrorBuilder.
    /// </summary>
    /// <param name="assignmentId">The unique identifier of the assignment to check for cascading revocation effects.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A ValidationErrorBuilder with any any validation errors representing a cascading revocation due to a dependency. 
    /// If no dependencys are found, the error builder will be empty. meaning a delete can be performed without cascading effects.</returns>
    Task<ValidationErrorBuilder> CheckCascadingAssignmentRevoke(Guid assignmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, Guid roleId, AuditValues audit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddAssignmentPackage(Guid userId, Guid assignmentId, Guid packageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> UpsertAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> UpsertAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, string policyPath, string policyVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> RemoveAssignmentPackage(Guid userId, Guid assignmentId, Guid packageId, CancellationToken cancellationToken = default);
   
    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> RemoveAssignmentResource(Guid userId, Guid assignmentId, Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> RemoveAssignmentInstance(Guid userId, Guid assignmentId, Guid resourceId, string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignment.
    /// </summary>
    Task<Assignment> GetAssignment(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignment.
    /// </summary>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignments.
    /// </summary>
    Task<List<Assignment>> GetFacilitatorAssignments(Guid fromId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignment.
    /// </summary>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all key role assignments for a given to entity.
    /// </summary>
    /// <param name="toId">The to party</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<IEnumerable<Assignment>> GetKeyRoleAssignments(Guid toId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches Client assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<SystemuserClientDto>> GetClients(Guid toId, string[] roles, string[] packages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment packages or role packages for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentOrRolePackageAccess>> GetPackagesForAssignment(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment packages for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentPackage>> GetAssignmentPackages(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment packages or role packages for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentResource>> GetAssignmentResources(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment instances for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentInstance>> GetAssignmentInstances(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment instances for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentInstance>> GetAssignmentInstances(Guid assignmentId, Guid resourceId, CancellationToken cancellationToken = default);
}
