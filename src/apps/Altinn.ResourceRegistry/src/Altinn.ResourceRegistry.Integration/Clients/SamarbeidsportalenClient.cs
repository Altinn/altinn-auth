using System.Net.Http.Json;
using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Integration.Clients
{
    /// <summary>
    /// Client for the open metadata API in Samarbeidsportalen. The API needs no authentication.
    /// Retries and timeouts come from the standard resilience handler that ServiceDefaults adds
    /// to every <see cref="HttpClient"/>.
    /// </summary>
    public class SamarbeidsportalenClient : ISamarbeidsportalenClient
    {
        private readonly HttpClient _client;
        private readonly SamarbeidsportalenSettings _settings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SamarbeidsportalenClient(HttpClient client, IOptions<SamarbeidsportalenSettings> settings)
        {
            _client = client;
            _settings = settings.Value;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MaskinportenScopePrefix>> GetScopePrefixes(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.PrefixEndpoint))
            {
                throw new InvalidOperationException($"{nameof(SamarbeidsportalenSettings)}:{nameof(SamarbeidsportalenSettings.PrefixEndpoint)} is not configured");
            }

            using HttpResponseMessage response = await _client.GetAsync(_settings.PrefixEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            List<MaskinportenScopePrefix>? prefixes = await response.Content.ReadFromJsonAsync<List<MaskinportenScopePrefix>>(cancellationToken);
            return prefixes ?? throw new InvalidOperationException($"The prefix list from {_settings.PrefixEndpoint} had no content");
        }
    }
}
