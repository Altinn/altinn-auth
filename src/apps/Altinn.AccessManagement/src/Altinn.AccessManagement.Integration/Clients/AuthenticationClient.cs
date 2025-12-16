using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Extensions;
using Altinn.AccessManagement.Core.Models.Authentication;
using Altinn.AccessManagement.Integration.Configuration;
using AltinnCore.Authentication.Utils;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DefaultRight = Altinn.AccessManagement.Core.Models.Authentication.DefaultRight;

namespace Altinn.AccessManagement.Integration.Clients
{
    /// <summary>
    /// A client for authentication actions in Altinn Platform.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AuthenticationClient : IAuthenticationClient
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _client;
        private readonly PlatformSettings _platformSettings;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationClient"/> class
        /// </summary>
        /// <param name="platformSettings">The current platform settings.</param>
        /// <param name="logger">the logger</param>
        /// <param name="httpContextAccessor">The http context accessor </param>
        /// <param name="httpClient">A HttpClient provided by the HttpClientFactory.</param>
        public AuthenticationClient(
            IOptions<PlatformSettings> platformSettings,
            ILogger<AuthenticationClient> logger,
            IHttpContextAccessor httpContextAccessor,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _platformSettings = platformSettings.Value;
            httpClient.BaseAddress = new Uri(platformSettings.Value.ApiAuthenticationEndpoint);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client = httpClient;
        }

        /// <inheritdoc />
        public async Task<SystemUser> GetSystemUser(int partyId, string systemUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                string endpointUrl = $"systemuser/{partyId}/{systemUserId}";
                string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);

                HttpResponseMessage response = await _client.GetAsync(endpointUrl, token, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SystemUser systemUser = await response.Content.ReadFromJsonAsync<SystemUser>(_serializerOptions, cancellationToken);

                    // The endpoint is not using the partyId input so added a check here to ensure the partyId is correct.
                    if (int.TryParse(systemUser?.PartyId, out int ownerParsed) && ownerParsed != partyId)
                    {
                        return null;
                    }
                    else
                    {
                        return systemUser;
                    }
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Fetching system user failed with status code: {statusCode}, details: {responseContent}", response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccessManagement // AuthenticationClient // GetSystemUser // Exception");
                throw;
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<List<DefaultRight>> GetDefaultRightsForRegisteredSystem(string systemId, CancellationToken cancellationToken = default)
        {
            List<DefaultRight> result = new List<DefaultRight>();

            try
            {
                string endpointUrl = $"systemregister/{systemId}/rights?useoldformatforapp=true";
                string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _platformSettings.JwtCookieName);

                HttpResponseMessage response = await _client.GetAsync(endpointUrl, token, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return await response.Content.ReadFromJsonAsync<List<DefaultRight>>(_serializerOptions, cancellationToken);
                }
                else
                {
                    string content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Fetching system user default rights failed with status code: {statusCode}, content: {content}", response.StatusCode, content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AccessManagement // AuthenticationClient // GetDefaultRightsForRegisteredSystem // Exception");
                throw;
            }

            return result;
        }
    }
}
