using Altinn.Authorization.Api.Contracts.Authorization;

namespace Altinn.Authorization.Tests.Data;

public static class DelegationChangesTestData
{
    public static DelegationChangeDto Default(params Action<DelegationChangeDto>[] actions)
    {
        var data = new DelegationChangeDto()
        {
            DelegationChangeId = 1337,
            ResourceRegistryDelegationChangeId = 0,
            DelegationChangeType = DelegationChangeTypeDto.Grant,
            ResourceId = "ttd/apps-test",
            ResourceType = string.Empty,
            OfferedByPartyId = 0,
            CoveredByPartyId = null,
            CoveredByUserId = null,
            PerformedByUserId = 20001336,
            PerformedByPartyId = null,
            BlobStoragePolicyPath = "{altinnAppId}/{offeredByPartyId}/{coveredBy}/delegationpolicy.xml",
            BlobStorageVersionId = "CorrectLeaseId",
            Created = DateTime.Now
        };

        foreach (var action in actions)
        {
            action(data);
        }

        WithBlobStorage(data);
        return data;
    }

    public static void WithBlobStorage(DelegationChangeDto data)
    {
        var offeredBy = data.InstanceId != null ? $"Instance{data.InstanceId}" : data.OfferedByPartyId.ToString();
        var coveredBy = data.CoveredByPartyId != null ? $"p{data.CoveredByPartyId}" : $"u{data.CoveredByUserId}";
        coveredBy = data.ToUuid.HasValue ? $"{data.ToUuidType}{data.ToUuid}" : coveredBy;
        data.BlobStoragePolicyPath = $"{data.ResourceId}/{offeredBy}/{coveredBy}/delegationpolicy.xml";
    }

    public static Action<DelegationChangeDto> WithChangeID(int changeID) => (delegation) => delegation.DelegationChangeId = changeID;

    public static Action<DelegationChangeDto> WithDelegationChangeType(DelegationChangeTypeDto changeType) => (delegation) => delegation.DelegationChangeType = changeType;

    public static Action<DelegationChangeDto> WithPerformedByUserID(int userID) => (delegation) => delegation.PerformedByUserId = userID;

    public static Action<DelegationChangeDto> WithResourceID(string resourceId) => (delegation) => delegation.ResourceId = resourceId;

    public static Action<DelegationChangeDto> WithCoveredByPartyID(int partyID) => (delegation) => delegation.CoveredByPartyId = partyID;

    public static Action<DelegationChangeDto> WithCoveredByUserID(int userID) => (delegation) => delegation.CoveredByUserId = userID;

    public static Action<DelegationChangeDto> WithResourceInstanceId(string instanceId) => (delegation) => delegation.InstanceId = instanceId;

    public static Action<DelegationChangeDto> WithToUuid(UuidTypeDto toType, Guid to) => (delegation) =>
    {
        delegation.ToUuidType = toType;
        delegation.ToUuid = to;
    };

    public static Action<DelegationChangeDto> WithOfferedByPartyID(int partyID) => (delegation) => delegation.OfferedByPartyId = partyID;

    public static Action<DelegationChangeDto> WithFromUuid(UuidTypeDto fromType, Guid from) => (delegation) =>
    {
        delegation.FromUuidType = fromType;
        delegation.FromUuid = from;
    };
}
