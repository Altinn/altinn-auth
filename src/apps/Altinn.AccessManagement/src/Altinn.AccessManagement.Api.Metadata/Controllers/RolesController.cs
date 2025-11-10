using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
        private readonly AppDbContext dbContext;

        /// <summary>
        /// Initialiserer en ny instans av <see cref="RolesController"/>.
        /// </summary>
        /// <param name="roleService">Service for håndtering av roller.</param>
        public RolesController(IRoleService roleService)
        {
            this.roleService = roleService;
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

            return Ok(res);
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

            return Ok(res);
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

            return Ok(res);
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

            return Ok(res);
        }

        /// <summary>
        /// Gets role packages
        /// </summary>
        [HttpGet("packages")]
        [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
        public async ValueTask<IEnumerable<PackageDto>> GetPackages([FromQuery] string role, [FromQuery] string variant, [FromQuery] bool includeResources)
        {
            RoleConstants.TryGetByCode(string.IsNullOrEmpty(role) ? "_" : role, out var roleDef);
            EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef);
            if (roleDef == null)
            {
                throw new ArgumentException("Role not found");
            }

            if (variantDef == null)
            {
                throw new ArgumentException("Variant not found");
            }

            return await roleService.GetRolePackages(roleDef.Id, variantDef.Id, includeResources);
        }

        /// <summary>
        /// Gets role resources
        /// </summary>
        [HttpGet("resources")]
        [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
        public async ValueTask<IEnumerable<ResourceDto>> GetResources([FromQuery] string role, [FromQuery] string variant, [FromQuery] bool includePackageResoures)
        {
            RoleConstants.TryGetByCode(string.IsNullOrEmpty(role) ? "_" : role, out var roleDef);
            EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef);
            if (roleDef == null)
            {
                throw new ArgumentException("Role not found");
            }

            if (variantDef == null)
            {
                throw new ArgumentException("Variant not found");
            }

            return await roleService.GetRoleResources(roleDef.Id, variantDef.Id, includePackageResoures);
        }

        /// <summary>
        /// Gets role packages
        /// </summary>
        [HttpGet("{id}/packages")]
        [ProducesResponseType(typeof(PackageDto), StatusCodes.Status200OK)]
        public async ValueTask<IEnumerable<PackageDto>> GetPackages([FromRoute] Guid roleId, [FromQuery] string variant, [FromQuery] bool includeResources)
        {
            EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef);
            if (variantDef == null)
            {
                throw new ArgumentException("Variant not found");
            }

            return await roleService.GetRolePackages(roleId, variantDef.Id, includeResources);
        }

        /// <summary>
        /// Gets role resources
        /// </summary>
        [HttpGet("{id}/resources")]
        [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
        public async ValueTask<IEnumerable<ResourceDto>> GetResources([FromRoute] Guid roleId, [FromQuery] string variant, [FromQuery] bool includePackageResoures)
        {
            EntityVariantConstants.TryGetByName(string.IsNullOrEmpty(variant) ? "_" : variant, out var variantDef);
            if (variantDef == null)
            {
                throw new ArgumentException("Variant not found");
            }

            return await roleService.GetRoleResources(roleId, variantDef.Id, includePackageResoures);
        }
    }
}
