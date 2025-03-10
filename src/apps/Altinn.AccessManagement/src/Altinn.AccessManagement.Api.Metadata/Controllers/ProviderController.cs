using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Controllers
{
    /// <summary>
    /// Provider controller
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderRepository providerRepository;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="providerRepository"><see cref="IProviderRepository"/></param>
        public ProviderController(IProviderRepository providerRepository)
        {
            this.providerRepository = providerRepository;
        }

        /// <summary>
        /// Get all providers
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IEnumerable<Provider>> GetAll()
        {
            return await providerRepository.Get();
        }
    }
}
