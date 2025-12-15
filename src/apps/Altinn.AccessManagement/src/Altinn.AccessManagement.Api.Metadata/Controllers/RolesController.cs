using System.ComponentModel.DataAnnotations;
using Altinn.AccessManagement.Api.Metadata.Translation;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers
{
    /// <summary>
    /// Metadata endpoints for Roles 
    /// </summary>
    [Route("/accessmanagement/api/v1/meta/info/roles/")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService roleService;
        private readonly ITranslationService translationService;

        /// <summary>
        /// Initialiserer en ny instans av <see cref="RolesController"/>.
        /// </summary>
        /// <param name="roleService">Service for håndtering av roller.</param>
        /// <param name="translationService">Service for translation of entities.</param>
        public RolesController(IRoleService roleService, ITranslationService translationService)
        {
            this.roleService = roleService;
            this.translationService = translationService;
        }

        /// <summary>
        /// Gets all <see cref="RoleDto"/>
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Route("")]
        [HttpGet]
        public async Task<ActionResult<List<RoleDto>>> GetAll()
        {
            var res = await roleService.GetAll();
            if (res == null)
            {
                return NotFound();
            }

            // Translate the collection
            var translated = await res.TranslateAsync(
                translationService, 
                this.GetLanguageCode(), 
                this.AllowPartialTranslation());

            return Ok(translated.ToList());
        }

        /// <summary>
        /// Gets <see cref="RoleDto"/> with id
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Route("{id}")]
        [HttpGet]
        public async Task<ActionResult<RoleDto>> GetId(Guid id)
        {
            var res = await roleService.GetById(id);
            if (res == null)
            {
                return NotFound();
            }

            // Translate the role
            var translated = await res.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }

        /// <summary>
        /// Gets <see cref="RoleDto"/> with a key/pair value
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Route("lookup")]
        [HttpGet]
        public async Task<ActionResult<RoleDto>> GetKeyPairValue([FromQuery] string key, [FromQuery] string value)
        {
            var res = await roleService.GetByKeyValue(key, value);
            if (res == null)
            {
                return NotFound();
            }

            // Translate the role
            var translated = await res.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }

        /// <summary>
        /// Gets possible lookup keys for roles
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [Route("lookup/keys")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetKeys()
        {
            var res = await roleService.GetLookupKeys();
            if (res == null)
            {
                return NotFound();
            }

            // No translation needed for lookup keys
            return Ok(res);
        }

        /// <summary>
        /// Gets role packages
        /// </summary>
        [HttpGet("packages")]
        [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
        public async ValueTask<ActionResult<IEnumerable<PackageDto>>> GetPackages([Required][FromQuery] string role, [Required][FromQuery] string variant, [FromQuery] bool includeResources = false)
        {            
            if (!RoleConstants.TryGetByCode(string.IsNullOrEmpty(role) ? "_" : role, out var roleDef))
            {
                return NotFound($"Role '{role}' not found");
            }

            if (!EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef))
            {
                return NotFound($"Variant '{variant}' not found");
            }

            var packages = await roleService.GetRolePackages(roleDef.Id, variantDef.Id, includeResources);
            
            // Translate the packages
            var translated = await packages.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }

        /// <summary>
        /// Gets role resources
        /// </summary>
        [HttpGet("resources")]
        [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
        public async ValueTask<ActionResult<IEnumerable<ResourceDto>>> GetResources([Required][FromQuery] string role, [Required][FromQuery] string variant, [FromQuery] bool includePackageResources = false)
        {
            if (!RoleConstants.TryGetByCode(string.IsNullOrEmpty(role) ? "_" : role, out var roleDef))
            {
                return NotFound($"Role '{role}' not found");
            }

            if (!EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef))
            {
                return NotFound($"Variant '{variant}' not found");
            }

            var resources = await roleService.GetRoleResources(roleDef.Id, variantDef.Id, includePackageResources);
            
            // Translate the resources
            var translated = await resources.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }

        /// <summary>
        /// Gets role packages
        /// </summary>
        [HttpGet("{id}/packages")]
        [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PackageDto>>> GetPackages([Required][FromRoute] Guid id, [Required][FromQuery] string variant, [FromQuery] bool includeResources = false)
        {
            if (!EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef))
            {
                return NotFound($"Variant '{variant}' not found");
            }

            var packages = await roleService.GetRolePackages(id, variantDef.Id, includeResources);
            
            // Translate the packages
            var translated = await packages.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }

        /// <summary>
        /// Gets role resources
        /// </summary>
        [HttpGet("{id}/resources")]
        [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources([Required][FromRoute] Guid id, [Required][FromQuery] string variant, [FromQuery] bool includePackageResources = false)
        {
            if (!EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef))
            {
                return NotFound($"Variant '{variant}' not found");
            }

            var resources = await roleService.GetRoleResources(id, variantDef.Id, includePackageResources);
            
            // Translate the resources
            var translated = await resources.TranslateAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }
    }
}
