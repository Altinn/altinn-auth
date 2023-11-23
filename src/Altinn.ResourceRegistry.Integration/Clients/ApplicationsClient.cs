using System.Net.Http.Json;
using System.Text.Json;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Services.Interfaces;
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

        /// <summary>
        /// Default constructor
        /// </summary>
        public ApplicationsClient(HttpClient client, IOptions<PlatformSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<ApplicationList> GetApplicationList(CancellationToken cancellationToken = default)
        {
            string availabbleServicePath = _settings.StorageApiEndpoint + $"applications";

            try
            {
                HttpResponseMessage response = await _client.GetAsync(availabbleServicePath, cancellationToken);

                return await response.Content.ReadFromJsonAsync<ApplicationList>(SerializerOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong when retrieving applications", ex);
            }
        }
    }
}
