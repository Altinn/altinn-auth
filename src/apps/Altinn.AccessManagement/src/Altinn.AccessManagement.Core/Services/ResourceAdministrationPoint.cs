using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.Core.Services
{
    /// <inheritdoc />
    public class ResourceAdministrationPoint : IResourceAdministrationPoint
    {
        private readonly ILogger<IResourceAdministrationPoint> _logger;
        private readonly IContextRetrievalService _contextRetrievalService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAdministrationPoint"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="contextRetrievalService">the handler for resource registry client</param>
        public ResourceAdministrationPoint(
            ILogger<IResourceAdministrationPoint> logger,
            IContextRetrievalService contextRetrievalService)
        {
            _logger = logger;
            _contextRetrievalService = contextRetrievalService;
        }

        /// <inheritdoc />
        public async Task<List<ServiceResource>> GetResources(ResourceType resourceType)
        {
            try
            {
                List<ServiceResource> resources = await _contextRetrievalService.GetResources();
                return resources.FindAll(r => r.ResourceType == resourceType);
            }
            catch (Exception)
            {
                _logger.LogError("//ResourceAdministrationPoint // GetResources by resourcetype failed to fetch resources");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ServiceResource>> GetResources(string scope, CancellationToken cancellationToken)
        {
            List<ServiceResource> resources = await _contextRetrievalService.GetResources(cancellationToken);

            if (string.IsNullOrWhiteSpace(scope))
            {
                return resources.Where(r => r.ResourceType == ResourceType.MaskinportenSchema && r.ResourceReferences != null && r.ResourceReferences.Any(rf => rf.ReferenceType == ReferenceType.MaskinportenScope));
            }
            else
            {
                return resources.Where(r => r.ResourceType == ResourceType.MaskinportenSchema && r.ResourceReferences != null && r.ResourceReferences.Any(rf => rf.ReferenceType == ReferenceType.MaskinportenScope && rf.Reference.Equals(scope)));
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResource> GetResource(string resourceRegistryId)
        {
            return await _contextRetrievalService.GetResource(resourceRegistryId);
        }
    }
}
