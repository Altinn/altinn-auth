using System.Net.Http.Json;
using System.Text.Json;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client to get Application info from Altinn Storage
    /// </summary>
    public class ApplicationsClient: IApplications
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient _client;
        private readonly PlatformSettings _settings;
        private readonly IMemoryCache _memoryCache;
        private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High)
            .SetAbsoluteExpiration(new TimeSpan(0, 0, 10, 0));

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationsClient(HttpClient client, IOptions<PlatformSettings> settings, IMemoryCache memoryCache)
        {
            _client = client;
            _settings = settings.Value;
            _memoryCache = memoryCache;
        }

        /// <inheritdoc/>
        public async Task<ApplicationList?> GetApplicationList(bool includeMigratedApps , CancellationToken cancellationToken = default)
        {
            string availabbleServicePath = _settings.StorageApiEndpoint + $"applications";

            try
            {
                string cacheKey = "applications";
                if (!_memoryCache.TryGetValue(cacheKey, out ApplicationList? applications))
                {
                    HttpResponseMessage response = await _client.GetAsync(availabbleServicePath, cancellationToken);

                    applications = await response.Content.ReadFromJsonAsync<ApplicationList>(SerializerOptions, cancellationToken);
                    _memoryCache.Set(cacheKey, applications, _cacheEntryOptions);
                }
                
                if (includeMigratedApps)
                {
                    return applications;
                }
                else
                {
                    return applications != null
                        ? new()
                        {
                            Applications = applications.Applications
                            .Where(a => !a.Id.Contains("/a2-") && !a.Id.Contains("/a1-")).ToList()
                        }
                        : null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving applications", ex);
            }
        }
    }
}
