using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;
using Altinn.AccessManagement.Integration.Services;

namespace AccessMgmt.Tests.Services;

public class EventMapperServiceTest
{
    private readonly EventMapperService _sut = new();

    [Fact]
    public void MapToDelegationChangeEventList_EmptyList_ReturnsEmptyEventList()
    {
        var result = _sut.MapToDelegationChangeEventList([]);

        result.DelegationChangeEvents.Should().BeEmpty();
    }

    [Fact]
    public void MapToDelegationChangeEventList_SingleChange_MapsAllFields()
    {
        var created = DateTime.UtcNow;
        var change = new DelegationChange
        {
            DelegationChangeId = 42,
            ResourceId = "ttd-some-app",
            DelegationChangeType = DelegationChangeType.Grant,
            OfferedByPartyId = 100,
            CoveredByPartyId = 200,
            CoveredByUserId = 300,
            PerformedByUserId = 400,
            Created = created,
        };

        var result = _sut.MapToDelegationChangeEventList([change]);

        result.DelegationChangeEvents.Should().HaveCount(1);
        var evt = result.DelegationChangeEvents[0];
        evt.EventType.Should().Be(DelegationChangeEventType.Grant);
        evt.DelegationChange.DelegationChangeId.Should().Be(42);
        evt.DelegationChange.AltinnAppId.Should().Be("ttd-some-app");
        evt.DelegationChange.OfferedByPartyId.Should().Be(100);
        evt.DelegationChange.CoveredByPartyId.Should().Be(200);
        evt.DelegationChange.CoveredByUserId.Should().Be(300);
        evt.DelegationChange.PerformedByUserId.Should().Be(400);
        evt.DelegationChange.Created.Should().Be(created);
    }

    [Fact]
    public void MapToDelegationChangeEventList_MultipleChanges_MapsAll()
    {
        var changes = Enumerable.Range(1, 5).Select(i => new DelegationChange
        {
            DelegationChangeId = i,
            ResourceId = $"app-{i}",
            DelegationChangeType = DelegationChangeType.Grant,
            Created = DateTime.UtcNow,
        }).ToList();

        var result = _sut.MapToDelegationChangeEventList(changes);

        result.DelegationChangeEvents.Should().HaveCount(5);
        result.DelegationChangeEvents.Select(e => e.DelegationChange.DelegationChangeId)
            .Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }
}
