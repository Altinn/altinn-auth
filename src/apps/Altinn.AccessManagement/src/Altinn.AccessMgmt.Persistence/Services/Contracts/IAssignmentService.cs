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
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, string roleCode, ChangeRequestOptions options);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Result<Assignment>> GetOrCreateAssignment2(Guid fromEntityId, Guid toEntityId, string roleCode, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, Guid roleId, ChangeRequestOptions options);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddPackageToAssignment(Guid userId, Guid assignmentId, Guid packageId, ChangeRequestOptions options);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddResourceToAssignment(Guid userId, Guid assignmentId, Guid resourceId, ChangeRequestOptions options);

    /// <summary>
    /// Get Assignment
    /// </summary>
    /// <param name="fromId">From Entity Id</param>
    /// <param name="toId">To Entity Id</param>
    /// <param name="roleId">Role Id</param>
    /// <returns></returns>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId);

    /// <summary>
    /// Get Assignment
    /// </summary>
    /// <param name="fromId">From Entity Id</param>
    /// <param name="toId">To Entity Id</param>
    /// <param name="roleCode">Role Code</param>
    /// <returns></returns>
    Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode);

    /// <summary>
    /// Get Inheirited Assignments
    /// </summary>
    /// <param name="fromId">From Entity Id</param>
    /// <param name="toId">To Entity Id</param>
    /// <param name="roleId">Role Id</param>
    /// <returns></returns>
    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, Guid roleId);

    /// <summary>
    /// Get Inheirited Assignments
    /// </summary>
    /// <param name="fromId">From Entity Id</param>
    /// <param name="toId">To Entity Id</param>
    /// <param name="roleCode">Role Code</param>
    /// <returns></returns>
    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, string roleCode);
}
