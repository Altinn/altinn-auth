using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Services.Interface;
using AltinnCore.Authentication.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Wrapper for the Altinn Access Management API
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AccessManagementWrapper : IAccessManagementWrapper
    {
        private readonly GeneralSettings _generalSettings;
        private readonly AccessManagementClient _client;
        private readonly ILogger<AccessManagementWrapper> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessManagementWrapper"/> class.
        /// </summary>
        public AccessManagementWrapper(ILogger<AccessManagementWrapper> logger, IOptions<GeneralSettings> generalSettings, AccessManagementClient client, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _client = client;
            _generalSettings = generalSettings.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(DelegationChangeInput input, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.Client.SendAsync(
                    new(HttpMethod.Post, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), "policyinformation/getdelegationchanges"))
                    {
                        Content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, MediaTypeNames.Application.Json)
                    },
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<DelegationChangeExternal>>(_serializerOptions, cancellationToken);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(content == string.Empty ? $"received status code {response.StatusCode}" : content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("AccessManagement // AccessManagementWrapper // GetAllDelegationChanges // Failed // Unexpected Exception // Unexpected HttpStatusCode: {statusCode}\n {responseContent}", ex.StatusCode, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("AccessManagement // AccessManagementWrapper // GetAllDelegationChanges // Failed // Unexpected Exception // Unexpected Message: {message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInput>[] actions)
        {
            var input = new DelegationChangeInput()
            {
                Resource = new List<AttributeMatch>(),
            };

            actions.ToList().ForEach(action => action(input));
            return await GetAllDelegationChanges(input);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), "authorizedparties?includeAltinn2=true"));
            request.Headers.Add("Authorization", "Bearer " + JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName));

            var response = await _client.Client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<IEnumerable<AuthorizedPartyDto>>(_serializerOptions, cancellationToken);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(content == string.Empty ? $"AuthorizedParties received status code {response.StatusCode}" : content);
        }
    }
}
