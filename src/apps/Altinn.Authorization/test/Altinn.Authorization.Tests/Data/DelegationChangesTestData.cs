using System;
using Altinn.Authorization.Enums;
using Altinn.Platform.Authorization.Models;

namespace Altinn.Platform.Authorization.IntegrationTests.Data;

public static class DelegationChangesTestData
{
    public static DelegationChangeExternal Default(params Action<DelegationChangeExternal>[] actions)
    {
        var data = new DelegationChangeExternal()
        {
            DelegationChangeId = 1337,
            ResourceRegistryDelegationChangeId = 0,
            DelegationChangeType = DelegationChangeType.Grant,
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

    public static void WithBlobStorage(DelegationChangeExternal data)
    {
        var offeredBy = data.InstanceId != null ? $"Instance{data.InstanceId}" : data.OfferedByPartyId.ToString();
        var coveredBy = data.CoveredByPartyId != null ? $"p{data.CoveredByPartyId}" : $"u{data.CoveredByUserId}";
        coveredBy = data.ToUuid.HasValue ? $"{data.ToUuidType}{data.ToUuid}" : coveredBy;
        data.BlobStoragePolicyPath = $"{data.ResourceId}/{offeredBy}/{coveredBy}/delegationpolicy.xml";
    } 

    public static Action<DelegationChangeExternal> WithChangeID(int changeID) => (delegation) => delegation.DelegationChangeId = changeID;

    public static Action<DelegationChangeExternal> WithDelegationChangeType(DelegationChangeType changeType) => (delegation) => delegation.DelegationChangeType = changeType;

    public static Action<DelegationChangeExternal> WithPerformedByUserID(int userID) => (delegation) => delegation.PerformedByUserId = userID;

    public static Action<DelegationChangeExternal> WithResourceID(string resourceId) => (delegation) => delegation.ResourceId = resourceId;

    public static Action<DelegationChangeExternal> WithCoveredByPartyID(int partyID) => (delegation) => delegation.CoveredByPartyId = partyID;

    public static Action<DelegationChangeExternal> WithCoveredByUserID(int userID) => (delegation) => delegation.CoveredByUserId = userID;

    public static Action<DelegationChangeExternal> WithResourceInstanceId(string instanceId) => (delegation) => delegation.InstanceId = instanceId;

    public static Action<DelegationChangeExternal> WithToUuid(UuidType toType, Guid to) => (delegation) =>
    {
        delegation.ToUuidType = toType;
        delegation.ToUuid = to;
    };

    public static Action<DelegationChangeExternal> WithOfferedByPartyID(int partyID) => (delegation) => delegation.OfferedByPartyId = partyID;

    public static Action<DelegationChangeExternal> WithFromUuid(UuidType fromType, Guid from) => (delegation) =>
    {
        delegation.FromUuidType = fromType;
        delegation.FromUuid = from;
    };
}