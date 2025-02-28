using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderRepository providerRepository;

        public ProviderController(IProviderRepository providerRepository)
        {
            this.providerRepository = providerRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<Provider>> GetAll()
        {
            return await providerRepository.Get();
        }
    }
}
