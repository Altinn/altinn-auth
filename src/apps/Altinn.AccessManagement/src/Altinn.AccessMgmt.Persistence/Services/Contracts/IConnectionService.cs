using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Interface for managing connections.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Gets the connections facilitated by the specified entity.
    /// </summary>
    /// <param name="fromId">The identifier of the entity, access has been provided from.</param>
    /// <param name="toId">The identifier of the entity access has been provided to.</param>
    /// <param name="facilitatorId">The identifier of the entity, access has been facilitated by.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of facilitated connections.</returns>
    Task<IEnumerable<ExtConnection>> Get(Guid? fromId, Guid? toId, Guid? facilitatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connections given to the specified entity (Assignment.To).
    /// </summary>
    /// <param name="toId">The identifier of the entity access has been provided to.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of given connections.</returns>
    Task<IEnumerable<ExtConnection>> GetGiven(Guid toId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connections received from the specified entity (Assignment.From).
    /// </summary>
    /// <param name="fromId">The identifier of the entity, access has been provided from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of received connections.</returns>
    Task<IEnumerable<ExtConnection>> GetReceived(Guid fromId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connections facilitated by the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of facilitated connections.</returns>
    Task<IEnumerable<ExtConnection>> GetFacilitated(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the specific connection between two entities.
    /// </summary>
    /// <param name="Id">The identifier of the connection.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A specific connection.</returns>
    Task<ExtConnection> Get(Guid Id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the packages associated with the specified entity.
    /// </summary>
    /// <param name="fromId">Assign from entity</param>
    /// <param name="toId">Assign to entity</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of packages.</returns>
    Task<IEnumerable<ConnectionPackage>> GetPackages(Guid? fromId, Guid? toId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add package to connection (Assignment or Delegation)
    /// </summary>
    /// <param name="connectionId">Connection identifierr</param>
    /// <param name="packageId">Package identifier</param>
    /// <param name="options">Change request options</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<bool> AddPackage(Guid connectionId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add package to assignment matching from+to+role
    /// Creates assignment if not exists
    /// </summary>
    /// <param name="fromId">Assign from entity</param>
    /// <param name="toId">Assign to entity</param>
    /// <param name="roleCode">Assignment role</param>
    /// <param name="packageId">Package to add to assignment</param>
    /// <param name="options">Change request options (audit)</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<bool> AddPackage(Guid fromId, Guid toId, string roleCode, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add package to assignment matching from+to+role
    /// Creates assignment if not exists
    /// </summary>
    /// <param name="fromId">Assign from entity</param>
    /// <param name="toId">Assign to entity</param>
    /// <param name="roleCode">Assignment role</param>
    /// <param name="packageUrn">Package to add to assignment</param>
    /// <param name="options">Change request options (audit)</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<bool> AddPackage(Guid fromId, Guid toId, string roleCode, string packageUrn, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove package from connection (Assignment or Delegation)
    /// </summary>
    /// <param name="connectionId">Connection identifierr</param>
    /// <param name="packageId">Package identifier</param>
    /// <param name="options">Change request options</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<bool> RemovePackage(Guid connectionId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove package from assignment matching from+to+role
    /// Returns true if assignment is null, assignmentPackage is null or when able to delete assignmentPackage
    /// </summary>
    /// <param name="fromId">Assign from entity</param>
    /// <param name="toId">Assign to entity</param>
    /// <param name="roleCode">Assignment role</param>
    /// <param name="packageId">Package to remove from assignment</param>
    /// <param name="options">Change request options (audit)</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns></returns>
    Task<bool> RemovePackage(Guid fromId, Guid toId, string roleCode, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the resources associated with the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of resources.</returns>
    Task<IEnumerable<Resource>> GetResources(Guid id, CancellationToken cancellationToken = default);
}
