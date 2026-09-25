using Altinn.AccessManagement.Api.Internal.Enums;
using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Api.Internal.Models;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;

namespace Altinn.AccessManagement.Api.Internal.Tests.Extensions;

[UnitTest]
public class DelegationChangeExtensionsTest
{
    public static TheoryData<DelegationChangeType> DelegationChangeTypes => new(Enum.GetValues<DelegationChangeType>());

    public static TheoryData<UuidType> UuidTypes => new(Enum.GetValues<UuidType>());

    [Fact]
    public void ToDelegationChangeExternal_WithFullyPopulatedSource_CopiesAllProperties()
    {
        var source = new DelegationChange
        {
            DelegationChangeId = 1,
            ResourceRegistryDelegationChangeId = 2,
            DelegationChangeType = DelegationChangeType.Revoke,
            ResourceId = "resource-id",
            ResourceType = "resource-type",
            InstanceId = "instance-id",
            OfferedByPartyId = 3,
            FromUuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FromUuidType = UuidType.Person,
            CoveredByPartyId = 4,
            CoveredByUserId = 5,
            ToUuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ToUuidType = UuidType.Organization,
            PerformedByUserId = 6,
            PerformedByPartyId = 7,
            PerformedByUuid = "33333333-3333-3333-3333-333333333333",
            PerformedByUuidType = UuidType.SystemUser,
            BlobStoragePolicyPath = "policy/path",
            BlobStorageVersionId = "version-id",
            Created = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc),
        };

        var result = source.ToDelegationChangeExternal();

        result.Should().BeEquivalentTo(new DelegationChangeExternal
        {
            DelegationChangeId = 1,
            ResourceRegistryDelegationChangeId = 2,
            DelegationChangeType = DelegationChangeTypeExternal.Revoke,
            ResourceId = "resource-id",
            ResourceType = "resource-type",
            InstanceId = "instance-id",
            OfferedByPartyId = 3,
            FromUuid = source.FromUuid,
            FromUuidType = UuidTypeExternal.Person,
            CoveredByPartyId = 4,
            CoveredByUserId = 5,
            ToUuid = source.ToUuid,
            ToUuidType = UuidTypeExternal.Organization,
            PerformedByUserId = 6,
            PerformedByPartyId = 7,
            PerformedByUuid = source.PerformedByUuid,
            PerformedByUuidType = UuidTypeExternal.SystemUser,
            BlobStoragePolicyPath = "policy/path",
            BlobStorageVersionId = "version-id",
            Created = source.Created,
        });
    }

    [Theory]
    [MemberData(nameof(DelegationChangeTypes))]
    public void ToDelegationChangeExternal_ForEachDelegationChangeType_MapsToSameNameAndValue(DelegationChangeType type)
    {
        var result = new DelegationChange { DelegationChangeType = type }.ToDelegationChangeExternal();

        result.DelegationChangeType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
    }

    [Theory]
    [MemberData(nameof(UuidTypes))]
    public void ToDelegationChangeExternal_ForEachUuidType_MapsToSameNameAndValue(UuidType type)
    {
        var result = new DelegationChange { FromUuidType = type, ToUuidType = type, PerformedByUuidType = type }.ToDelegationChangeExternal();

        result.FromUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
        result.ToUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
        result.PerformedByUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
    }
}
