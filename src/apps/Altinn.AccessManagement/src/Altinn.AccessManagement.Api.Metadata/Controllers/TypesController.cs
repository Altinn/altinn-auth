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
