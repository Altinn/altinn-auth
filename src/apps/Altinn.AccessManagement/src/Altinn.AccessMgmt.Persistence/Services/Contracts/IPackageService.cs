using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services.Contracts;

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
    /// <param name="searchInResources">Indicate if term should filter on resource values</param>
    /// <returns>A list of search results containing packages that match the term.</returns>
    Task<IEnumerable<SearchObject<PackageDto>>> Search(string term, bool searchInResources = false);

    /// <summary>
    /// Retrieves the hierarchical structure of area groups.
    /// </summary>
    /// <returns>A collection of area group DTOs representing the hierarchy.</returns>
    Task<IEnumerable<AreaGroupDto>> GetHierarchy();

    /// <summary>
    /// Retrieves all area groups.
    /// </summary>
    /// <returns>A list of area groups.</returns>
    Task<IEnumerable<ExtAreaGroup>> GetAreaGroups();

    /// <summary>
    /// Retrieves a specific area group by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the area group.</param>
    /// <returns>The area group with the specified ID.</returns>
    Task<ExtAreaGroup> GetAreaGroup(Guid id);

    /// <summary>
    /// Retrieves all areas within a specified area group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the area group.</param>
    /// <returns>A collection of areas associated with the specified group.</returns>
    Task<IEnumerable<Area>> GetAreas(Guid groupId);

    /// <summary>
    /// Retrieves a specific area by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the area.</param>
    /// <returns>The area with the specified ID.</returns>
    Task<Area> GetArea(Guid id);

    /// <summary>
    /// Retrieves all packages associated with a specific area.
    /// </summary>
    /// <param name="areaId">The unique identifier of the area.</param>
    /// <returns>A collection of packages belonging to the specified area.</returns>
    Task<IEnumerable<PackageDto>> GetPackagesByArea(Guid areaId);

    /// <summary>
    /// Retrieves a specific package by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the package.</param>
    /// <returns>The package with the specified ID.</returns>
    Task<PackageDto> GetPackage(Guid id);

    /// <summary>
    /// Get package by urnValue
    /// urn:altinn:accesspackage:skattnaering (Key:urn:altinn:accesspackage, Value:skattnaering)
    /// </summary>
    /// <param name="urnValue">The urnValue to lookup</param>
    /// <returns>The package with the specified Urn.</returns>
    Task<PackageDto> GetPackageByUrnValue(string urnValue);

    /// <summary>
    /// Retrieves all resources associated with a specific package.
    /// </summary>
    /// <param name="packageId">The unique identifier of the package.</param>
    /// <returns>A collection of resources related to the specified package.</returns>
    Task<IEnumerable<Resource>> GetPackageResources(Guid packageId);
}
