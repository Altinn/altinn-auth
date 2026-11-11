using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authentication.Integration.Configuration;
using Altinn.Platform.Authentication.Core.Models.ResourceRegistry;
using System.Diagnostics.CodeAnalysis;
using System.Web;
using System.Security.Policy;
using Altinn.Platform.Authentication.Core.Models.Rights;
using System.Net.Http.Json;

namespace Altinn.Platform.Authentication.Integration.ResourceRegister
{
    /// <summary>
    /// Client implementation for integration with the Resource Registry
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ResourceRegistryClient : IResourceRegistryClient
    {
        /// <summary>
        /// How long a resource is kept in the cache. Short, so a resource that changes in the resource registry
        /// (for example turns non-delegable) is picked up within a few minutes.
        /// </summary>
        private static readonly TimeSpan ResourceCacheDuration = TimeSpan.FromMinutes(5);

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<IResourceRegistryClient> _logger;
        private readonly PlatformSettings _platformSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _serializerOptions =
            new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="LookupClient"/> class
        /// </summary>
        /// <param name="httpClient">HttpClient from default httpclientfactory</param>
        /// <param name="logger">the logger</param>
        /// <param name="httpContextAccessor">handler for http context</param>
        /// <param name="platformSettings">the platform setttings</param>
        /// <param name="memoryCache">the memory cache</param>
        public ResourceRegistryClient(
            HttpClient httpClient,
            ILogger<ResourceRegistryClient> logger,
            IHttpContextAccessor httpContextAccessor,
            IOptions<PlatformSettings> platformSettings,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _platformSettings = platformSettings.Value;
            httpClient.BaseAddress = new Uri(_platformSettings.ApiResourceRegistryEndpoint!);
            _httpClient = httpClient;
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <inheritdoc/>
        public async Task<ServiceResource?> GetResource(string resourceId)
        {
            string cacheKey = $"resourceregistry:resource:{resourceId}";
            if (_memoryCache.TryGetValue(cacheKey, out ServiceResource? resource) && resource is not null)
            {
                return resource;
            }

            resource = null;

            try
            {
                // It's not possible to filter on AltinnApp or Altinn2Service for this endpoint
                string endpointUrl = $"resource/{HttpUtility.UrlEncode(resourceId)}";

                HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    string content = await response.Content.ReadAsStringAsync();
                    resource = JsonSerializer.Deserialize<ServiceResource>(content, options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication // ResourceRegistryClient // SearchResources // Exception");
                throw;
            }

            // Only a resource that was found is cached. A missing resource is looked up again on the next call, so
            // one that has just been created in the resource registry can be used right away.
            if (resource is not null)
            {
                _memoryCache.Set(cacheKey, resource, ResourceCacheDuration);
            }

            return resource;
        }

        public async Task<List<PolicyRightsDTO>> GetRights(string resourceId)
        {
            try
            {
                string endpointUrl = $"resource/{HttpUtility.UrlEncode(resourceId)}/policy/rights";

                HttpResponseMessage response = await _httpClient.GetAsync(endpointUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var policyRightsDTOs = await response.Content.ReadFromJsonAsync<List<PolicyRightsDTO>>(_serializerOptions);
                    if (policyRightsDTOs is not null)
                    {
                        return policyRightsDTOs;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication // ResourceRegistryClient // GetRights // Exception");
                throw;
            }
            return [];
        }
    }
}
