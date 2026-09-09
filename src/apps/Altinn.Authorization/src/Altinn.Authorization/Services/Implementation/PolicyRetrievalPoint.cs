using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation
{
    /// <summary>
    /// The Policy Retrieval point responsible to find the correct policy
    /// based on the context Request
    /// </summary>
    public class PolicyRetrievalPoint : IPolicyRetrievalPoint
    {
        private readonly IPolicyRepository _repository;
        private readonly IMemoryCache _memoryCache;
        private readonly GeneralSettings _generalSettings;
        private readonly IResourceRegistry _resourceRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyRetrievalPoint"/> class.
        /// </summary>
        /// <param name="policyRepository">The policy Repository..</param>
        /// <param name="memoryCache">The cache handler </param>
        /// <param name="settings">The app settings</param>
        /// <param name="resourceRegistry">The regis</param>
        public PolicyRetrievalPoint(IPolicyRepository policyRepository, IMemoryCache memoryCache, IOptions<GeneralSettings> settings, IResourceRegistry resourceRegistry)
        {
            _repository = policyRepository;
            _memoryCache = memoryCache;
            _generalSettings = settings.Value;
            _resourceRegistry = resourceRegistry;
        }

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
            string policyPath = PolicyHelper.GetAltinnAppsPolicyPath(org, app);
            return await GetPolicyInternalAsync(policyPath);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
        {
            return await GetPolicyInternalAsync(policyPath, version, cancellationToken);
        }

        private async Task<XacmlPolicy> GetPolicyInternalAsync(string policyPath, string version = "", CancellationToken cancellationToken = default)
        {
            string cacheKey = policyPath + version;
            if (_memoryCache.TryGetValue(cacheKey, out byte[] cachedDocument))
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
            PutPolicyDocumentInCache(cacheKey, policyDocument);

            return policy;
        }

        // The cache holds the policy document rather than the parsed policy: callers mutate the XacmlPolicy they get
        // back, so every read needs its own instance.
        private static XacmlPolicy ParsePolicyDocument(byte[] policyDocument)
        {
            using MemoryStream stream = new MemoryStream(policyDocument, writable: false);
            return PolicyHelper.ParsePolicy(stream);
        }

        private void PutPolicyDocumentInCache(string cacheKey, byte[] policyDocument)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, _generalSettings.PolicyCacheTimeout, 0));

            _memoryCache.Set(cacheKey, policyDocument, cacheEntryOptions);
        }
    }
}
