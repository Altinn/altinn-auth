using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Extensions;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Services.Implementation
{
    /// <summary>
    /// Wrapper for resource registry
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ResourceRegistryWrapper : IResourceRegistry
    {
        private readonly ResourceRegistryClient _resourceRegistry;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IMemoryCache _memoryCache;
        private readonly GeneralSettings _generalSettings;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegistryWrapper"/> class.
        /// </summary>
        public ResourceRegistryWrapper(ResourceRegistryClient resourceRegistryClient, IAccessTokenGenerator accessTokenGenerator, IMemoryCache memoryCache, IOptions<GeneralSettings> settings)
        {
            _resourceRegistry = resourceRegistryClient;
            _generalSettings = settings.Value;
            _memoryCache = memoryCache;
            _accessTokenGenerator = accessTokenGenerator;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            string cacheKey = "r:" + resourceId;
            if (!_memoryCache.TryGetValue(cacheKey, out ServiceResource resource))
            {
                string apiurl = $"resource/{resourceId}";
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    resource = await response.Content.ReadFromJsonAsync<ServiceResource>(_jsonOptions, cancellationToken);
                    PutInCache(cacheKey, _generalSettings.PolicyCacheTimeout, resource);
                }
            }

            return resource;
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetResourcePolicyAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            string cacheKey = "resourcepolicy:" + resourceId;
            if (!_memoryCache.TryGetValue(cacheKey, out XacmlPolicy policy))
            {
                string apiurl = $"resource/{resourceId}/policy";
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream policyBlob = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using (policyBlob)
                    {
                        policy = policyBlob.Length > 0 ? PolicyHelper.ParsePolicy(policyBlob) : null;
                    }

                    PutInCache(cacheKey, _generalSettings.PolicyCacheTimeout, policy);
                }
            }

            return policy;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessListResourceMembershipWithActionFilterDto>> GetMembershipsForResourceForParty(PartyUrn partyUrn, ResourceIdUrn resourceIdUrn, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"AccListMemb|{partyUrn}|{resourceIdUrn}";
            if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<AccessListResourceMembershipWithActionFilterDto> memberships))
            {
                string apiurl = $"access-lists/memberships?party={partyUrn}&resource={resourceIdUrn}";
                string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, platformAccessToken: accessToken, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ListObject<AccessListResourceMembershipWithActionFilterDto> result = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipWithActionFilterDto>>(_jsonOptions, cancellationToken);
                    memberships = result.Items;
                    PutInCache(cacheKey, _generalSettings.PolicyCacheTimeout, memberships);
                }
            }

            return memberships;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessListInfoDto>> GetMembershipsForParty(PartyUrn.PartyUuid partyUrn, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"AccListMemb|{partyUrn}";
            if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<AccessListInfoDto> memberships))
            {
                string apiurl = $"access-lists/get-by-member?party={partyUrn}";
                string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, platformAccessToken: accessToken, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    memberships = await response.Content.ReadFromJsonAsync<IEnumerable<AccessListInfoDto>>(_jsonOptions, cancellationToken);
                    PutInCache(cacheKey, _generalSettings.PolicyCacheTimeout, memberships);
                }
            }

            return memberships;
        }

        private void PutInCache(string cachekey, int cacheTimeout, object cacheObject)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, cacheTimeout, 0));

            _memoryCache.Set(cachekey, cacheObject, cacheEntryOptions);
        }
    }
}
