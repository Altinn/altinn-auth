using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Interface for managing connections.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Gets the connections given by the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>A collection of given connections.</returns>
    Task<IEnumerable<ExtConnection>> GetGiven(Guid id);

    /// <summary>
    /// Gets the connections received by the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>A collection of received connections.</returns>
    Task<IEnumerable<ExtConnection>> GetRecived(Guid id);

    /// <summary>
    /// Gets the connections facilitated by the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>A collection of facilitated connections.</returns>
    Task<IEnumerable<ExtConnection>> GetFacilitated(Guid id);

    /// <summary>
    /// Gets the specific connection between two entities.
    /// </summary>
    /// <param name="fromId">The identifier of the entity giving the connection.</param>
    /// <param name="toId">The identifier of the entity receiving the connection.</param>
    /// <returns>A collection of specific connections.</returns>
    Task<IEnumerable<ExtConnection>> GetSpecific(Guid fromId, Guid toId);

    /// <summary>
    /// Gets the specific connection between two entities.
    /// </summary>
    /// <param name="Id">The identifier of the connection.</param>
    /// <returns>A specific connection.</returns>
    Task<ExtConnection> Get(Guid Id);

    /// <summary>
    /// Gets the packages associated with the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>A collection of packages.</returns>
    Task<IEnumerable<Package>> GetPackages(Guid id);

    /// <summary>
    /// Gets the resources associated with the specified entity.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>A collection of resources.</returns>
    Task<IEnumerable<Resource>> GetResources(Guid id);
}
