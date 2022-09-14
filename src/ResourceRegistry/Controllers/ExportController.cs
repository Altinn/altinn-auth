using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Utils;
using Microsoft.AspNetCore.Mvc;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Altinn.ResourceRegistry.Controllers
{
    [Route("ResourceRegistry/api/[controller]")]
    public class ExportController : Controller
    {
        private IResourceRegistry _resourceRegistry;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ExportController(IResourceRegistry resourceRegistry)
        {
            _resourceRegistry = resourceRegistry;
        }

        protected async Task<IActionResult> Index()
        {
            List<ServiceResource> serviceResources = await _resourceRegistry.Search(null);
            string rdfString = RdfUtil.CreateRdf(serviceResources);
            return Content(rdfString);
        }
    }
}
