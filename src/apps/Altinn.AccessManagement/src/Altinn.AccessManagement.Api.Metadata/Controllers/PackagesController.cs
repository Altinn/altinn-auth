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
    [Route("search")]
    [HttpGet]
    public async Task<ActionResult<SearchObject<PackageDto>>> Search([FromQuery] string term)
    {
        var res = await packageService.Search(term);
        if (res == null || !res.Any())
        {
            return NoContent();
        }

        return Ok(res);
    }

    /// <summary>
    /// Eksporterer hierarkiet av area groups.
    /// </summary>
    /// <returns>Liste over area groups.</returns>
    [Route("export")]
    [HttpGet]
    public async Task<ActionResult<AreaGroupDto>> GetHierarchy()
    {
        var res = await packageService.GetHierarchy();
        if (res == null || !res.Any())
        {
            return NoContent();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter alle area groups.
    /// </summary>
    /// <returns>Liste over area groups.</returns>
    [Route("group/")]
    [HttpGet]
    public async Task<ActionResult<AreaGroup>> GetGroups()
    {
        var res = await packageService.GetAreaGroups();
        if (res == null || !res.Any())
        {
            return NoContent();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter en spesifikk area group basert på ID.
    /// </summary>
    /// <param name="id">Unik identifikator for area group.</param>
    /// <returns>Den spesifikke area group.</returns>
    [Route("group/{id}")]
    [HttpGet]
    public async Task<ActionResult<AreaGroup>> GetGroup(Guid id)
    {
        var res = await packageService.GetAreaGroup(id);
        if (res == null)
        {
            return NotFound();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter alle områder tilhørende en area group.
    /// </summary>
    /// <param name="id">ID til area group.</param>
    /// <returns>Liste over områder.</returns>
    [Route("group/{id}/areas")]
    [HttpGet]
    public async Task<ActionResult<Area>> GetGroupAreas(Guid id)
    {
        var res = await packageService.GetAreas(id);
        if (res == null || !res.Any())
        {
            var grp = await packageService.GetAreaGroup(id);
            if (grp == null)
            {
                // Group is not found
                return NotFound();
            }

            // Group found, but no areas
            return NoContent();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter et spesifikt område basert på ID.
    /// </summary>
    /// <param name="id">ID til området.</param>
    /// <returns>Områdeobjekt.</returns>
    [Route("area/{id}")]
    [HttpGet]
    public async Task<ActionResult<Area>> GetArea(Guid id)
    {
        var res = await packageService.GetArea(id);
        if (res == null)
        {
            return NotFound();
        }
        
        return Ok(res);
    }

    /// <summary>
    /// Henter alle access packages for et spesifikt område.
    /// </summary>
    /// <param name="id">ID til området.</param>
    /// <returns>Liste over access packages.</returns>
    [Route("area/{id}/packages")]
    [HttpGet]
    public async Task<ActionResult<PackageDto>> GetAreaPackages(Guid id)
    {
        var res = await packageService.GetPackagesByArea(id);
        if (res == null || !res.Any())
        {
            var area = await packageService.GetArea(id);
            if (area == null)
            {
                // Area is not found
                return NotFound();
            }

            // Area found, but no packages
            return NoContent();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter en spesifikk access package basert på ID.
    /// </summary>
    /// <param name="id">ID til access package.</param>
    /// <returns>Access package objekt.</returns>
    [Route("package/{id}")]
    [HttpGet]
    public async Task<ActionResult<PackageDto>> GetPackage(Guid id)
    {
        var res = await packageService.GetPackage(id);
        if (res == null)
        {
            return NotFound();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter en spesifikk access package basert på URN-verdi.
    /// </summary>
    /// <param name="urnValue">URN-verdi for access package.</param>
    /// <returns>Access package objekt.</returns>
    [Route("package/urn/{urnValue}")]
    [HttpGet]
    public async Task<ActionResult<PackageDto>> GetPackageByUrn(string urnValue)
    {
        var res = await packageService.GetPackageByUrnValue(urnValue);
        if (res == null)
        {
            return NotFound();
        }

        return Ok(res);
    }

    /// <summary>
    /// Henter alle ressurser tilknyttet en spesifikk access package.
    /// </summary>
    /// <param name="id">ID til access package.</param>
    /// <returns>Liste over ressurser.</returns>
    [Route("package/{id}/resources")]
    [HttpGet]
    public async Task<ActionResult<Resource>> GetPackageResources(Guid id)
    {
        var res = await packageService.GetPackageResources(id);
        if (res == null || !res.Any())
        {
            var pkg = await packageService.GetPackage(id);
            if (pkg == null)
            {
                // Package is not found
                return NotFound();
            }

            // Package found, but no resources
            return NoContent();
        }

        return Ok(res);
    }
}
