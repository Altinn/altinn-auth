using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.Authorization.Models;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
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
    public async Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(DelegationChangeInput input, CancellationToken cancellationToken = default)
    {
        var response = await _client.Client.SendAsync(
            new(HttpMethod.Post, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), "policyinformation/getdelegationchanges"))
            {
                Content = new StringContent(JsonSerializer.Serialize(input), Encoding.UTF8, MediaTypeNames.Application.Json)
            },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<IEnumerable<DelegationChangeExternal>>(_serializerOptions, cancellationToken);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(content == string.Empty ? $"received status code {response.StatusCode}" : content);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInput>[] actions)
    {
        var input = new DelegationChangeInput()
        {
            Resource = new List<AttributeMatch>(),
        };

        actions.ToList().ForEach(action => action(input));
        return await GetAllDelegationChanges(input);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(
                new Uri(_client.Settings.Value.ApiAccessManagementEndpoint),
                "authorizedparties?includeAltinn2=true&includeAltinn3=true&includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=false&includePartiesViaKeyRoles=true&includeSubParties=true&includeInactiveParties=true")
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
                $"authorizedparty/{partyId}?includeAltinn2=true&includeAltinn3=true&includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=false&includePartiesViaKeyRoles=true&includeSubParties=true&includeInactiveParties=true")
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
    public async Task<IEnumerable<AccessPackageUrn>> GetAccessPackages(Guid to, Guid from, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"AccPkgs|f:{from}|t:{to}";

        if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<AccessPackageUrn> result))
        {
            var response = await _client.Client.SendAsync(
                new(HttpMethod.Get, new Uri(new Uri(_client.Settings.Value.ApiAccessManagementEndpoint), $"policyinformation/accesspackages?to={to}&from={from}")),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadFromJsonAsync<IEnumerable<AccessPackageUrn>>(_serializerOptions, cancellationToken);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetPriority(CacheItemPriority.High)
                .SetAbsoluteExpiration(new TimeSpan(0, 0, _generalSettings.RoleCacheTimeout, 0));

                _memoryCache.Set(cacheKey, result, cacheEntryOptions);

                return result;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(content == string.Empty ? $"received status code {response.StatusCode}" : content);
        }

        return result;
    }
}
