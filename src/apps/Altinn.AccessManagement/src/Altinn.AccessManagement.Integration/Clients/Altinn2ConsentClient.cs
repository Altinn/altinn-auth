using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Extensions;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.SblBridge;
using Altinn.AccessManagement.Integration.Configuration;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Register.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Integration.Clients;

/// <summary>
/// Proxy implementation for consent
/// </summary>
[ExcludeFromCodeCoverage]
public class Altinn2ConsentClient : IAltinn2ConsentClient
{
    private readonly SblBridgeSettings _sblBridgeSettings;
    private readonly ILogger _logger;
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PlatformSettings _platformSettings;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="PartiesClient"/> class
    /// </summary>
    /// <param name="httpClient">HttpClient from default httpclientfactory</param>
    /// <param name="sblBridgeSettings">the sbl bridge settings</param>
    /// <param name="logger">the logger</param>
    /// <param name="httpContextAccessor">handler for http context</param>
    /// <param name="platformSettings">the platform setttings</param>
    /// <param name="accessTokenGenerator">An instance of the AccessTokenGenerator service.</param>
    public Altinn2ConsentClient(
        HttpClient httpClient, 
        IOptions<SblBridgeSettings> sblBridgeSettings, 
        ILogger<Altinn2ConsentClient> logger, 
        IHttpContextAccessor httpContextAccessor, 
        IOptions<PlatformSettings> platformSettings,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _sblBridgeSettings = sblBridgeSettings.Value;
        _logger = logger;
        httpClient.BaseAddress = new Uri(platformSettings.Value.ApiAuthorizationEndpoint);
        httpClient.DefaultRequestHeaders.Add(platformSettings.Value.SubscriptionKeyHeaderName, platformSettings.Value.SubscriptionKey);
        _client = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _platformSettings = platformSettings.Value;
        _accessTokenGenerator = accessTokenGenerator;
        _serializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> GetAltinn2ConsentListForMigration(int numberOfConsentsToReturn, int? status, bool onlyGetExpired, CancellationToken cancellationToken = default)
    {
        try
        {
            string endpointUrl = $"getconsentlistformigration?numberOfConsentsToReturn={numberOfConsentsToReturn}&status={status}&onlyGetExpired={onlyGetExpired}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);
            var accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "access-management");

            HttpResponseMessage response = await _client.GetAsync(token, endpointUrl, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonSerializer.Deserialize<List<Guid>>(responseContent, _serializerOptions);
            }

            _logger.LogError("AccessManagement // Altinn2ConsentClient // GetConsent // Unexpected HttpStatusCode: {StatusCode}\n {responseContent}", response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // Altinn2ConsentClient // GetConsent // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Altinn2ConsentRequest>> GetMultipleAltinn2Consents(List<string> consentList, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = string.Join("&", consentList.Select(g => "consentList=" + WebUtility.UrlEncode(g.ToString())));

            string endpointUrl = $"getmultipleconsents?getmultipleconsents?{query}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);
            var accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "access-management");

            HttpResponseMessage response = await _client.GetAsync(token, endpointUrl, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                List<Altinn2ConsentRequest> altinn2Consents = JsonSerializer.Deserialize<List<Altinn2ConsentRequest>>(responseContent, _serializerOptions);

                return altinn2Consents;
            }

            _logger.LogError("AccessManagement // Altinn2ConsentClient // GetConsent // Unexpected HttpStatusCode: {StatusCode}\n {responseContent}", response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // Altinn2ConsentClient // GetConsent // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Altinn2ConsentRequest> GetAltinn2Consent(Guid consentGuid, CancellationToken cancellationToken = default)
    {
        try
        {
            string endpointUrl = $"consent?consentGuid={consentGuid}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);
            var accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "access-management");

            HttpResponseMessage response = await _client.GetAsync(token, endpointUrl, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Altinn2ConsentRequest altinn2ConsentRequest = JsonSerializer.Deserialize<Altinn2ConsentRequest>(responseContent, _serializerOptions);
                return altinn2ConsentRequest;
            }

            _logger.LogError("AccessManagement // Altinn2ConsentClient // GetConsent // Unexpected HttpStatusCode: {StatusCode}\n {responseContent}", response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // Altinn2ConsentClient // GetConsent // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAltinn2ConsentMigrateStatus(string consentGuid, int status, CancellationToken cancellationToken = default)
    {
        try
        {
            string endpointUrl = $"updateconsentmigratestatus?consentGuid={consentGuid}&status={status}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);
            var accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "access-management");
            StringContent requestBody = new StringContent(JsonSerializer.Serialize(consentGuid), Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _client.PostAsync(token, endpointUrl, requestBody, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return bool.Parse(responseContent);
            }

            _logger.LogError("AccessManagement // Altinn2ConsentClient // UpdateConsentStatus // Unexpected HttpStatusCode: {StatusCode}\n {responseContent}", response.StatusCode, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AccessManagement // Altinn2ConsentClient // UpdateConsentStatus // Exception");
            throw;
        }
    }
}
