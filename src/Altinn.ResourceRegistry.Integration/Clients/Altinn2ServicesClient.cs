using System.Text.Json;
using System.Xml;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Http client used for 
    /// </summary>
    public class Altinn2ServicesClient : IAltinn2Services
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
        };

        private readonly HttpClient _client;
        private readonly PlatformSettings _settings;

        /// <summary>
        /// Client implemenation
        /// </summary>
        public Altinn2ServicesClient(HttpClient client, IOptions<PlatformSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<List<AvailableService>?> AvailableServices(int languageId, CancellationToken cancellationToken = default)
        {
            List<AvailableService>? availableServices = null;
            string availabbleServicePath = _settings.BridgeApiEndpoint + $"metadata/api/availableServices?languageID={languageId}&appTypesToInclude=0&includeExpired=false";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(availabbleServicePath, cancellationToken);
                
                string availableServiceString = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!string.IsNullOrEmpty(availableServiceString))
                {
                    availableServices = JsonSerializer.Deserialize<List<AvailableService>>(availableServiceString, SerializerOptions);
                }

                return availableServices;
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving Action options", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource?> GetServiceResourceFromService(string serviceCode, int serviceEditionCode, CancellationToken cancellationToken = default)
        {
            string bridgeBaseUrl = _settings.BridgeApiEndpoint;
            string url = $"{bridgeBaseUrl}metadata/api/resourceregisterresource?serviceCode={serviceCode}&serviceEditionCode={serviceEditionCode}";

            HttpResponseMessage response = await _client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(contentString))
            {
                return null;
            }

            ServiceResource? serviceResource = JsonSerializer.Deserialize<ServiceResource>(contentString, SerializerOptions);
            return serviceResource;
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy?> GetXacmlPolicy(string serviceCode, int serviceEditionCode, string identifier, CancellationToken cancellationToken = default)
        {
            string bridgeBaseUrl = _settings.BridgeApiEndpoint;
            string url = $"{bridgeBaseUrl}authorization/api/resourcepolicyfile?serviceCode={serviceCode}&serviceEditionCode={serviceEditionCode}&identifier={identifier}";

            HttpResponseMessage response = await _client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string contentString = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrEmpty(contentString))
            {
                return null;
            }

            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(new StringReader(contentString)))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

            return policy;
        }
    }
}
