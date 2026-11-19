using System.Text.Json;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Models.Oed;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.Authorization.Tests.Unit
{
    /// <summary>
    /// Every hit in HybridCache is a System.Text.Json copy of the stored value, and the distributed tier holds nothing
    /// else, so each type the PDP caches must come back from the serializer as it went in.
    /// </summary>
    [UnitTest]
    public class CacheRoundTripTest
    {
        private static readonly HybridCacheEntryOptions ReadOptions = new() { Flags = HybridCacheEntryFlags.DisableUnderlyingData };

        [Fact]
        public Task Party_RoundTripsThroughCache() => AssertRoundTrips(new Party
        {
            PartyId = 50001337,
            PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000001337"),
            PartyTypeName = PartyType.Person,
            SSN = "01017012345",
            Name = "Kari Nordmann",
            Person = new Person { SSN = "01017012345", FirstName = "Kari", LastName = "Nordmann", Name = "Kari Nordmann", AddressPostalCode = "0150" },
            ChildParties = [new Party { PartyId = 50001338, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000001338"), PartyTypeName = PartyType.Person, Name = "Child" }]
        });

        [Fact]
        public Task UserProfile_RoundTripsThroughCache() => AssertRoundTrips(new UserProfile
        {
            UserId = 1337,
            UserUuid = Guid.Parse("00000000-0000-0000-0001-000000001337"),
            UserName = "kari",
            PartyId = 50001337,
            Email = "kari@example.com",
            Party = new Party { PartyId = 50001337, PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000001337"), PartyTypeName = PartyType.Person, SSN = "01017012345" }
        });

        [Fact]
        public Task PipResponseDto_RoundTripsThroughCache() => AssertRoundTrips(new PipResponseDto
        {
            Roles = [RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"), RoleUrn.Parse("urn:altinn:rolecode:dagl")],
            AccessPackages = [AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator")]
        });

        [Fact]
        public Task DelegationChangeDto_RoundTripsThroughCache() => AssertRoundTrips(new DelegationChangeDto
        {
            DelegationChangeId = 1,
            DelegationChangeType = DelegationChangeTypeDto.Grant,
            ResourceId = "org1/app1",
            ResourceType = "AltinnApp",
            InstanceId = "f8d3526c-596b-4322-a041-38a8925c2a82",
            OfferedByPartyId = 50001337,
            FromUuid = Guid.Parse("00000000-0000-0000-0005-000000001337"),
            FromUuidType = UuidTypeDto.Organization,
            CoveredByUserId = 20001337,
            ToUuid = Guid.Parse("00000000-0000-0000-0001-000000001337"),
            ToUuidType = UuidTypeDto.Person,
            PerformedByUserId = 20001337,
            PerformedByUuid = "00000000-0000-0000-0001-000000001337",
            PerformedByUuidType = UuidTypeDto.Person,
            BlobStoragePolicyPath = "org1/app1/50001337/u20001337/delegationpolicy.xml",
            BlobStorageVersionId = "2026-01-02T03:04:05.0000000Z",
            Created = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        });

        [Fact]
        public Task OedRoleAssignment_RoundTripsThroughCache() => AssertRoundTrips(new OedRoleAssignment
        {
            OedRoleCode = "urn:digitaltdodsbo:arving",
            From = "01017012345",
            To = "02027012345",
            Created = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        });

        private static async Task AssertRoundTrips<T>(T value)
            where T : class
        {
            HybridCache cache = TestSetupUtil.CreateHybridCache();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            await cache.SetAsync("key", value, cancellationToken: cancellationToken);
            T copy = await cache.GetOrCreateAsync<T>("key", static _ => default, ReadOptions, cancellationToken: cancellationToken);

            copy.Should().NotBeSameAs(value);
            JsonSerializer.Serialize(copy).Should().Be(JsonSerializer.Serialize(value));
        }
    }
}
