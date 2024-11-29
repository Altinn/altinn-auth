using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.Oed;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Service implementation for OED Role Assignment integration
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OedRoleAssignmentWrapper : IOedRoleAssignmentWrapper
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly OedAuthzClient _oedAuthz;
        private readonly IMaskinportenService _maskinportenService;
        private readonly OedAuthzMaskinportenClientSettings _maskinportenClientSettings;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesWrapper"/> class
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="oedAuthzClient">the client handler for OED api</param>
        /// <param name="maskinportenService">the maskinporten service</param>
        /// <param name="maskinportenClientSettings">The maskinporten client settings</param>
        public OedRoleAssignmentWrapper(ILogger<OedRoleAssignmentWrapper> logger, OedAuthzClient oedAuthzClient, IMaskinportenService maskinportenService, IOptions<OedAuthzMaskinportenClientSettings> maskinportenClientSettings)
        {
            _logger = logger;
            _oedAuthz = oedAuthzClient;
            _maskinportenService = maskinportenService;
            _maskinportenClientSettings = maskinportenClientSettings.Value;
        }

        /// <inheritdoc/>
        public async Task<List<OedRoleAssignment>> GetOedRoleAssignments(string from, string to)
        {
            try
            {
                TokenResponse tokenResponse = await _maskinportenService.GetToken(_maskinportenClientSettings.EncodedJwk, _maskinportenClientSettings.Environment, _maskinportenClientSettings.ClientId, _maskinportenClientSettings.Scope, string.Empty);
                _oedAuthz.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

                OedRoleAssignments oedRoleAssignmentResponse = new() { RoleAssignments = new List<OedRoleAssignment>() };
                OedRoleAssignmentRequest oedRoleAssignmentRequest = new OedRoleAssignmentRequest
                {
                    From = from,
                    To = to
                };

                StringContent requestBody = new StringContent(JsonSerializer.Serialize(oedRoleAssignmentRequest), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _oedAuthz.Client.PostAsync("api/v1/pip", requestBody);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    oedRoleAssignmentResponse = JsonSerializer.Deserialize<OedRoleAssignments>(responseContent, _serializerOptions);
                }
                else
                {
                    _logger.LogError("OedAuthz // OedRoleAssignmentWrapper // GetOedRoleAssignments // Failed // Unexpected Exception // Unexpected HttpStatusCode: {statusCode}\n {responseContent}", response.StatusCode, responseContent);
                }

                return oedRoleAssignmentResponse.RoleAssignments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OedAuthz // OedRoleAssignmentWrapper // GetOedRoleAssignments // Failed // Unexpected Exception");
                throw;
            }
        }
    }
}