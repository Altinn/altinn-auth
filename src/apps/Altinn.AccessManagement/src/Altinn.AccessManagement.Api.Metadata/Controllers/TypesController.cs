using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers
{
    [Route("/accessmanagement/api/v1/meta/types")]
    [ApiController]
    public class TypesController : ControllerBase
    {
        /// <summary>
        /// Gets all <see cref="TypeDto"/>
        /// </summary>
        /// [Route("")]
        /// [HttpGet]
        private ActionResult<List<TypeDto>> GetAllTypes()
        {
            return EntityTypeConstants.AllEntities().Select(t => DtoMapper.Convert(t.Entity)).ToList();
        }

        /// <summary>
        /// Gets all <see cref="TypeDto"/> for given type
        /// </summary>
        /// [Route("{parentTypeName}/subtypes")]
        /// [HttpGet]
        private ActionResult<List<SubTypeDto>> GetSubTypes(string parentTypeName)
        {
            if (!EntityTypeConstants.TryGetByName(parentTypeName, out var entityType))
            {
                return NotFound($"Type {parentTypeName} not found. Try 'organization'");
            }

            return EntityVariantConstants.AllEntities().Where(t => t.Entity.TypeId == entityType.Id).Select(t => DtoMapper.ConvertFlat(t.Entity)).ToList();
        }

        /// <summary>
        /// Gets all organization sub types <see cref="SubTypeDto"/>
        /// </summary>
        [Route("organization/subtypes")]
        [HttpGet]
        public ActionResult<List<SubTypeDto>> GetOrganizationSubTypes()
        {
            return EntityVariantConstants.AllEntities().Where(t => t.Entity.TypeId == EntityTypeConstants.Organization.Id).Select(t => DtoMapper.ConvertFlat(t.Entity)).ToList();
        }
    }
}
