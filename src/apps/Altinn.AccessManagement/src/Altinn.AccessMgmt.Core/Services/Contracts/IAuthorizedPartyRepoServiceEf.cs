using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Interface for managing optimized database queries using entity framework, to be used by authorized parties service.
/// </summary>
public interface IAuthorizedPartyRepoServiceEf
{
    /// <summary>
    /// Gets an entity by its unique identifier
    /// </summary>
    /// <param name="id">The uuid of the entity</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>Single entity</returns>
    Task<Entity?> GetEntity(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Get Entity based on PartyId
    /// </summary>
    /// <param name="partyId">party id</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Single entity</returns>
    Task<Entity?> GetEntityByPartyId(int partyId, CancellationToken ct = default);

    /// <summary>
    /// Get Entity based on Organization Id
    /// </summary>
    /// <param name="organizationId">organization id</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Single entity</returns>
    Task<Entity?> GetEntityByOrganizationId(string organizationId, CancellationToken ct = default);

    /// <summary>
    /// Get Entity based on Person Id
    /// </summary>
    /// <param name="personId">person id</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    Task<Entity?> GetEntityByPersonId(string personId, CancellationToken ct = default);

    /// <summary>
    /// Get Entity based on User Id
    /// </summary>
    /// <param name="userId">userId</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    Task<Entity?> GetEntityByUserId(int userId, CancellationToken ct = default);

    /// <summary>
    /// Get Entity based on Username
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    Task<Entity?> GetEntityByUsername(string username, CancellationToken ct = default);

    /// <summary>
    /// Get all child entities based on a list of parentIds
    /// </summary>
    /// <param name="parentIds">List of parent ids</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of entities</returns>
    Task<IEnumerable<Entity>> GetSubunits(IEnumerable<Guid> parentIds, CancellationToken ct = default);

    /// <summary>
    /// Get all entities based on a list of ids
    /// </summary>
    /// <param name="ids">All party uuids to get</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of entities</returns>
    Task<IEnumerable<Entity>> GetEntities(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Get all entities based on a list of partyIds
    /// </summary>
    /// <param name="partyIds">All party ids to get</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of entities</returns>
    Task<IEnumerable<Entity>> GetEntitiesByPartyIds(IEnumerable<int> partyIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all key role assignments for a given to entity.
    /// </summary>
    /// <param name="toId">The to party</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of keyrole assignments</returns>
    Task<IEnumerable<Assignment>> GetKeyRoleAssignments(Guid toId, CancellationToken ct = default);

    /// <summary>
    /// Get list of packages the to party has access to, on behalf of the from party
    /// </summary>
    /// <returns>Enumerable of package permissions</returns>
    Task<List<ConnectionQueryExtendedRecord>> GetConnectionsFromOthers(Guid toId, AuthorizedPartiesFilters filters = null, CancellationToken ct = default);

    /// <summary>
    /// Get list of packages the to party has access to, on behalf of the from party
    /// </summary>
    /// <returns>Enumerable of package permissions</returns>
    Task<List<ConnectionQueryExtendedRecord>> GetPipConnectionsFromOthers(Guid toId, AuthorizedPartiesFilters filters = null, CancellationToken ct = default);

    /// <summary>
    /// Get resources by provider code and/or resource ids
    /// </summary>
    /// <param name="providerCode">Provider code</param>
    /// <param name="resourceIds">Resource ids</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of resources</returns>
    Task<Dictionary<string, Resource>> GetResourcesByProvider(string? providerCode = null, IEnumerable<string>? resourceIds = null, CancellationToken ct = default);

    /// <summary>
    /// Get role resources by provider code and/or resource ids
    /// </summary>
    /// <param name="providerCode">Provider code</param>
    /// <param name="resourceIds">Resource ids</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of role resources</returns>
    Task<Dictionary<Guid, IEnumerable<RoleResource>>> GetRoleResourcesByProvider(string? providerCode = null, IEnumerable<string>? resourceIds = null, CancellationToken ct = default);

    /// <summary>
    /// Get package resources by provider code and/or resource ids
    /// </summary>
    /// <param name="providerCode">Provider code</param>
    /// <param name="resourceIds">Resource ids</param>
    /// <param name="ct">The <see cref="CancellationToken"/></param>
    /// <returns>Enumerable of package resources</returns>
    Task<Dictionary<Guid, IEnumerable<PackageResource>>> GetPackageResourcesByProvider(string? providerCode = null, IEnumerable<string>? resourceIds = null, CancellationToken ct = default);
}
