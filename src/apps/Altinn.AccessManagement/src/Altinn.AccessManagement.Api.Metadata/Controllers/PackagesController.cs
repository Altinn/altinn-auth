using Altinn.AccessManagement.Api.Metadata.Translation;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
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
    private readonly ITranslationService translationService;

    /// <summary>
    /// Initialiserer en ny instans av <see cref="PackagesController"/>.
    /// </summary>
    /// <param name="packageService">Service for håndtering av access packages.</param>
    /// <param name="translationService">Service for translation of entities.</param>
    public PackagesController(IPackageService packageService, ITranslationService translationService)
    {
        this.packageService = packageService;
        this.translationService = translationService;
    }

    /// <summary>
    /// Søker etter access packages basert på et søkeord.
    /// </summary>
    /// <param name="term">Søketerm.</param>
    /// <param name="resourceProviderCode">Tjenesteeier OrgCode (DIGDIR, KRT, NAV, SKATT)</param>   
    /// <param name="searchInResources">Søk i ressurs verdier</param>
    /// <param name="typeName">Package for type (e.g. Organization, Person)</param>
    /// <returns>Liste over søkeresultater.</returns>
    [Route("search")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchObject<PackageDto>>>> Search([FromQuery] string term, [FromQuery] List<string> resourceProviderCode = null, [FromQuery] bool searchInResources = false, [FromQuery] string? typeName = null)
    {
        Guid? typeId = null;
        if (!string.IsNullOrEmpty(typeName))
        {
            if (!EntityTypeConstants.TryGetByName(name: typeName, includeTranslations: true, out var type))
            {
                return Problem($"Type '{typeName}' not found. Try organization, organisasjon or person");
            }

            typeId = type.Id;
        }

        var res = await packageService.Search(term, resourceProviderCode, searchInResources, typeId);
        if (res == null || !res.Any())
        {
            return NoContent();
        }

        // Translate each search result with deep translation for nested objects
        var translatedResults = new List<SearchObject<PackageDto>>();
        foreach (var searchResult in res)
        {
            var translatedDto = await searchResult.Object.TranslateDeepAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());
            
            translatedResults.Add(new SearchObject<PackageDto> 
            { 
                Object = translatedDto,
                Score = searchResult.Score,
                Fields = searchResult.Fields
            });
        }

        return Ok(translatedResults);
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

        // Translate the area groups with deep translation for nested Areas and Packages
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }

    /// <summary>
    /// Henter alle area groups.
    /// </summary>
    /// <returns>Liste over area groups.</returns>
    [Route("group/")]
    [HttpGet]
    public async Task<ActionResult<AreaGroupDto>> GetGroups()
    {
        var res = await packageService.GetAreaGroups();
        if (res == null || !res.Any())
        {
            return NoContent();
        }

        // Translate the area groups with deep translation for nested Areas and Packages
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }

    /// <summary>
    /// Henter en spesifikk area group basert på ID.
    /// </summary>
    /// <param name="id">Unik identifikator for area group.</param>
    /// <returns>Den spesifikke area group.</returns>
    [Route("group/{id}")]
    [HttpGet]
    public async Task<ActionResult<AreaGroupDto>> GetGroup(Guid id)
    {
        var res = await packageService.GetAreaGroup(id);
        if (res == null)
        {
            return NotFound();
        }

        // Translate the area group with deep translation for nested Areas and Packages
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }

    /// <summary>
    /// Henter alle områder tilhørende en area group.
    /// </summary>
    /// <param name="id">ID til area group.</param>
    /// <returns>Liste over områder.</returns>
    [Route("group/{id}/areas")]
    [HttpGet]
    public async Task<ActionResult<AreaDto>> GetGroupAreas(Guid id)
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

        // Translate the areas with deep translation for nested Groups and Packages
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }

    /// <summary>
    /// Henter et spesifikt område basert på ID.
    /// </summary>
    /// <param name="id">ID til området.</param>
    /// <returns>Områdeobjekt.</returns>
    [Route("area/{id}")]
    [HttpGet]
    public async Task<ActionResult<AreaDto>> GetArea(Guid id)
    {
        var res = await packageService.GetArea(id);
        if (res == null)
        {
            return NotFound();
        }

        // Translate the area with deep translation for nested Groups and Packages
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());
        
        return Ok(translated);
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

        // Translate the packages with deep translation for nested Area and Resources
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
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

        // Translate the package with deep translation for nested Area and Resources
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
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

        // Translate the package with deep translation for nested Area and Resources
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }

    /// <summary>
    /// Henter alle ressurser tilknyttet en spesifikk access package.
    /// </summary>
    /// <param name="id">ID til access package.</param>
    /// <returns>Liste over ressurser.</returns>
    [Route("package/{id}/resources")]
    [HttpGet]
    public async Task<ActionResult<ResourceDto>> GetPackageResources(Guid id)
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

        // Translate the resources with deep translation for nested Provider and Type
        var translated = await res.TranslateDeepAsync(
            translationService,
            this.GetLanguageCode(),
            this.AllowPartialTranslation());

        return Ok(translated);
    }
}
