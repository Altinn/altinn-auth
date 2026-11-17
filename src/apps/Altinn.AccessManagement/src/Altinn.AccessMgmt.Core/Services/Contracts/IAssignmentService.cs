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
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, Guid roleId, AuditValues audit = null, CancellationToken cancellationToken = default);

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
    /// Clears assignments for a dead person.
    /// </summary>
    /// <returns></returns>
    Task ClearAssignmentsInAfterLife(Guid deadPerson, AuditValues audit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all assignments given to a system user that has been deleted.
    /// </summary>
    /// <param name="systemUserId">The uuid of the deleted system user.</param>
    /// <param name="audit">Audit values used when saving the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of assignments removed.</returns>
    Task<int> ClearAssignmentsForDeletedSystemUser(Guid systemUserId, AuditValues audit = null, CancellationToken cancellationToken = default);
}
