using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessManagement.Persistence;
using Altinn.AccessManagement.Persistence.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Services.Legacy;

public sealed class DelegationMetadataRouter(ILegacyRoutingPolicy policy, IServiceScopeFactory scopeFactory) : IDelegationMetadataRepository
{
    private static readonly HashSet<string> InstanceMethods = new(StringComparer.Ordinal)
    {
        "InsertInstanceDelegation",
        "GetLastInstanceDelegationChange",
        "GetAllCurrentReceivedInstanceDelegations",
        "GetAllLatestInstanceDelegationChanges",
        "InsertMultipleInstanceDelegations",
        "GetActiveInstanceDelegations",
    };

    private static readonly HashSet<string> FeedMethods = new(StringComparer.Ordinal)
    {
        "GetNextPageAppDelegationChanges",
        "GetNextPageResourceDelegationChanges",
        "GetNextPageInstanceDelegationChanges"
    };

    private async Task<T> Route<T>(string methodName, Func<IDelegationMetadataRepository, Task<T>> call, CancellationToken ct)
    {
        string feature = InstanceMethods.Contains(methodName)
            ? "InstanceDelegation"
            : "ResourceDelegation";

        var useEF = await policy.IsEnabledAsync(feature, "EF", "AccessManagement", ct);

        if (FeedMethods.Contains(methodName))
        {
            useEF = false;
        }

        using var scope = scopeFactory.CreateScope();

        var target = useEF
            ? (IDelegationMetadataRepository)scope.ServiceProvider.GetRequiredService<DelegationMetadataEF>() 
            : (IDelegationMetadataRepository)scope.ServiceProvider.GetRequiredService<DelegationMetadataRepo>();

        return await call(target);
    }

    public Task<DelegationChange> InsertDelegation(
            ResourceAttributeMatchType resourceMatchType,
            DelegationChange delegationChange,
            CancellationToken cancellationToken = default)
            => Route(
                nameof(InsertDelegation),
                repo => repo.InsertDelegation(resourceMatchType, delegationChange, cancellationToken),
                cancellationToken);

    public Task<List<InstanceDelegationChange>> GetAllLatestInstanceDelegationChanges(
        InstanceDelegationSource source, 
        string resourceID, 
        string instanceID,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllLatestInstanceDelegationChanges),
            repo => repo.GetAllLatestInstanceDelegationChanges(source, resourceID, instanceID, cancellationToken),
            cancellationToken);

    public Task<List<InstanceDelegationChange>> GetAllCurrentReceivedInstanceDelegations(
        List<Guid> toUuid, 
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllCurrentReceivedInstanceDelegations),
            repo => repo.GetAllCurrentReceivedInstanceDelegations(toUuid, cancellationToken),
            cancellationToken);

    public Task<InstanceDelegationChange> GetLastInstanceDelegationChange(
        InstanceDelegationChangeRequest request, 
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetLastInstanceDelegationChange),
            repo => repo.GetLastInstanceDelegationChange(request, cancellationToken),
            cancellationToken);

    public Task<InstanceDelegationChange> InsertInstanceDelegation(
        InstanceDelegationChange instanceDelegationChange,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(InsertInstanceDelegation),
            repo => repo.InsertInstanceDelegation(instanceDelegationChange, cancellationToken),
            cancellationToken);

    public Task<bool> InsertMultipleInstanceDelegations(
        List<PolicyWriteOutput> policyWriteOutputs,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(InsertMultipleInstanceDelegations),
            repo => repo.InsertMultipleInstanceDelegations(policyWriteOutputs, cancellationToken),
            cancellationToken);

    public Task<IEnumerable<InstanceDelegationChange>> GetActiveInstanceDelegations(
        List<string> resourceIds, 
        Guid from, 
        List<Guid> to,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetActiveInstanceDelegations),
            repo => repo.GetActiveInstanceDelegations(resourceIds, from, to, cancellationToken),
            cancellationToken);

    public Task<DelegationChange> GetCurrentDelegationChange(
        ResourceAttributeMatchType resourceMatchType, 
        string resourceId, 
        int offeredByPartyId,
        int? coveredByPartyId, 
        int? coveredByUserId, 
        Guid? toUuid, 
        UuidType toUuidType,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetCurrentDelegationChange),
            repo => repo.GetCurrentDelegationChange(resourceMatchType, resourceId, offeredByPartyId, coveredByPartyId, coveredByUserId, toUuid, toUuidType, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllCurrentAppDelegationChanges(
        List<int> offeredByPartyIds, 
        List<string> altinnAppIds,
        List<int> 
        coveredByPartyIds = null, 
        List<int> coveredByUserIds = null,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllCurrentAppDelegationChanges),
            repo => repo.GetAllCurrentAppDelegationChanges(offeredByPartyIds, altinnAppIds, coveredByPartyIds, coveredByUserIds, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllCurrentAppDelegationChanges(
        List<string> altinnAppIds, 
        List<int> fromPartyIds, 
        UuidType toUuidType, 
        Guid toUuid,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllCurrentAppDelegationChanges),
            repo => repo.GetAllCurrentAppDelegationChanges(altinnAppIds, fromPartyIds, toUuidType, toUuid, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllCurrentResourceRegistryDelegationChanges(
        List<int> offeredByPartyIds, 
        List<string> resourceRegistryIds,
        List<int> coveredByPartyIds = null, 
        int? coveredByUserId = null,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllCurrentResourceRegistryDelegationChanges),
            repo => repo.GetAllCurrentResourceRegistryDelegationChanges(offeredByPartyIds, resourceRegistryIds, coveredByPartyIds, coveredByUserId, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllCurrentResourceRegistryDelegationChanges(
        List<string> resourceRegistryIds, 
        List<int> fromPartyIds, 
        UuidType toUuidType, 
        Guid toUuid,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllCurrentResourceRegistryDelegationChanges),
            repo => repo.GetAllCurrentResourceRegistryDelegationChanges(resourceRegistryIds, fromPartyIds, toUuidType, toUuid, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetOfferedResourceRegistryDelegations(
        int offeredByPartyId, 
        List<string> resourceRegistryIds = null, 
        List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetOfferedResourceRegistryDelegations),
            repo => repo.GetOfferedResourceRegistryDelegations(offeredByPartyId, resourceRegistryIds, resourceTypes, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetOfferedDelegations(
        List<int> offeredByPartyIds, 
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetOfferedDelegations),
            repo => repo.GetOfferedDelegations(offeredByPartyIds, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByPartys(
        List<int> coveredByPartyIds, 
        List<int> offeredByPartyIds = null,
        List<string> resourceRegistryIds = null, 
        List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetReceivedResourceRegistryDelegationsForCoveredByPartys),
            repo => repo.GetReceivedResourceRegistryDelegationsForCoveredByPartys(coveredByPartyIds, offeredByPartyIds, resourceRegistryIds, resourceTypes, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByUser(
        int coveredByUserId, 
        List<int> offeredByPartyIds,
        List<string> resourceRegistryIds = null, 
        List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetReceivedResourceRegistryDelegationsForCoveredByUser),
            repo => repo.GetReceivedResourceRegistryDelegationsForCoveredByUser(coveredByUserId, offeredByPartyIds, resourceRegistryIds, resourceTypes, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetResourceRegistryDelegationChanges(
        List<string> resourceIds, 
        int offeredByPartyId, 
        int coveredByPartyId, 
        AccessManagement.Core.Models.ResourceRegistry.ResourceType resourceType, 
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetResourceRegistryDelegationChanges),
            repo => repo.GetResourceRegistryDelegationChanges(resourceIds, offeredByPartyId, coveredByPartyId, resourceType, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(
        List<int> coveredByUserIds, 
        List<int> coveredByPartyIds,
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllDelegationChangesForAuthorizedParties),
            repo => repo.GetAllDelegationChangesForAuthorizedParties(coveredByUserIds, coveredByPartyIds, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(
        List<Guid> toPartyUuids, 
        CancellationToken cancellationToken = default)
        => Route(
            nameof(GetAllDelegationChangesForAuthorizedParties),
            repo => repo.GetAllDelegationChangesForAuthorizedParties(toPartyUuids, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetNextPageAppDelegationChanges(
        long startFeedIndex,
        CancellationToken cancellationToken)
        => Route(
            nameof(GetNextPageAppDelegationChanges),
            repo => repo.GetNextPageAppDelegationChanges(startFeedIndex, cancellationToken),
            cancellationToken);

    public Task<List<DelegationChange>> GetNextPageResourceDelegationChanges(
        long startFeedIndex,
        CancellationToken cancellationToken)
        => Route(
            nameof(GetNextPageResourceDelegationChanges),
            repo => repo.GetNextPageResourceDelegationChanges(startFeedIndex, cancellationToken),
            cancellationToken);

    public Task<List<InstanceDelegationChange>> GetNextPageInstanceDelegationChanges(
        long startFeedIndex,
        CancellationToken cancellationToken)
        => Route(
            nameof(GetNextPageInstanceDelegationChanges),
            repo => repo.GetNextPageInstanceDelegationChanges(startFeedIndex, cancellationToken),
            cancellationToken);
}
