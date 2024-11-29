using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Profile.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// Wrapper for the profile api
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ProfileWrapper : IProfile
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private readonly ProfileClient _profileClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileWrapper"/> class
        /// </summary>
        /// <param name="profileClient">the client handler for profile api in Bridge</param>
        /// <param name="logger">The logger</param>
        public ProfileWrapper(ProfileClient profileClient, ILogger<ProfileWrapper> logger)
        {
            _profileClient = profileClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<UserProfile> GetUserProfile(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                string endpointUrl = $"internal/user";

                var response = await _profileClient.Client.PostAsJsonAsync(endpointUrl, new { UserId = userId }, cancellationToken);
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonSerializer.Deserialize<UserProfile>(responseContent, _serializerOptions);
                }

                _logger.LogError("ProfileAPI // ProfileWrapper // GetUserProfile // Failed // Unexpected HttpStatusCode: {statusCode}\n {responseContent}", response.StatusCode, responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProfileAPI // ProfileWrapper // GetUserProfile // Failed // Unexpected Exception");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<UserProfile> GetUserProfileByPersonId(string personId, CancellationToken cancellationToken = default)
        {
            try
            {
                string endpointUrl = $"internal/user";

                var response = await _profileClient.Client.PostAsJsonAsync(endpointUrl, new { Ssn = personId }, cancellationToken);
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonSerializer.Deserialize<UserProfile>(responseContent, _serializerOptions);
                }

                _logger.LogError("ProfileAPI // ProfileWrapper // GetUserProfile // Failed // Unexpected HttpStatusCode: {statusCode}\n {responseContent}", response.StatusCode, responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProfileAPI // ProfileWrapper // GetUserProfile // Failed // Unexpected Exception");
                throw;
            }
        }
    }
}
