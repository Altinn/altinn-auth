using Altinn.AccessMgmt.Core.Models;
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
    Task<Result<Assignment>> GetOrCreateAssignment2(Guid fromEntityId, Guid toEntityId, string roleCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, string roleCode);

    /// <summary>
    /// Gets assignment and creates if not exits
    /// </summary>
    /// <returns></returns>
    Task<Assignment> GetOrCreateAssignment(Guid fromId, Guid toId, Guid roleId);

    /// <summary>
    /// Adds a package to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddPackageToAssignment(Guid userId, Guid assignmentId, Guid packageId);

    /// <summary>
    /// Adds a resource to the delegation
    /// </summary>
    /// <returns></returns>
    Task<bool> AddResourceToAssignment(Guid userId, Guid assignmentId, Guid resourceId);

    Task<Assignment> GetAssignment(Guid fromId, Guid toId, Guid roleId);

    Task<Assignment> GetAssignment(Guid fromId, Guid toId, string roleCode);

    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, Guid roleId);

    Task<IEnumerable<InheritedAssignment>> GetInheritedAssignment(Guid fromId, Guid toId, string roleCode);
}
