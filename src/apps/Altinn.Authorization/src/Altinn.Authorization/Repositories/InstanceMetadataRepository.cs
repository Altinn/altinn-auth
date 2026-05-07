using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Repositories
{
    /// <summary>
    /// Repository for retrieving instance authentication information from the
    /// Storage HTTP API.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class InstanceMetadataRepository : IInstanceMetadataRepository
    {
        private readonly ILogger<InstanceMetadataRepository> logger;
        private readonly HttpClient _storageClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceMetadataRepository"/> class.
        /// </summary>
        /// <param name="logger">the logger</param>
        /// <param name="storageClient">Storage client</param>
        /// <param name="platformSettings">Storage config</param>
        public InstanceMetadataRepository(ILogger<InstanceMetadataRepository> logger, HttpClient storageClient, IOptions<PlatformSettings> platformSettings)
        {
            this.logger = logger;
            _storageClient = storageClient;
            storageClient.BaseAddress = new Uri(platformSettings.Value.ApiStorageEndpoint);
        }

        /// <inheritdoc/>
        public async Task<AuthInfo> GetAuthInfo(string instanceId)
        {
            HttpResponseMessage response = await _storageClient.GetAsync($"instances/{instanceId}/process/authinfo");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AuthInfo>(responseData);
            }
            else
            {
                string reason = await response.Content.ReadAsStringAsync();
                logger.LogError("// InstanceMetadataRepository // GetAuthInfo // Failed to lookup auth info from storage. Response {Response}. \n Reason {Reason}.", response, reason);

                throw await PlatformHttpException.CreateAsync(response);
            }
        }
    }
}
