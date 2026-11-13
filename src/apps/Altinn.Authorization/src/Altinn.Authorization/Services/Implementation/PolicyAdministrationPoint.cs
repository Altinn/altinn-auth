using System.Net;
using Altinn.Authorization.Xacml;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The Policy Administration Point responsible for storing app policies
    /// </summary>
    public class PolicyAdministrationPoint : IPolicyAdministrationPoint
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly HybridCache _cache;
        private readonly ILogger<PolicyAdministrationPoint> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyAdministrationPoint"/> class.
        /// </summary>
        /// <param name="policyRepository">The policy repository (blob storage)</param>
        /// <param name="cache">The cache <see cref="PolicyRetrievalPoint"/> serves policies from</param>
        /// <param name="logger">The logger</param>
        public PolicyAdministrationPoint(IPolicyRepository policyRepository, HybridCache cache, ILogger<PolicyAdministrationPoint> logger)
        {
            _policyRepository = policyRepository;
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> WritePolicyAsync(string org, string app, Stream fileStream)
        {
            if (fileStream == null)
            {
                throw new ArgumentException("The policy file can not be null");
            }

            string filePath = XacmlPolicyHelper.GetAltinnAppsPolicyPath(org, app);
            Response<BlobContentInfo> response = await _policyRepository.WritePolicyAsync(filePath, fileStream);

            bool stored = response?.GetRawResponse()?.Status == (int)HttpStatusCode.Created;
            if (stored)
            {
                // PolicyRetrievalPoint may hold the previous policy for PolicyCacheTimeout minutes. Removing the entry
                // clears this process, and the shared tier once one is configured; other pods keep their copy until it expires.
                // The policy is stored at this point, so a cache failure must not turn the result into an error.
                // RemoveAsync can pass on errors from the distributed tier.
                try
                {
                    await _cache.RemoveAsync(PolicyRetrievalPoint.GetCacheKey(filePath));
                }
                catch (Exception ex)
                {
                    // org and app come from the query string, so they stay out of the log entry.
                    _logger.LogWarning(ex, "Could not remove the cached policy after storing a new one");
                }
            }

            return stored;
        }
    }
}
