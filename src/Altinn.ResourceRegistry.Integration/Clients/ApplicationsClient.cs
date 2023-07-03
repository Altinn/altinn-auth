using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client to get Application info from Altinn Storage
    /// </summary>
    public class ApplicationsClient: IApplications
    {
        private readonly HttpClient _client;
        private readonly PlatformSettings _settings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationsClient(HttpClient client, IOptions<PlatformSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<ApplicationList> GetApplicationList()
        {
            ApplicationList applicationList;
            string availabbleServicePath = _settings.StorageApiEndpoint + $"applications";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(availabbleServicePath);

                string responseContent = await response.Content.ReadAsStringAsync();
                applicationList = System.Text.Json.JsonSerializer.Deserialize<ApplicationList>(responseContent, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                return applicationList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving applications", ex);
            }
        }
    }
}
