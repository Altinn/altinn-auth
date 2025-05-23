using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Assignment service
/// </summary>
public interface IAssignmentService
{
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
    /// Fetches inherited assignments.
    /// </summary>
    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches inherited assignments.
    /// </summary>
    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches Client assignments.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<ClientDto>> GetClients(Guid toId, string[] roles, string[] packages, CancellationToken cancellationToken = default);
}
