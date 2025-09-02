using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Assignment service
/// </summary>
public interface IAssignmentService
{
    /// <summary>
    /// Imports administrative packages to an existing assignment or creates a new assignment if non exists.
    /// </summary>
    /// <param name="toUuid">The unique identifier of the user getting the packages that are being added</param>
    /// <param name="fromUuid">The unique identifier of the party from whom the packages are for</param>
    /// <param name="packages">A collection of package identifiers to be imported.</param>
    /// <param name="options">information on the change time and who</param>
    /// <param name="cancellationToken">A token holding any cancelation requests</param>
    /// <returns>returns the assignment the packages are added to.</returns>
    Task<int> ImportAdminAssignmentPackages(Guid toUuid, Guid fromUuid, IEnumerable<string> packages, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes administrative packages from a an assignment.
    /// </summary>
    /// <param name="toUuid">The unique identifier of the user having the packages that are being revoked.</param>
    /// <param name="fromUuid">The unique identifier of the party from whom the packages are being revoked.</param>
    /// <param name="packages">A collection of package urns to be revoked.</param>
    /// <param name="options">information on the change time and who</param>
    /// <param name="cancellationToken">A token holding any cancelation requests</param>
    /// <returns>The assignment updated</returns>
    Task<int> RevokeAdminAssignmentPackages(Guid toUuid, Guid fromUuid, IEnumerable<string> packages, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exists.
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignmentInternal(Guid fromId, Guid toId, string roleCode, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Result<Assignment>> GetOrCreateAssignment(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<ProblemInstance> DeleteAssignment(Guid fromId, Guid toId, string roleCode, ChangeRequestOptions options, bool cascade = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, Guid roleId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddPackageToAssignment(Guid userId, Guid assignmentId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddResourceToAssignment(Guid userId, Guid assignmentId, Guid resourceId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignment.
    /// </summary>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches assignment.
    /// </summary>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches Client assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ClientDto>> GetClients(Guid toId, string[] roles, string[] packages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches all assignment packages or role packages for a given assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<AssignmentOrRolePackageAccess>> GetPackagesForAssignment(Guid assignmentId, CancellationToken cancellationToken = default);
}
