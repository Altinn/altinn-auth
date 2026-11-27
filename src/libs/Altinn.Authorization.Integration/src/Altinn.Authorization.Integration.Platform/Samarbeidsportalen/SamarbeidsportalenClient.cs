#nullable enable

using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.Samarbeidsportalen;

/// <summary>
/// Implementation of <see cref="ISamarbeidsportalenClient"/>. The API needs no authentication, so
/// the client uses its own named <see cref="HttpClient"/> without a platform access token. Retries
/// and timeouts come from the HTTP client defaults the host configures (ServiceDefaults).
/// </summary>
/// <param name="httpClientFactory">Factory for the named <see cref="HttpClient"/>.</param>
/// <param name="options">The <see cref="SamarbeidsportalenOptions"/>.</param>
internal sealed class SamarbeidsportalenClient(
    IHttpClientFactory httpClientFactory,
    IOptions<SamarbeidsportalenOptions> options)
    : ISamarbeidsportalenClient
{
    /// <summary>
    /// Name of the <see cref="HttpClient"/> used for Samarbeidsportalen.
    /// </summary>
    internal const string HttpClientName = "Samarbeidsportalen";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MaskinportenScopePrefix>> GetScopePrefixes(CancellationToken cancellationToken = default)
    {
        var endpoint = options.Value.PrefixEndpoint
            ?? throw new InvalidOperationException($"{nameof(SamarbeidsportalenOptions)}:{nameof(SamarbeidsportalenOptions.PrefixEndpoint)} is not configured");

        var client = httpClientFactory.CreateClient(HttpClientName);
        using HttpResponseMessage response = await client.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        List<MaskinportenScopePrefix>? prefixes = await response.Content.ReadFromJsonAsync<List<MaskinportenScopePrefix>>(cancellationToken);
        return prefixes ?? throw new InvalidOperationException($"The prefix list from {endpoint} had no content");
    }
}
