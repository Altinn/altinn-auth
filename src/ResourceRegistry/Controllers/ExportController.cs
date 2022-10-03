using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.ResourceRegistry.Controllers
{
    /// <summary>
    /// Controller responsible export of resources from the resource registry
    /// </summary>
    [Route("ResourceRegistry/api/v1/[controller]")]
    public class ExportController : Controller
    {
        private IResourceRegistry _resourceRegistry;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportController"/> controller.
        /// </summary>
        /// <param name="resourceRegistry">Service implementation for resource registry</param>
        public ExportController(IResourceRegistry resourceRegistry)
        {
            _resourceRegistry = resourceRegistry;
        }

        /// <summary>
        /// Index
        /// </summary>
        /// <returns>ActionResult</returns>
        protected async Task<IActionResult> Index()
        {
            List<ServiceResource> serviceResources = await _resourceRegistry.Search(null);
            string rdfString = RdfUtil.CreateRdf(serviceResources);
            return Content(rdfString);
        }
    }
}
