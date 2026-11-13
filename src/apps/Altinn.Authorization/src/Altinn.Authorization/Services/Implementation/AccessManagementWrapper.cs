using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Enums;
using Altinn.Authorization.Xacml.Constants;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Services.Interface;
using AltinnCore.Authentication.Utils;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation;

/// <summary>
/// Wrapper for the Altinn Access Management API
/// </summary>
[ExcludeFromCodeCoverage]
public class AccessManagementWrapper : IAccessManagementWrapper
{
    private static readonly string[] RolesTags = ["roles"];
    private static readonly string[] DelegationTags = ["delegations"];

    // The cache is read and written in two steps instead of through the factory of GetOrCreateAsync: the factory
    // cannot decline to cache its result, and a non-OK or empty response must not be cached.
    private static readonly HybridCacheEntryOptions ReadOptions = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };
    private static readonly HybridCacheEntryOptions DelegationCacheOptions = new() { Expiration = TimeSpan.FromMinutes(5) };

    private readonly GeneralSettings _generalSettings;
    private readonly AccessManagementClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _writeOptions;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessManagementWrapper"/> class.
    /// </summary>
    public AccessManagementWrapper(IOptions<GeneralSettings> generalSettings, AccessManagementClient client, IHttpContextAccessor httpContextAccessor, HybridCache cache)
    {
        _client = client;
        _generalSettings = generalSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
        _writeOptions = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(_generalSettings.RoleCacheTimeout) };
    }

    /// <summary>
    /// Gets the cache key under which <see cref="GetRolesAndAccessPackages"/> holds the answer for the given parties.
    /// </summary>
    public static string GetRolesAndAccessPackagesCacheKey(Guid to, Guid from, AccessRestriction accessRestriction, string viaPartyOrganizationNumber)
        => $"roles:f:{from}|t:{to}|ac:{accessRestriction}|vp:{viaPartyOrganizationNumber}";

    /// <inheritdoc/>
    public async Task<IEnumerable<DelegationChangeDto>> GetAllDelegationChanges(DelegationChangeInputDto input, CancellationToken cancellationToken = default)
    {
        var response = await _client.Client.SendAsync(
            new(HttpMethod.Post, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), "policyinformation/getdelegationchanges"))
            {
                Content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, MediaTypeNames.Application.Json)
            },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<DelegationChangeDto>>(_serializerOptions, cancellationToken);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(content == string.Empty ? $"received status code {response.StatusCode}" : content);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DelegationChangeDto>> GetAllDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInputDto>[] actions)
    {
        var input = new DelegationChangeInputDto()
        {
            Resource = new List<AttributeMatchDto>(),
        };

        actions.ToList().ForEach(action => action(input));

        string cacheKey = GetDelegationChangesCacheKey(input);
        IEnumerable<DelegationChangeDto> result = await _cache.GetOrCreateAsync<IEnumerable<DelegationChangeDto>>(cacheKey, static _ => default, ReadOptions, cancellationToken: cancellationToken);
        if (result == null)
        {
            result = await GetAllDelegationChanges(input, cancellationToken);
            await _cache.SetAsync(cacheKey, result, DelegationCacheOptions, DelegationTags, cancellationToken);
        }

        return result;
    }

    private static string GetDelegationChangesCacheKey(DelegationChangeInputDto input)
    {
        string[] parts =
        [
            $"s:{input.Subject.Id}:{input.Subject.Value}",
            $"p:{input.Party.Value}",
            $"a:{input.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute)?.Value}/{input.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute)?.Value}",
            $"r:{input.Resource.FirstOrDefault(r => r.Id == AltinnXacmlConstants.MatchAttributeIdentifiers.ResourceRegistryAttribute)?.Value}",
            $"ac:{input.AccessRestriction}",
            $"vp:{input.ViaPartyOrganizationNumber}",
        ];

        return "delegations:" + string.Join("-", parts.Where(c => c != null && (c != string.Empty || !c.EndsWith(':'))));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(
                new Uri(_client.Settings.Value.ApiAccessManagementEndpoint),
                "authorizedparties?includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=false&includePartiesViaKeyRoles=true&includeSubParties=true&includeInactiveParties=true")
            );
        request.Headers.Add("Authorization", "Bearer " + JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName));

        var response = await _client.Client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<AuthorizedPartyDto>>(_serializerOptions, cancellationToken);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(content == string.Empty ? $"AuthorizedParties received status code {response.StatusCode}" : content);
    }

    /// <inheritdoc/>
    public async Task<AuthorizedPartyDto> GetAuthorizedParty(int partyId, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(
                new Uri(_client.Settings.Value.ApiAccessManagementEndpoint),
                $"authorizedparty/{partyId}?includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=false&includePartiesViaKeyRoles=true&includeSubParties=true&includeInactiveParties=true")
            );
        request.Headers.Add("Authorization", "Bearer " + JwtTokenUtil.GetTokenFromContext(_httpContextAccessor.HttpContext, _generalSettings.RuntimeCookieName));

        var response = await _client.Client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthorizedPartyDto>(_serializerOptions, cancellationToken);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(content == string.Empty ? $"AuthorizedParty received status code {response.StatusCode}" : content);
    }

    /// <inheritdoc/>
    public async Task<PipResponseDto> GetRolesAndAccessPackages(Guid to, Guid from, AccessRestriction accessRestriction = AccessRestriction.None, string viaPartyOrganizationNumber = null, CancellationToken cancellationToken = default)
    {
        string cacheKey = GetRolesAndAccessPackagesCacheKey(to, from, accessRestriction, viaPartyOrganizationNumber);

        PipResponseDto result = await _cache.GetOrCreateAsync<PipResponseDto>(cacheKey, static _ => default, ReadOptions, cancellationToken: cancellationToken);
        if (result == null)
        {
            var response = await _client.Client.SendAsync(
                new(HttpMethod.Get, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), $"policyinformation/roles-and-accesspackages?to={to}&from={from}&accessRestriction={accessRestriction}{ViaPartyQuery(viaPartyOrganizationNumber)}")),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_serializerOptions, cancellationToken);

                if (result != null)
                {
                    await _cache.SetAsync(cacheKey, result, _writeOptions, RolesTags, cancellationToken);
                    return result;
                }
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(content == string.Empty ? $"received status code {response.StatusCode}" : content);
        }

        return result;
    }

    private static string ViaPartyQuery(string viaPartyOrganizationNumber) =>
        string.IsNullOrWhiteSpace(viaPartyOrganizationNumber) ? string.Empty : $"&viaParty={Uri.EscapeDataString(viaPartyOrganizationNumber)}";
}
