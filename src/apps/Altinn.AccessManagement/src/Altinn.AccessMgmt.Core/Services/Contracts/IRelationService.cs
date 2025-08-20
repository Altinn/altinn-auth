using Altinn.AccessMgmt.Core.Models;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Service for getting connections
/// </summary>
public interface IRelationService
{
    /// <summary>
    /// Get Connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="toId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationPackageDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="fromId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationPackageDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="toId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="fromId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default);
}
