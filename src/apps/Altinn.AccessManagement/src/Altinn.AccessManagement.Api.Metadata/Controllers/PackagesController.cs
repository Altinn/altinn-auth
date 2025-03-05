using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers;

/// <summary>
/// Controller for å håndtere metadata og informasjon om access packages.
/// </summary>
[Route("/accessmanagement/api/v1/meta/info/accesspackages/")]
[ApiController]
public class PackagesController : ControllerBase
{
    private readonly IPackageService packageService;

    /// <summary>
    /// Initialiserer en ny instans av <see cref="PackagesController"/>.
    /// </summary>
    /// <param name="packageService">Service for håndtering av access packages.</param>
    public PackagesController(IPackageService packageService)
    {
        this.packageService = packageService;
    }

    /// <summary>
    /// Søker etter access packages basert på et søkeord.
    /// </summary>
    /// <param name="term">Søketerm.</param>
    /// <returns>Liste over søkeresultater.</returns>
    [Route("search/{term}")]
    [HttpGet]
    public async Task<IEnumerable<SearchObject<PackageDto>>> Search(string term)
    {
        return await packageService.Search(term);
    }

    /// <summary>
    /// Eksporterer hierarkiet av area groups.
    /// </summary>
    /// <returns>Liste over area groups.</returns>
    [Route("export")]
    [HttpGet]
    public async Task<IEnumerable<AreaGroupDto>> GetHierarchy()
    {
        return await packageService.GetHierarchy();
    }

    /// <summary>
    /// Henter alle area groups.
    /// </summary>
    /// <returns>Liste over area groups.</returns>
    [Route("group/")]
    [HttpGet]
    public async Task<IEnumerable<AreaGroup>> GetGroups()
    {
        return await packageService.GetAreaGroups();
    }

    /// <summary>
    /// Henter en spesifikk area group basert på ID.
    /// </summary>
    /// <param name="id">Unik identifikator for area group.</param>
    /// <returns>Den spesifikke area group.</returns>
    [Route("group/{id}")]
    [HttpGet]
    public async Task<AreaGroup> GetGroup(Guid id)
    {
        return await packageService.GetAreaGroup(id);
    }

    /// <summary>
    /// Henter alle områder tilhørende en area group.
    /// </summary>
    /// <param name="id">ID til area group.</param>
    /// <returns>Liste over områder.</returns>
    [Route("group/{id}/areas")]
    [HttpGet]
    public async Task<IEnumerable<Area>> GetGroupAreas(Guid id)
    {
        return await packageService.GetAreas(id);
    }

    /// <summary>
    /// Henter et spesifikt område basert på ID.
    /// </summary>
    /// <param name="id">ID til området.</param>
    /// <returns>Områdeobjekt.</returns>
    [Route("area/{id}")]
    [HttpGet]
    public async Task<Area> GetArea(Guid id)
    {
        return await packageService.GetArea(id);
    }

    /// <summary>
    /// Henter alle access packages for et spesifikt område.
    /// </summary>
    /// <param name="id">ID til området.</param>
    /// <returns>Liste over access packages.</returns>
    [Route("area/{id}/packages")]
    [HttpGet]
    public async Task<IEnumerable<PackageDto>> GetAreaPackages(Guid id)
    {
        return await packageService.GetPackagesByArea(id);
    }

    /// <summary>
    /// Henter en spesifikk access package basert på ID.
    /// </summary>
    /// <param name="id">ID til access package.</param>
    /// <returns>Access package objekt.</returns>
    [Route("package/{id}")]
    [HttpGet]
    public async Task<PackageDto> GetPackage(Guid id)
    {
        return await packageService.GetPackage(id);
    }

    /// <summary>
    /// Henter en spesifikk access package basert på URN-verdi.
    /// </summary>
    /// <param name="urnValue">URN-verdi for access package.</param>
    /// <returns>Access package objekt.</returns>
    [Route("package/urn/{urnvalue}")]
    [HttpGet]
    public async Task<PackageDto> GetPackageByUrn(string urnValue)
    {
        return await packageService.GetPackageByUrnValue(urnValue);
    }

    /// <summary>
    /// Henter alle ressurser tilknyttet en spesifikk access package.
    /// </summary>
    /// <param name="id">ID til access package.</param>
    /// <returns>Liste over ressurser.</returns>
    [Route("package/{id}/resources")]
    [HttpGet]
    public async Task<IEnumerable<Resource>> GetPackageResources(Guid id)
    {
        return await packageService.GetPackageResources(id);
    }
}
