using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Extensions;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Register.Models;
using AltinnCore.Authentication.Utils;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services
{
    /// <summary>
    /// Handles register service
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RegisterService : IRegisterService
    {
        private static readonly string[] PartyTags = ["party"];

        // The cache is read and written in two steps instead of through the factory of GetOrCreateAsync: a party list is
        // fetched for the ids that missed in one call, and a failed lookup must not be cached.
        private static readonly HybridCacheEntryOptions ReadOptions = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };
        private static readonly HybridCacheEntryOptions WriteOptions = new() { Expiration = TimeSpan.FromMinutes(10) };

        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly GeneralSettings _generalSettings;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly ILogger<IRegisterService> _logger;
        private readonly HybridCache _cache;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterService"/> class.
        /// </summary>
        public RegisterService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            IAccessTokenGenerator accessTokenGenerator,
            IOptions<GeneralSettings> generalSettings,
            IOptions<PlatformSettings> platformSettings,
            ILogger<IRegisterService> logger,
            HybridCache cache)
        {
            httpClient.BaseAddress = new Uri(platformSettings.Value.ApiRegisterEndpoint);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _generalSettings = generalSettings.Value;
            _accessTokenGenerator = accessTokenGenerator;
            _logger = logger;
            _cache = cache;
        }

        /// <inheritdoc/>
        public async Task<Party> GetParty(int partyId, CancellationToken cancellationToken = default)
        {
            return await GetPartiesAsync(new List<int> { partyId }, false, cancellationToken)
                .ContinueWith(task => task.Result.FirstOrDefault(), cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<Party>> GetPartiesAsync(List<Guid> partyUuids, bool includeSubunits = false, CancellationToken cancellationToken = default)
        {
            List<Party> parties = new List<Party>();
            List<Guid> partyUuidsNotInCache = new List<Guid>();

            foreach (Guid partyUuid in partyUuids.Distinct())
            {
                Party party = await GetCachedAsync(GetPartyUuidCacheKey(partyUuid, includeSubunits), cancellationToken);
                if (party != null)
                {
                    parties.Add(party);
                }
                else
                {
                    partyUuidsNotInCache.Add(partyUuid);
                }
            }

            if (partyUuidsNotInCache.Count == 0)
            {
                return parties;
            }

            string endpointUrl = $"parties/partylistbyuuid?fetchSubUnits={includeSubunits}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName);
            string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
            StringContent requestBody = new StringContent(JsonSerializer.Serialize(partyUuidsNotInCache), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync(endpointUrl, requestBody, token, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            List<Party> remainingParties = new();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                remainingParties = JsonSerializer.Deserialize<List<Party>>(responseContent, _serializerOptions);
            }

            if (remainingParties.Count > 0)
            {
                foreach (Party party in remainingParties)
                {
                    if (party?.PartyId != null)
                    {
                        parties.Add(party);
                        await PutInCacheAsync(party, includeSubunits, cancellationToken);
                    }
                }
            }

            return parties;
        }

        /// <inheritdoc/>
        public async Task<List<Party>> GetPartiesAsync(List<int> partyIds, bool includeSubunits = false, CancellationToken cancellationToken = default)
        {
            List<Party> parties = new List<Party>();
            List<int> partyIdsNotInCache = new List<int>();

            foreach (int partyId in partyIds.Distinct())
            {
                Party party = await GetCachedAsync(GetPartyIdCacheKey(partyId, includeSubunits), cancellationToken);
                if (party != null)
                {
                    parties.Add(party);
                }
                else
                {
                    partyIdsNotInCache.Add(partyId);
                }
            }

            if (partyIdsNotInCache.Count == 0)
            {
                return parties;
            }

            string endpointUrl = $"parties/partylist?fetchSubUnits={includeSubunits}";
            string token = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName);
            string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");
            StringContent requestBody = new StringContent(JsonSerializer.Serialize(partyIdsNotInCache), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _client.PostAsync(endpointUrl, requestBody, token, accessToken, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            List<Party> remainingParties = new();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                remainingParties = JsonSerializer.Deserialize<List<Party>>(responseContent, _serializerOptions);
            }

            if (remainingParties.Count > 0)
            {
                foreach (Party party in remainingParties)
                {
                    if (party?.PartyId != null)
                    {
                        parties.Add(party);
                        await PutInCacheAsync(party, includeSubunits, cancellationToken);
                    }
                }
            }

            return parties;
        }

        /// <inheritdoc/>
        public async Task<Party> PartyLookup(string orgNo, string person, CancellationToken cancellationToken = default)
        {
            string cacheKey;
            PartyLookup partyLookup;

            if (!string.IsNullOrWhiteSpace(orgNo))
            {
                cacheKey = $"party:org:{orgNo}";
                partyLookup = new PartyLookup { OrgNo = orgNo };
            }
            else if (!string.IsNullOrWhiteSpace(person))
            {
                cacheKey = $"party:fnr:{person}";
                partyLookup = new PartyLookup { Ssn = person };
            }
            else
            {
                return null;
            }

            Party party = await GetCachedAsync(cacheKey, cancellationToken);
            if (party == null)
            {
                string endpointUrl = "parties/lookup";

                string bearerToken = JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName);
                string accessToken = _accessTokenGenerator.GenerateAccessToken("platform", "authorization");

                StringContent content = new StringContent(JsonSerializer.Serialize(partyLookup));
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                HttpResponseMessage response = await _client.PostAsync(endpointUrl, content, bearerToken, accessToken, cancellationToken);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    party = JsonSerializer.Deserialize<Party>(responseContent, _serializerOptions);
                    if (party != null)
                    {
                        await _cache.SetAsync(cacheKey, party, WriteOptions, PartyTags, cancellationToken);
                    }
                }
                else
                {
                    string reason = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("// RegisterService // PartyLookup // Failed to lookup party in platform register. Response {Response}. \n Reason {Reason}.", response, reason);

                    throw await PlatformHttpException.CreateAsync(response);
                }
            }

            return party;
        }

        private static string GetPartyIdCacheKey(int partyId, bool includeSubunits) => $"party:{partyId}|inclSubunits:{includeSubunits}";

        private static string GetPartyUuidCacheKey(Guid partyUuid, bool includeSubunits) => $"party-uuid:{partyUuid}|inclSubunits:{includeSubunits}";

        private ValueTask<Party> GetCachedAsync(string cacheKey, CancellationToken cancellationToken)
            => _cache.GetOrCreateAsync<Party>(cacheKey, static _ => default, ReadOptions, cancellationToken: cancellationToken);

        private async Task PutInCacheAsync(Party party, bool includeSubunits, CancellationToken cancellationToken)
        {
            await _cache.SetAsync(GetPartyIdCacheKey(party.PartyId, includeSubunits), party, WriteOptions, PartyTags, cancellationToken);
            if (party.PartyUuid.HasValue)
            {
                await _cache.SetAsync(GetPartyUuidCacheKey(party.PartyUuid.Value, includeSubunits), party, WriteOptions, PartyTags, cancellationToken);
            }
        }
    }
}
