using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        private readonly IPackageService packageService;

        public PackagesController(IPackageService packageService) 
        {
            this.packageService = packageService;
        }

        [HttpGet]
        public async Task<IEnumerable<AreaGroupDto>> GetAll()
        {
            return await packageService.GetAreaGroupDtos();
        }
    }
}
