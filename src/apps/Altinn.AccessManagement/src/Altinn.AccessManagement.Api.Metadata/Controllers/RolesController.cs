using System.ComponentModel.DataAnnotations;
using Altinn.AccessMgmt.Core.Extensions;
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

            // Translate the collection with deep translation for nested Provider
            var translated = await res.TranslateDeepAsync(
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
            var res = await roleService.GetById([id]);
            if (res == null)
            {
                return NotFound();
            }

            // Translate the role with deep translation for nested Provider
            var translated = await res.TranslateDeepAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
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
            
            // Translate the packages with deep translation for nested Area and Resources
            var translated = await packages.TranslateDeepAsync(
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
            
            // Translate the resources with deep translation for nested Provider and Type
            var translated = await resources.TranslateDeepAsync(
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
            
            // Translate the packages with deep translation for nested Area and Resources
            var translated = await packages.TranslateDeepAsync(
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
            
            // Translate the resources with deep translation for nested Provider and Type
            var translated = await resources.TranslateDeepAsync(
                translationService,
                this.GetLanguageCode(),
                this.AllowPartialTranslation());

            return Ok(translated);
        }
    }
}
