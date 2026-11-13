using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Xacml;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The Policy Retrieval point responsible to find the correct policy
    /// based on the context Request
    /// </summary>
    public class PolicyRetrievalPoint : IPolicyRetrievalPoint
    {
        private static readonly string[] CacheTags = ["policy"];

        private readonly IPolicyRepository _repository;
        private readonly HybridCache _cache;
        private readonly HybridCacheEntryOptions _readOptions;
        private readonly HybridCacheEntryOptions _writeOptions;
        private readonly IResourceRegistry _resourceRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyRetrievalPoint"/> class.
        /// </summary>
        /// <param name="policyRepository">The policy Repository..</param>
        /// <param name="cache">The cache handler </param>
        /// <param name="settings">The app settings</param>
        /// <param name="resourceRegistry">The regis</param>
        public PolicyRetrievalPoint(IPolicyRepository policyRepository, HybridCache cache, IOptions<GeneralSettings> settings, IResourceRegistry resourceRegistry)
        {
            _repository = policyRepository;
            _cache = cache;
            TimeSpan expiration = TimeSpan.FromMinutes(settings.Value.PolicyCacheTimeout);

            // The cache is read and written in two steps instead of through the factory of GetOrCreateAsync: the factory
            // cannot decline to cache its result, and an empty or unparseable document must not be cached. The read
            // options carry the expiration too, since a hit from the distributed tier takes its local lifetime from them.
            _readOptions = new HybridCacheEntryOptions { Expiration = expiration, Flags = HybridCacheEntryFlags.DisableUnderlyingData };
            _writeOptions = new HybridCacheEntryOptions { Expiration = expiration };
            _resourceRegistry = resourceRegistry;
        }

        /// <summary>
        /// Gets the cache key under which the policy document at <paramref name="policyPath"/> is held.
        /// </summary>
        /// <param name="policyPath">The blob storage path of the policy</param>
        /// <param name="version">The blob storage version, or empty for the current policy</param>
        internal static string GetCacheKey(string policyPath, string version = "") => "policy:" + policyPath + version;

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(XacmlContextRequest request)
        {
            PolicyResourceType policyResourceType = PolicyHelper.GetPolicyResourceType(request, out string resourceId, out string org, out string app);
            if (policyResourceType.Equals(PolicyResourceType.ResourceRegistry))
            {
                return await _resourceRegistry.GetResourcePolicyAsync(resourceId);
            }

            return await GetPolicyAsync(org, app);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(string org, string app)
        {
            string policyPath = XacmlPolicyHelper.GetAltinnAppsPolicyPath(org, app);
            return await GetPolicyInternalAsync(policyPath);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
        {
            return await GetPolicyInternalAsync(policyPath, version, cancellationToken);
        }

        private async Task<XacmlPolicy> GetPolicyInternalAsync(string policyPath, string version = "", CancellationToken cancellationToken = default)
        {
            string cacheKey = GetCacheKey(policyPath, version);
            byte[] cachedDocument = await _cache.GetOrCreateAsync<byte[]>(cacheKey, static _ => default, _readOptions, cancellationToken: cancellationToken);
            if (cachedDocument != null)
            {
                return ParsePolicyDocument(cachedDocument);
            }

            Stream policyBlob = string.IsNullOrEmpty(version) ?
                await _repository.GetPolicyAsync(policyPath, cancellationToken) :
                await _repository.GetPolicyVersionAsync(policyPath, version, cancellationToken);

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
            await _cache.SetAsync(cacheKey, policyDocument, _writeOptions, CacheTags, cancellationToken);

            return policy;
        }

        // The cache holds the policy document rather than the parsed policy: callers mutate the XacmlPolicy they get
        // back, so every read needs its own instance.
        private static XacmlPolicy ParsePolicyDocument(byte[] policyDocument)
        {
            using MemoryStream stream = new MemoryStream(policyDocument, writable: false);
            return XacmlPolicyHelper.ParsePolicy(stream);
        }
    }
}
