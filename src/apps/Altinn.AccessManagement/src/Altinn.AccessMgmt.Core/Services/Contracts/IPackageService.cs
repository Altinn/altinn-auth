using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Defines the contract for managing package-related operations, including search, retrieval,
/// and hierarchical organization of areas and groups.
/// </summary>
public interface IPackageService
{
    /// <summary>
    /// Searches for packages based on a search term.
    /// </summary>
    /// <param name="term">The search term to filter packages.</param>
    /// <param name="resourceProviderCodes">Resource.Provider.Code (brreg, digdir, skatt)</param>
    /// <param name="searchInResources">Indicate if term should filter on resource values</param>
    /// <param name="typeId">Filter for entityType</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A list of search results containing packages that match the term.</returns>
    Task<IEnumerable<SearchObject<PackageDto>>> Search(string term, List<string> resourceProviderCodes = null, bool searchInResources = false, Guid? typeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the hierarchical structure of area groups.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of area group DTOs representing the hierarchy.</returns>
    Task<IEnumerable<AreaGroupDto>> GetHierarchy(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all area groups.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A list of area groups.</returns>
    Task<IEnumerable<AreaGroupDto>> GetAreaGroups(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific area group by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the area group.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The area group with the specified ID.</returns>
    Task<AreaGroupDto> GetAreaGroup(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all areas within a specified area group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the area group.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of areas associated with the specified group.</returns>
    Task<IEnumerable<AreaDto>> GetAreas(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific area by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the area.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The area with the specified ID.</returns>
    Task<AreaDto> GetArea(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all packages associated with a specific area.
    /// </summary>
    /// <param name="areaId">The unique identifier of the area.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of packages belonging to the specified area.</returns>
    Task<IEnumerable<PackageDto>> GetPackagesByArea(Guid areaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific package by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the package.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The package with the specified ID.</returns>
    Task<PackageDto> GetPackage(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get package by urnValue
    /// urn:altinn:accesspackage:skattnaering (Key:urn:altinn:accesspackage, Value:skattnaering)
    /// </summary>
    /// <param name="urnValue">The urnValue to lookup</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The package with the specified Urn.</returns>
    Task<PackageDto> GetPackageByUrnValue(string urnValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all resources associated with a specific package.
    /// </summary>
    /// <param name="packageId">The unique identifier of the package.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A collection of resources related to the specified package.</returns>
    Task<IEnumerable<ResourceDto>> GetPackageResources(Guid packageId, CancellationToken cancellationToken = default);
}
