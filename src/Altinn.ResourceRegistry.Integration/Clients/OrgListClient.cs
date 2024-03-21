using System.Net.Http.Json;
using System.Text.Json;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client responsible for collection 
    /// </summary>
    public class OrgListClient : IOrgListClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly HttpClient _client;
        private readonly ResourceRegistrySettings _settings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public OrgListClient(HttpClient client, IOptions<ResourceRegistrySettings> resourceRegistrySettings)
        {
            _client = client;
            _settings = resourceRegistrySettings.Value;
        }

        /// <summary>
        /// Returns configured org list
        /// </summary>
        public async Task<OrgList?> GetOrgList(CancellationToken cancellationToken = default)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await _client.GetAsync(_settings.OrgListEndpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    response.Dispose();
                    response = await _client.GetAsync(_settings.OrgListAlternativeEndpoint, cancellationToken);
                }

                return await response.Content.ReadFromJsonAsync<OrgList>(SerializerOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving Action options", ex);
            }
            finally 
            { 
                response?.Dispose(); 
            }
        }
    }
}
