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
    
    /// <summary>
    /// Revokes access to an imported assignment resource for a specified from, to and audit info.
    /// </summary>
    /// <param name="fromId">The unique identifier of the entity from which the resource was originally assigned.</param>
    /// <param name="toId">The unique identifier of the entity whose access to the resource is being revoked.</param>
    /// <param name="resourceName">The id of the resource to revoke access to. Cannot be null or empty.</param>
    /// <param name="audit">The audit information to record for this operation. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of resources that were
    /// successfully revoked.</returns>
    Task<int> RevokeImportedAssignmentResource(Guid fromId, Guid toId, string resourceName, AuditValues audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports access to an assignment resource for a specified from, to and audit info.
    /// </summary>
    /// <param name="fromId">The unique identifier of the source assignment from which the resource change is imported.</param>
    /// <param name="toId">The unique identifier of the target assignment to which the resource change is applied.</param>
    /// <param name="resourceName">The id that identifies the resource being changed. Cannot be null or empty.</param>
    /// <param name="blobStoragePolicyPath">The path to the blob storage policy that governs access to the resource data. Cannot be null or empty.</param>
    /// <param name="blobStorageVersionId">The version identifier of the blob storage object to associate with the resource change. Cannot be null or empty.</param>
    /// <param name="delegationEventId">The identifier of the delegation event that triggered this resource change.</param>
    /// <param name="audit">The audit information to record for this operation. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the import operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of resource changes
    /// imported.</returns>
    Task<int> ImportAssignmentResourceChange(Guid fromId, Guid toId, string resourceName, string blobStoragePolicyPath, string blobStorageVersionId, int delegationEventId, AuditValues audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes access to an imported assignment instance for a specified target entity.
    /// </summary>
    /// <param name="fromId">The unique identifier of the entity from which the resource was originally assigned.</param>
    /// <param name="toId">The unique identifier of the entity whose access to the resource is being revoked.</param>
    /// <param name="resourceName">The id of the resource to revoke access to. Cannot be null or empty.</param>
    /// <param name="instanceId">The identifier for the instance the delegation is about</param>
    /// <param name="audit">The audit information to record for this operation. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of resources that were
    /// successfully revoked.</returns>
    Task<int> RevokeImportedInstanceAssignment(Guid fromId, Guid toId, string resourceName, string instanceId, AuditValues audit, CancellationToken cancellationToken = default);

    /// <summary>
    ///  Imports access to an assignment instance for a specified from, to and audit info.
    /// </summary>
    /// <param name="fromId">The unique identifier of the source assignment from which the resource change is imported.</param>
    /// <param name="toId">The unique identifier of the target assignment to which the resource change is applied.</param>
    /// <param name="resourceName">The id that identifies the resource being changed. Cannot be null or empty.</param>
    /// <param name="blobStoragePolicyPath">The path to the blob storage policy that governs access to the resource data. Cannot be null or empty.</param>
    /// <param name="blobStorageVersionId">The version identifier of the blob storage object to associate with the resource change. Cannot be null or empty.</param>
    /// <param name="instanceId">The identifier for the instance the delegation is about</param>
    /// <param name="delegationEventId">The identifier of the delegation event that triggered this resource change.</param>
    /// <param name="audit">The audit information to record for this operation. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the import operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of resource changes
    /// imported.</returns>
    Task<int> ImportInstanceAssignmentChange(Guid fromId, Guid toId, string resourceName, string blobStoragePolicyPath, string blobStorageVersionId, string instanceId, int delegationEventId, AuditValues audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears assignments for a dead person.
    /// </summary>
    /// <returns></returns>
    Task ClearAssignmentsInAfterLife(Guid deadPerson, AuditValues audit = null, CancellationToken cancellationToken = default);
}
