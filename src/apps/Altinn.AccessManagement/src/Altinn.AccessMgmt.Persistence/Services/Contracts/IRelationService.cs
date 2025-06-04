using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

/// <summary>
/// Service for getting connections
/// </summary>
public interface IRelationService
{
    /// <summary>
    /// Get Connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsFrom(Guid partyId, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="packageId">Filter for package</param>
    /// <param name="resourceId">Filter for resource</param>
    /// <returns></returns>
    Task<IEnumerable<RelationDto>> GetConnectionsTo(Guid partyId, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null);

    /// <summary>
    /// Get connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="roleId">Filter for role</param>
    /// <returns></returns>
    Task<IEnumerable<CompactRelationDto>> GetConnectionsFrom(Guid partyId, Guid? roleId = null);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="roleId">Filter for role</param>\
    /// <returns></returns>
    Task<IEnumerable<CompactRelationDto>> GetConnectionsTo(Guid partyId, Guid? roleId = null);

    /// <summary>
    /// List entities that has this package from given party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="packageId">Filter for package</param>
    /// <returns></returns>
    Task<IEnumerable<ConnectionPermission>> GetPackagePermissionsFrom(Guid partyId, Guid packageId);

    /// <summary>
    /// List entities that the party has this package at
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="packageId">Filter for package</param>
    /// <returns></returns>
    Task<IEnumerable<ConnectionPermission>> GetPackagePermissionsTo(Guid partyId, Guid packageId);

    /// <summary>
    /// Get packages the party has given
    /// </summary>
    /// <param name="partyId">Party</param>
    /// <param name="toId">Given to this party</param>
    /// <param name="packageId">Optional filter for single package</param>
    Task<IEnumerable<CompactPackage>> GetPackagesFrom(Guid partyId, Guid? toId = null, Guid? packageId = null);

    /// <summary>
    /// Get packages the party has received
    /// </summary>
    /// <param name="partyId">Party</param>
    /// <param name="fromId">Given from this party</param>
    /// <param name="packageId">Optional filter for single package</param>
    Task<IEnumerable<CompactPackage>> GetPackagesTo(Guid partyId, Guid? fromId = null, Guid? packageId = null);

    /// <summary>
    /// Get packages the party has given
    /// </summary>
    /// <param name="partyId">Party</param>
    /// <param name="toId">Given to this party</param>
    /// <param name="resourceId">Optional filter for single resource</param>
    Task<IEnumerable<CompactResource>> GetResourcesFrom(Guid partyId, Guid? toId = null, Guid? resourceId = null);

    /// <summary>
    /// Get resources the party has received
    /// </summary>
    /// <param name="partyId">Party</param>
    /// <param name="fromId">Given from this party</param>
    /// <param name="resourceId">Optional filter for single resource</param>
    Task<IEnumerable<CompactResource>> GetResourcesTo(Guid partyId, Guid? fromId = null, Guid? resourceId = null);
}
