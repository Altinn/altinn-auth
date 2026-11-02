using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ABAC.Xacml;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// The Policy Retrieval point responsible to find the correct policy
    /// based on the context Request
    /// </summary>
    public class PolicyRetrievalPoint : IPolicyRetrievalPoint
    {
        private static readonly string[] PolicyTag = ["policy"];

        private readonly IPolicyFactory _policyFactory;
        private readonly HybridCache _cache;
        private readonly HybridCacheEntryOptions _cacheEntryOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyRetrievalPoint"/> class.
        /// </summary>
        /// <param name="policyFactory">The policy factory</param>
        /// <param name="cache">The cache</param>
        /// <param name="cacheConfig">The cache config settings</param>
        public PolicyRetrievalPoint(IPolicyFactory policyFactory, HybridCache cache, IOptions<CacheConfig> cacheConfig)
        {
            _policyFactory = policyFactory;
            _cache = cache;
            _cacheEntryOptions = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(cacheConfig.Value.PolicyCacheTimeout) };
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(XacmlContextRequest request, CancellationToken cancellationToken = default)
        {
            string policyPath = PolicyHelper.GetPolicyPath(request);
            return await GetPolicyInternalAsync(policyPath, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(string org, string app, CancellationToken cancellationToken = default)
        {
            string policyPath = PolicyHelper.GetAltinnAppsPolicyPath(org, app);
            return await GetPolicyInternalAsync(policyPath, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(string resourceRegistryId, CancellationToken cancellationToken = default)
        {
            string policyPath = PolicyHelper.GetResourceRegistryPolicyPath(resourceRegistryId);
            return await GetPolicyInternalAsync(policyPath, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
        {
            return await GetPolicyInternalAsync(policyPath, version, cancellationToken);
        }

        private async Task<XacmlPolicy> GetPolicyInternalAsync(string policyPath, string version = "", CancellationToken cancellationToken = default)
        {
            string cacheKey = $"policy:{policyPath}:{version}";
            byte[] cachedDocument = await _cache.GetAsync<byte[]>(cacheKey, cancellationToken);
            if (cachedDocument != null)
            {
                return ParsePolicyDocument(cachedDocument);
            }

            Stream policyBlob = string.IsNullOrEmpty(version) ?
                await _policyFactory.Create(policyPath).GetPolicyAsync(cancellationToken) :
                await _policyFactory.Create(policyPath).GetPolicyVersionAsync(version, cancellationToken);

            byte[] policyDocument;
            using (policyBlob)
            {
                if (policyBlob.Length == 0)
                {
                    // An empty document means the blob or the requested version does not exist. Caching that would
                    // shadow a version created later in the same cache window.
                    return null;
                }

                policyBlob.Position = 0;
                using MemoryStream buffer = new MemoryStream();
                await policyBlob.CopyToAsync(buffer, cancellationToken);
                policyDocument = buffer.ToArray();
            }

            // Parse before caching, so a document that fails to parse is never cached and the next
            // read fetches a fresh copy.
            XacmlPolicy policy = ParsePolicyDocument(policyDocument);
            await _cache.SetAsync(cacheKey, policyDocument, _cacheEntryOptions, PolicyTag, cancellationToken);

            return policy;
        }

        // The cache holds the policy document rather than the parsed policy: callers mutate the XacmlPolicy they get
        // back, so every read needs its own instance, and XacmlPolicy does not survive the cache serializer.
        private static XacmlPolicy ParsePolicyDocument(byte[] policyDocument)
        {
            using MemoryStream stream = new MemoryStream(policyDocument, writable: false);
            return PolicyHelper.ParsePolicy(stream);
        }
    }
}
