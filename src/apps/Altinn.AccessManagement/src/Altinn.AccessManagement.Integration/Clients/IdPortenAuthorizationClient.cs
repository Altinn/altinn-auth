using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;
using Altinn.AccessManagement.Integration.Configuration;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Integration.Clients;

/// <summary>
/// Client for getting IdPorten authorizations from IdPorten API
/// </summary>
[ExcludeFromCodeCoverage]
public class IdPortenAuthorizationClient : IIdPortenAuthorizationClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly IMaskinportenService _maskinportenService;
    private readonly IdPortenAuthorizationMaskinportenClientSettings _maskinportenClientSettings;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="PartiesClient"/> class
    /// </summary>
    /// <param name="httpClient">HttpClient from default httpclientfactory</param>
    /// <param name="logger">the logger</param>
    /// <param name="maskinportenService">An instance of the Maskinporten service.</param>
    /// <param name="maskinportenClientSettings">The Maskinporten client settings.</param>
    public IdPortenAuthorizationClient(
        HttpClient httpClient,
        ILogger<IdPortenAuthorizationClient> logger,
        IMaskinportenService maskinportenService,
        IOptions<IdPortenAuthorizationMaskinportenClientSettings> maskinportenClientSettings)
    {
        _logger = logger;
        _client = httpClient;
        _maskinportenService = maskinportenService;
        _maskinportenClientSettings = maskinportenClientSettings.Value;
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <inheritdoc/>
    public async Task<IdPortenClientResult<List<IdPortenAuthorization>>> GetIdPortenAuthorizations(string ssn, CancellationToken cancellationToken)
    {
        try
        {
            UriBuilder uriBuilder = new UriBuilder($"{_maskinportenClientSettings.IdPortenApiEndpoint}api-provider/authorizations");

            var request = await CreateRequest(ssn, uriBuilder.Uri.ToString(), HttpMethod.Post, cancellationToken);

            HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var value = JsonSerializer.Deserialize<List<IdPortenAuthorization>>(responseContent, _serializerOptions);
                return new(response.StatusCode, value);
            }

            _logger.LogError("AccessManagement // IdPortenAuthorizationClient // GetIdPortenAuthorizations // Unexpected HttpStatusCode: {StatusCode}\n {ResponseContent}", response.StatusCode, responseContent);
            return new(response.StatusCode, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // IdPortenAuthorizationClient // GetIdPortenAuthorizations // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdPortenClientResult<bool>> DeleteIdPortenAuthorization(string ssn, string id, CancellationToken cancellationToken)
    {
        try
        {
            UriBuilder uriBuilder = new UriBuilder($"{_maskinportenClientSettings.IdPortenApiEndpoint}api-provider/authorizations/{id}");

            var request = await CreateRequest(ssn, uriBuilder.Uri.ToString(), HttpMethod.Delete, cancellationToken);

            HttpResponseMessage response = await _client.SendAsync(request, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new(response.StatusCode, true);
            }

            _logger.LogError("AccessManagement // IdPortenAuthorizationClient // DeleteIdPortenAuthorization // Unexpected HttpStatusCode: {StatusCode}\n {ResponseContent}", response.StatusCode, responseContent);
            return new(response.StatusCode, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // IdPortenAuthorizationClient // DeleteIdPortenAuthorization // Exception");
            throw;
        }
    }

    private async Task<HttpRequestMessage> CreateRequest(string ssn, string uri, HttpMethod method, CancellationToken cancellationToken)
    {
        var body = new { pid = ssn };
        string json = JsonSerializer.Serialize(body, _serializerOptions);
        StringContent requestBody = new StringContent(json, Encoding.UTF8, "application/json");

        TokenResponse tokenResponse = await _maskinportenService.GetToken(_maskinportenClientSettings.EncodedJwk, _maskinportenClientSettings.Environment, _maskinportenClientSettings.ClientId, _maskinportenClientSettings.Scope, string.Empty);
        var request = new HttpRequestMessage(method, uri)
        {
            Content = requestBody,
            Headers = 
            { 
                Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken)
            }
        };
        
        return request;
    }
}
