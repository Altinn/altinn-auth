using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Enums;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Services.Interface;
using AltinnCore.Authentication.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Altinn.Platform.Authorization.Services.Implementation;

/// <summary>
/// Wrapper for the Altinn Access Management API
/// </summary>
[ExcludeFromCodeCoverage]
public class AccessManagementWrapper : IAccessManagementWrapper
{
    private readonly GeneralSettings _generalSettings;
    private readonly AccessManagementClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _memoryCache;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessManagementWrapper"/> class.
    /// </summary>
    public AccessManagementWrapper(IOptions<GeneralSettings> generalSettings, AccessManagementClient client, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
    {
        _client = client;
        _generalSettings = generalSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _memoryCache = memoryCache;
    }

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
        return await GetAllDelegationChanges(input, cancellationToken);
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
        var cacheKey = $"RolesAndAccPkgs|f:{from}|t:{to}|ac:{accessRestriction}|vp:{viaPartyOrganizationNumber}";

        if (!_memoryCache.TryGetValue(cacheKey, out PipResponseDto result))
        {
            var response = await _client.Client.SendAsync(
                new(HttpMethod.Get, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), $"policyinformation/roles-and-accesspackages?to={to}&from={from}&accessRestriction={accessRestriction}{ViaPartyQuery(viaPartyOrganizationNumber)}")),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<PipResponseDto>(_serializerOptions, cancellationToken);

                if (result != null)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.High)
                    .SetAbsoluteExpiration(new TimeSpan(0, 0, _generalSettings.RoleCacheTimeout, 0));

                    _memoryCache.Set(cacheKey, result, cacheEntryOptions);
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
