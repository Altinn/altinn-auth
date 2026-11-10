using System.ComponentModel;
using System.Net;
using System.Text.Json;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.Api.Contracts.ResourceRegistry;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Extensions;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Services.Implementation
{
    /// <summary>
    /// Wrapper for resource registry
    /// </summary>
    public class ResourceRegistryWrapper : IResourceRegistry
    {
        private static readonly string[] ResourceTags = ["resource"];
        private static readonly string[] PolicyTags = ["policy"];
        private static readonly string[] AccessListTags = ["access-list"];

        private readonly ResourceRegistryClient _resourceRegistry;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly HybridCache _cache;
        private readonly HybridCacheEntryOptions _readOptions;
        private readonly HybridCacheEntryOptions _writeOptions;
        private readonly HybridCacheEntryOptions _policyReadOptions;
        private readonly HybridCacheEntryOptions _policyWriteOptions;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegistryWrapper"/> class.
        /// </summary>
        public ResourceRegistryWrapper(ResourceRegistryClient resourceRegistryClient, IAccessTokenGenerator accessTokenGenerator, HybridCache cache, IOptions<GeneralSettings> settings)
        {
            _resourceRegistry = resourceRegistryClient;
            _cache = cache;
            _accessTokenGenerator = accessTokenGenerator;
            TimeSpan expiration = TimeSpan.FromMinutes(settings.Value.PolicyCacheTimeout);

            // The cache is read and written in two steps instead of through the factory of GetOrCreateAsync: the factory
            // cannot decline to cache its result, and a non-OK response or an empty policy must not be cached. The read
            // options carry the expiration too, since a hit from the distributed tier takes its local lifetime from them.
            _readOptions = new HybridCacheEntryOptions { Expiration = expiration, Flags = HybridCacheEntryFlags.DisableUnderlyingData };
            _writeOptions = new HybridCacheEntryOptions { Expiration = expiration };

            // The parsed policy never leaves this process: XacmlPolicy serializes but cannot be deserialized, so the
            // distributed tier would hand every other reader a document it cannot read.
            _policyReadOptions = new HybridCacheEntryOptions { Expiration = expiration, Flags = HybridCacheEntryFlags.DisableUnderlyingData | HybridCacheEntryFlags.DisableDistributedCache };
            _policyWriteOptions = new HybridCacheEntryOptions { Expiration = expiration, Flags = HybridCacheEntryFlags.DisableDistributedCache };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        /// <inheritdoc/>
        public async Task<ServiceResource> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            string cacheKey = "resource:" + resourceId;
            CachedResource cached = await GetCachedAsync<CachedResource>(cacheKey, _readOptions, cancellationToken);
            if (cached != null)
            {
                return cached.Resource;
            }

            ServiceResource resource = null;
            string apiurl = $"resource/{resourceId}";
            HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                resource = await response.Content.ReadFromJsonAsync<ServiceResource>(_jsonOptions, cancellationToken);
                await _cache.SetAsync(cacheKey, new CachedResource(resource), _writeOptions, ResourceTags, cancellationToken);
            }

            return resource;
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetResourcePolicyAsync(string resourceId, CancellationToken cancellationToken = default)
        {
            string cacheKey = "resource-policy:" + resourceId;
            CachedResourcePolicy cached = await GetCachedAsync<CachedResourcePolicy>(cacheKey, _policyReadOptions, cancellationToken);
            if (cached != null)
            {
                return cached.Policy;
            }

            XacmlPolicy policy = null;
            string apiurl = $"resource/{resourceId}/policy";
            HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream policyBlob = await response.Content.ReadAsStreamAsync(cancellationToken);
                using (policyBlob)
                {
                    policy = policyBlob.Length > 0 ? PolicyHelper.ParsePolicy(policyBlob) : null;
                }

                if (policy != null)
                {
                    await _cache.SetAsync(cacheKey, new CachedResourcePolicy(policy), _policyWriteOptions, PolicyTags, cancellationToken);
                }
            }

            return policy;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessListResourceMembershipWithActionFilterDto>> GetMembershipsForResourceForParty(PartyUrn partyUrn, ResourceIdUrn resourceIdUrn, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"access-list:{partyUrn}|{resourceIdUrn}";
            IEnumerable<AccessListResourceMembershipWithActionFilterDto> memberships = await GetCachedAsync<IEnumerable<AccessListResourceMembershipWithActionFilterDto>>(cacheKey, _readOptions, cancellationToken);
            if (memberships == null)
            {
                string apiurl = $"access-lists/memberships?party={partyUrn}&resource={resourceIdUrn}";
                string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, platformAccessToken: accessToken, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ListObject<AccessListResourceMembershipWithActionFilterDto> result = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipWithActionFilterDto>>(_jsonOptions, cancellationToken);
                    memberships = result.Items;
                    await _cache.SetAsync(cacheKey, memberships, _writeOptions, AccessListTags, cancellationToken);
                }
            }

            return memberships;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessListInfoDto>> GetMembershipsForParty(PartyUrn.PartyUuid partyUrn, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"access-list:{partyUrn}";
            IEnumerable<AccessListInfoDto> memberships = await GetCachedAsync<IEnumerable<AccessListInfoDto>>(cacheKey, _readOptions, cancellationToken);
            if (memberships == null)
            {
                string apiurl = $"access-lists/get-by-member?party={partyUrn}";
                string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
                HttpResponseMessage response = await _resourceRegistry.Client.GetAsync(apiurl, platformAccessToken: accessToken, cancellationToken: cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    memberships = await response.Content.ReadFromJsonAsync<IEnumerable<AccessListInfoDto>>(_jsonOptions, cancellationToken);
                    await _cache.SetAsync(cacheKey, memberships, _writeOptions, AccessListTags, cancellationToken);
                }
            }

            return memberships;
        }

        private ValueTask<T> GetCachedAsync<T>(string cacheKey, HybridCacheEntryOptions options, CancellationToken cancellationToken)
            => _cache.GetOrCreateAsync<T>(cacheKey, static _ => default, options, cancellationToken: cancellationToken);

        /// <summary>
        /// Holds a resource in the cache. The type is marked immutable, so a local hit returns the instance itself instead
        /// of deserializing a copy; a decision reads the resource twice. Callers must not change the returned resource,
        /// as with IMemoryCache before. Unlike the policy, the resource is plain JSON and may use the distributed tier.
        /// </summary>
        [ImmutableObject(true)]
        private sealed class CachedResource
        {
            public CachedResource(ServiceResource resource)
            {
                Resource = resource;
            }

            public ServiceResource Resource { get; }
        }

        /// <summary>
        /// Holds a parsed resource policy in the local cache. The type is immutable, so HybridCache stores and returns
        /// the instance itself instead of a serialized copy, and one parse serves every decision on the resource.
        /// </summary>
        [ImmutableObject(true)]
        private sealed class CachedResourcePolicy
        {
            public CachedResourcePolicy(XacmlPolicy policy)
            {
                Policy = policy;
            }

            public XacmlPolicy Policy { get; }
        }
    }
}
