using System.ComponentModel;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Authentication;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.SblBridge;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Platform.Register.Models;
using Authorization.Platform.Authorization.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Core.Services;

/// <summary>
/// Context Retrieval Service
/// </summary>
public class ContextRetrievalService : IContextRetrievalService
{
    private static readonly string[] RolesTag = ["roles"];
    private static readonly string[] PartyTag = ["party"];
    private static readonly string[] ResourceTag = ["resource"];
    private static readonly string[] ProfileTag = ["profile"];

    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _roleOptions;
    private readonly HybridCacheEntryOptions _partyOptions;
    private readonly HybridCacheEntryOptions _mainUnitOptions;
    private readonly HybridCacheEntryOptions _keyRolePartyIdsOptions;
    private readonly HybridCacheEntryOptions _resourceOptions;
    private readonly HybridCacheEntryOptions _resourceListOptions;
    private readonly HybridCacheEntryOptions _subjectResourcesOptions;
    private readonly IResourceRegistryClient _resourceRegistryClient;
    private readonly IAltinnRolesClient _altinnRolesClient;
    private readonly IPartiesClient _partiesClient;
    private readonly IAuthenticationClient _authenticationClient;
    private readonly IProfileClient _profileClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextRetrievalService"/> class
    /// </summary>
    /// <param name="cacheConfig">Cache config</param>
    /// <param name="cache">The cache</param>
    /// <param name="resourceRegistryClient">The client for integration with the ResourceRegistry</param>
    /// <param name="altinnRolesClient">The client for integration with the SBL Bridge for role information</param>
    /// <param name="partiesClient">The client for integration </param>
    /// <param name="authenticationClient">The client for integration with authentication</param>
    /// <param name="profileClient">The client for integration with profile</param>
    public ContextRetrievalService(IOptions<CacheConfig> cacheConfig, HybridCache cache, IResourceRegistryClient resourceRegistryClient, IAltinnRolesClient altinnRolesClient, IPartiesClient partiesClient, IAuthenticationClient authenticationClient, IProfileClient profileClient)
    {
        CacheConfig config = cacheConfig.Value;
        _cache = cache;
        _roleOptions = Minutes(config.AltinnRoleCacheTimeout);
        _partyOptions = Minutes(config.PartyCacheTimeout);
        _mainUnitOptions = Minutes(config.MainUnitCacheTimeout);
        _keyRolePartyIdsOptions = Minutes(config.KeyRolePartyIdsCacheTimeout);
        _resourceOptions = Minutes(config.ResourceRegistryResourceCacheTimeout);
        _subjectResourcesOptions = Minutes(config.ResourceRegistrySubjectResourcesCacheTimeout);

        // The whole resource registry is several MB serialized (tt02: 10 MB), far over HybridCache's payload limit, so
        // the two lists never leave this process: no distributed tier, and an explicit local expiration so a short
        // default LocalCacheExpiration from the shared registration cannot evict them.
        TimeSpan resourceListExpiration = TimeSpan.FromMinutes(config.ResourceRegistryResourceCacheTimeout);
        _resourceListOptions = new HybridCacheEntryOptions
        {
            Expiration = resourceListExpiration,
            LocalCacheExpiration = resourceListExpiration,
            Flags = HybridCacheEntryFlags.DisableDistributedCache,
        };

        _resourceRegistryClient = resourceRegistryClient;
        _altinnRolesClient = altinnRolesClient;
        _partiesClient = partiesClient;
        _authenticationClient = authenticationClient;
        _profileClient = profileClient;
    }

    /// <inheritdoc/>
    public async Task<List<Role>> GetDecisionPointRolesForUser(int coveredByUserId, int offeredByPartyId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"roles:decision:u{coveredByUserId}:p{offeredByPartyId}",
            async ct => await _altinnRolesClient.GetDecisionPointRolesForUser(coveredByUserId, offeredByPartyId, ct) ?? new List<Role>(),
            _roleOptions,
            RolesTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Role>> GetRolesForDelegation(int coveredByUserId, int offeredByPartyId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"roles:delegation:u{coveredByUserId}:p{offeredByPartyId}",
            async ct => await _altinnRolesClient.GetRolesForDelegation(coveredByUserId, offeredByPartyId, ct) ?? new List<Role>(),
            _roleOptions,
            RolesTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Party> GetPartyAsync(int partyId, CancellationToken cancellationToken = default)
    {
        List<Party> result = await GetPartiesAsync(partyId.SingleToList(), cancellationToken: cancellationToken);
        return result.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<List<Party>> GetPartiesAsync(List<int> partyIds, bool includeSubunits = false, CancellationToken cancellationToken = default)
    {
        List<Party> parties = new List<Party>();
        List<int> partyIdsNotInCache = new List<int>();

        foreach (int partyId in partyIds.Distinct())
        {
            Party party = await _cache.GetAsync<Party>(PartyIdKey(partyId, includeSubunits), cancellationToken);
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

        List<Party> remainingParties = await _partiesClient.GetPartiesAsync(partyIdsNotInCache, includeSubunits, cancellationToken);
        if (remainingParties.Count > 0)
        {
            foreach (Party party in remainingParties)
            {
                if (party?.PartyId != null)
                {
                    parties.Add(party);
                    await _cache.SetAsync(PartyIdKey(party.PartyId, includeSubunits), party, _partyOptions, PartyTag, cancellationToken);
                }
            }
        }

        return parties;
    }

    /// <inheritdoc/>
    public async Task<SortedDictionary<int, Party>> GetPartiesAsSortedDictionaryAsync(List<int> partyIds, bool includeSubunits = false, CancellationToken cancellationToken = default)
    {
        SortedDictionary<int, Party> dict = [];

        List<Party> parties = await GetPartiesAsync(partyIds, includeSubunits, cancellationToken);
        foreach (Party party in parties)
        {
            dict.Add(party.PartyId, party);
        }

        return dict;
    }

    /// <inheritdoc/>
    public async Task<Party> GetPartyByUuid(Guid partyUuid, bool includeSubunits = false, CancellationToken cancellationToken = default)
    {
        Dictionary<string, Party> parties = await GetPartiesByUuids(partyUuid.SingleToList(), includeSubunits, cancellationToken);
        parties.TryGetValue(partyUuid.ToString(), out Party result);
        return result;
    }

    /// <inheritdoc />
    public async Task<SystemUser> GetSystemUserById(int partyId, string systemUserUuid, CancellationToken cancellationToken = default)
    {
        SystemUser systemUser = await _authenticationClient.GetSystemUser(partyId, systemUserUuid, cancellationToken);
        return systemUser;
    }

    /// <inheritdoc />
    public async Task<List<DefaultRight>> GetDefaultRightsForRegisteredSystem(string productId, CancellationToken cancellationToken = default)
    {
        List<DefaultRight> defaultRight = await _authenticationClient.GetDefaultRightsForRegisteredSystem(productId, cancellationToken);
        return defaultRight;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, Party>> GetPartiesByUuids(IEnumerable<Guid> partyUuids, bool includeSubunits = false, CancellationToken cancellationToken = default)
    {
        Dictionary<string, Party> parties = new Dictionary<string, Party>();
        List<Guid> partyKeysNotInCache = new List<Guid>();

        foreach (Guid partyKey in partyUuids.Distinct())
        {
            Party party = await _cache.GetAsync<Party>(PartyUuidKey(partyKey, includeSubunits), cancellationToken);
            if (party != null)
            {
                parties.Add($"{party.PartyUuid}", party);
            }
            else
            {
                partyKeysNotInCache.Add(partyKey);
            }
        }

        if (partyKeysNotInCache.Count == 0)
        {
            return parties;
        }

        List<Party> remainingParties = await _partiesClient.GetPartiesAsync(partyKeysNotInCache, includeSubunits, cancellationToken);
        if (remainingParties.Count > 0)
        {
            foreach (Party party in remainingParties)
            {
                if (party != null && party.PartyUuid != Guid.Empty)
                {
                    parties.Add($"{party.PartyUuid}", party);
                    if (party.PartyUuid.HasValue)
                    {
                        await _cache.SetAsync(PartyUuidKey(party.PartyUuid.Value, includeSubunits), party, _partyOptions, PartyTag, cancellationToken);
                    }
                }
            }
        }

        return parties;
    }

    /// <inheritdoc/>
    public async Task<Party> GetPartyForOrganization(string organizationNumber, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"party:org:{organizationNumber}",
            ct => _partiesClient.LookupPartyBySSNOrOrgNo(new PartyLookup { OrgNo = organizationNumber }, ct),
            _partyOptions,
            PartyTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Party> GetPartyForPerson(string ssn, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"party:ssn:{ssn}",
            ct => _partiesClient.LookupPartyBySSNOrOrgNo(new PartyLookup { Ssn = ssn }, ct),
            _partyOptions,
            PartyTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<int>> GetKeyRolePartyIds(int userId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"party:keyrole:u{userId}",
            async ct => await _partiesClient.GetKeyRoleParties(userId, ct),
            _keyRolePartyIdsOptions,
            PartyTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<MainUnit>> GetMainUnits(List<int> subunitPartyIds, CancellationToken cancellationToken = default)
    {
        List<MainUnit> allMainUnits = new List<MainUnit>();
        List<int> subunitsNotInCache = new List<int>();

        foreach (int subunitPartyId in subunitPartyIds.Distinct())
        {
            MainUnit mainUnit = await _cache.GetAsync<MainUnit>(MainUnitKey(subunitPartyId), cancellationToken);
            if (mainUnit != null)
            {
                allMainUnits.Add(mainUnit);
            }
            else
            {
                subunitsNotInCache.Add(subunitPartyId);
            }
        }

        if (subunitsNotInCache.Count == 0)
        {
            return allMainUnits;
        }

        List<MainUnit> remainingMainUnits = await _partiesClient.GetMainUnits(new MainUnitQuery { PartyIds = subunitsNotInCache }, cancellationToken);
        if (remainingMainUnits.Count > 0)
        {
            foreach (MainUnit mainUnit in remainingMainUnits)
            {
                allMainUnits.Add(mainUnit);
                await _cache.SetAsync(MainUnitKey(mainUnit.SubunitPartyId), mainUnit, _mainUnitOptions, PartyTag, cancellationToken);
            }
        }

        return allMainUnits;
    }

    /// <inheritdoc/>
    public async Task<MainUnit> GetMainUnit(int subunitPartyId, CancellationToken cancellationToken = default)
    {
        MainUnit mainUnit = await _cache.GetAsync<MainUnit>(MainUnitKey(subunitPartyId), cancellationToken);
        if (mainUnit != null)
        {
            return mainUnit;
        }

        List<MainUnit> mainUnits = await GetMainUnits(subunitPartyId.SingleToList(), cancellationToken);
        return mainUnits.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> GetResource(string resourceRegistryId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"resource:id:{resourceRegistryId}",
            ct => _resourceRegistryClient.GetResource(resourceRegistryId, ct),
            _resourceOptions,
            ResourceTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<RightDto>> GetResourcePolicyV2(string resourceRegistryId, string languageCode = "nb", CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"resource:rights:{resourceRegistryId}:{languageCode}",
            ct => _resourceRegistryClient.GetPolicyRightsV2(resourceRegistryId, languageCode, ct),
            _resourceOptions,
            ResourceTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ServiceResource>> GetResources(CancellationToken cancellationToken = default)
    {
        return await GetResourceListAsync("resource:all", ct => _resourceRegistryClient.GetResources(ct), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ServiceResource>> GetResourceList(CancellationToken cancellationToken = default)
    {
        return await GetResourceListAsync("resource:list", _resourceRegistryClient.GetResourceList, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> GetResourceFromResourceList(string resourceId = null, string org = null, string app = null, string serviceCode = null, string serviceEditionCode = null, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"resource:lookup:r:{resourceId},o:{org},a:{app},sc:{serviceCode},sec:{serviceEditionCode}",
            async ct =>
            {
                List<ServiceResource> resources = await GetResourceList(ct);
                foreach (ServiceResource serviceResource in resources)
                {
                    if (resourceId != null && (serviceResource.ResourceType != ResourceType.Altinn2Service || serviceResource.ResourceType != ResourceType.AltinnApp || serviceResource.ResourceType != ResourceType.MigratedApp) &&
                        serviceResource.Identifier == resourceId)
                    {
                        return serviceResource;
                    }

                    if (org != null && app != null && (serviceResource.ResourceType == ResourceType.AltinnApp || serviceResource.ResourceType == ResourceType.MigratedApp) &&
                        serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType == ReferenceType.ApplicationId && string.Equals(rf.Reference, $"{org}/{app}", StringComparison.OrdinalIgnoreCase)))
                    {
                        return serviceResource;
                    }

                    if (serviceCode != null && serviceEditionCode != null && serviceResource.ResourceType == ResourceType.Altinn2Service &&
                        serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType == ReferenceType.ServiceCode && string.Equals(rf.Reference, $"{serviceCode}", StringComparison.OrdinalIgnoreCase)) &&
                        serviceResource.ResourceReferences.Exists(rf => rf.ReferenceType == ReferenceType.ServiceEditionCode && string.Equals(rf.Reference, $"{serviceEditionCode}", StringComparison.OrdinalIgnoreCase)))
                    {
                        return serviceResource;
                    }
                }

                return null;
            },
            _resourceOptions,
            ResourceTag,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Party> GetPartyForUser(int userId, int partyId, CancellationToken cancellationToken = default)
    {
        List<Party> partyList = await GetPartiesForUser(userId, cancellationToken);

        foreach (Party party in partyList)
        {
            if (party?.PartyId == partyId)
            {
                return party;
            }

            Party childParty = party?.ChildParties?.FirstOrDefault(p => p.PartyId == partyId);
            if (childParty != null)
            {
                return childParty;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, IEnumerable<BaseAttribute>>> GetSubjectResources(IEnumerable<string> subjects, CancellationToken cancellationToken = default)
    {
        Dictionary<string, IEnumerable<BaseAttribute>> subjectResources = new Dictionary<string, IEnumerable<BaseAttribute>>();
        List<string> subjectKeysNotInCache = new List<string>();

        foreach (string subjectKey in subjects.Distinct())
        {
            IEnumerable<BaseAttribute> resources = await _cache.GetAsync<IEnumerable<BaseAttribute>>(SubjectResourcesKey(subjectKey), cancellationToken);
            if (resources != null)
            {
                subjectResources.Add(subjectKey, resources);
            }
            else
            {
                subjectKeysNotInCache.Add(subjectKey);
            }
        }

        if (subjectKeysNotInCache.Count == 0)
        {
            return subjectResources;
        }

        IDictionary<string, IEnumerable<BaseAttribute>> remainingSubjectResources = await _resourceRegistryClient.GetSubjectResources(subjectKeysNotInCache, cancellationToken);
        foreach (string subject in subjectKeysNotInCache)
        {
            IEnumerable<BaseAttribute> resources;
            if (remainingSubjectResources.TryGetValue(subject, out resources))
            {
                subjectResources.Add(subject, resources);
            }

            await _cache.SetAsync(SubjectResourcesKey(subject), resources ?? Enumerable.Empty<BaseAttribute>(), _subjectResourcesOptions, ResourceTag, cancellationToken);
        }

        return subjectResources;
    }

    /// <inheritdoc/>
    public async Task<NewUserProfile> GetNewUserProfile(int userId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrFetchAsync(
            $"profile:user:{userId}",
            ct => _profileClient.GetUser(new UserProfileLookup { UserId = userId }, ct),
            _partyOptions,
            ProfileTag,
            cancellationToken);
    }

    private static HybridCacheEntryOptions Minutes(int minutes) => new() { Expiration = TimeSpan.FromMinutes(minutes) };

    private static string PartyIdKey(int partyId, bool includeSubunits) => $"party:id:{partyId}:subunits:{includeSubunits}";

    private static string PartyUuidKey(Guid partyUuid, bool includeSubunits) => $"party:uuid:{partyUuid}:subunits:{includeSubunits}";

    private static string MainUnitKey(int subunitPartyId) => $"party:mainunit:{subunitPartyId}";

    private static string SubjectResourcesKey(string subject) => $"resource:subject:{subject}";

    private async Task<List<Party>> GetPartiesForUser(int userId, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"party:user:{userId}",
            async ct => await _partiesClient.GetPartiesForUserAsync(userId, ct),
            _partyOptions,
            PartyTag,
            cancellationToken);
    }

    private async Task<List<ServiceResource>> GetResourceListAsync(string cacheKey, Func<CancellationToken, Task<List<ServiceResource>>> fetch, CancellationToken cancellationToken)
    {
        ResourceList cached = await _cache.GetAsync<ResourceList>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached.Resources;
        }

        List<ServiceResource> resources = await fetch(cancellationToken);
        if (resources?.Count > 0)
        {
            await _cache.SetAsync(cacheKey, new ResourceList(resources), _resourceListOptions, ResourceTag, cancellationToken);
        }

        return resources;
    }

    /// <summary>
    /// Holds a whole resource registry list. Sealed and marked immutable so <see cref="HybridCache"/> keeps the instance
    /// in memory as-is and every hit returns it, instead of serializing it on write and deserializing it on every read.
    /// </summary>
    [ImmutableObject(true)]
    private sealed class ResourceList(List<ServiceResource> resources)
    {
        public List<ServiceResource> Resources { get; } = resources;
    }
}
