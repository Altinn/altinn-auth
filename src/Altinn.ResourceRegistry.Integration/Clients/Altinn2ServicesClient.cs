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
        public async Task<List<AvailableService>?> AvailableServices(int languageId)
        {
            List<AvailableService>? availableServices = null;
            string availabbleServicePath = _settings.BridgeApiEndpoint + $"metadata/api/availableServices?languageID={languageId}&appTypesToInclude=0&includeExpired=false";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(availabbleServicePath);
                
                string availableServiceString = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(availableServiceString))
                {
                    availableServices = System.Text.Json.JsonSerializer.Deserialize<List<AvailableService>>(availableServiceString, new System.Text.Json.JsonSerializerOptions());
                }

                return availableServices;
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving Action options", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResource?> GetServiceResourceFromService(string serviceCode, int serviceEditionCode)
        {
            string bridgeBaseUrl = _settings.BridgeApiEndpoint;
            string url = $"{bridgeBaseUrl}metadata/api/resourceregisterresource?serviceCode={serviceCode}&serviceEditionCode={serviceEditionCode}";

            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string contentString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(contentString))
            {
                return null;
            }

            ServiceResource? serviceResource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(contentString);
            return serviceResource;
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy?> GetXacmlPolicy(string serviceCode, int serviceEditionCode, string identifier)
        {
            string bridgeBaseUrl = _settings.BridgeApiEndpoint;
            string url = $"{bridgeBaseUrl}authorization/api/resourcepolicyfile?serviceCode={serviceCode}&serviceEditionCode={serviceEditionCode}&identifier={identifier}";

            HttpResponseMessage response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string contentString = await response.Content.ReadAsStringAsync();
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
