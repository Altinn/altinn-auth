using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.Authorization;

namespace Altinn.AccessManagement.Api.Internal.Tests.Extensions;

[UnitTest]
public class DelegationChangeExtensionsTest
{
    public static TheoryData<DelegationChangeType> DelegationChangeTypes => new(Enum.GetValues<DelegationChangeType>());

    public static TheoryData<UuidType> UuidTypes => new(Enum.GetValues<UuidType>());

    public static TheoryData<AuthContextDto> AuthContexts => new(Enum.GetValues<AuthContextDto>());

    [Fact]
    public void ToDelegationChangeDto_WithFullyPopulatedSource_CopiesAllProperties()
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

        var result = source.ToDelegationChangeDto();

        result.Should().BeEquivalentTo(new DelegationChangeDto
        {
            DelegationChangeId = source.DelegationChangeId,
            ResourceRegistryDelegationChangeId = source.ResourceRegistryDelegationChangeId,
            DelegationChangeType = DelegationChangeTypeDto.Revoke,
            ResourceId = source.ResourceId,
            ResourceType = source.ResourceType,
            InstanceId = source.InstanceId,
            OfferedByPartyId = source.OfferedByPartyId,
            FromUuid = source.FromUuid,
            FromUuidType = UuidTypeDto.Person,
            CoveredByPartyId = source.CoveredByPartyId,
            CoveredByUserId = source.CoveredByUserId,
            ToUuid = source.ToUuid,
            ToUuidType = UuidTypeDto.Organization,
            PerformedByUserId = source.PerformedByUserId,
            PerformedByPartyId = source.PerformedByPartyId,
            PerformedByUuid = source.PerformedByUuid,
            PerformedByUuidType = UuidTypeDto.SystemUser,
            BlobStoragePolicyPath = source.BlobStoragePolicyPath,
            BlobStorageVersionId = source.BlobStorageVersionId,
            Created = source.Created,
        });
    }

    [Theory]
    [MemberData(nameof(DelegationChangeTypes))]
    public void ToDelegationChangeDto_ForEachDelegationChangeType_MapsToSameNameAndValue(DelegationChangeType type)
    {
        var result = new DelegationChange { DelegationChangeType = type }.ToDelegationChangeDto();

        result.DelegationChangeType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
    }

    [Theory]
    [MemberData(nameof(UuidTypes))]
    public void ToDelegationChangeDto_ForEachUuidType_MapsToSameNameAndValue(UuidType type)
    {
        var result = new DelegationChange { FromUuidType = type, ToUuidType = type, PerformedByUuidType = type }.ToDelegationChangeDto();

        result.FromUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
        result.ToUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
        result.PerformedByUuidType.Should().HaveSameNameAs(type).And.HaveSameValueAs(type);
    }

    [Fact]
    public void ToDelegationChangeInput_WithFullyPopulatedSource_CopiesAllAttributes()
    {
        var source = new DelegationChangeInputDto
        {
            Subject = new AttributeMatchDto("urn:altinn:userid", "20001337"),
            Party = new AttributeMatchDto("urn:altinn:partyid", "50001337"),
            Resource = [new AttributeMatchDto("urn:altinn:org", "ttd"), new AttributeMatchDto("urn:altinn:app", "app1")],
            AuthContext = AuthContextDto.ClientAccess,
            ViaPartyOrganizationNumber = "910000000",
        };

        var result = source.ToDelegationChangeInput();

        result.Should().BeEquivalentTo(source, options => options.WithStrictOrdering());
    }

    [Theory]
    [MemberData(nameof(AuthContexts))]
    public void ToDelegationChangeInput_ForEachAuthContext_MapsToSameNameAndValue(AuthContextDto authContext)
    {
        var result = new DelegationChangeInputDto { AuthContext = authContext }.ToDelegationChangeInput();

        result.AuthContext.Should().HaveSameNameAs(authContext).And.HaveSameValueAs(authContext);
    }
}
