using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Tests.Unit.Constants;

/// <summary>
/// Pins the activity type catalog to the events the database triggers can actually produce:
/// every producible (type, subtype, trigger, status) combination must resolve to a catalog
/// entry (exactly, or through the status-null fallback). A trigger change that introduces a
/// new combination without a catalog entry fails here instead of surfacing as a null
/// ActivityTypeId in the API.
/// </summary>
[UnitTest]
public class ActivityTypeConstantsTests
{
    /// <summary>
    /// Every event combination the triggers in ActivityLogTriggerScripts can emit. Statuses
    /// cover all request child events regardless of trigger, since the status column copies
    /// whatever the row holds at that point.
    /// </summary>
    public static TheoryData<ActivityLogType, ActivityLogSubtype?, ActivityLogTrigger, RequestStatus?> ProducibleEvents()
    {
        var data = new TheoryData<ActivityLogType, ActivityLogSubtype?, ActivityLogTrigger, RequestStatus?>();

        foreach (var trigger in new[] { ActivityLogTrigger.Created, ActivityLogTrigger.Deleted })
        {
            data.Add(ActivityLogType.Assignment, null, trigger, null);
            data.Add(ActivityLogType.Assignment, ActivityLogSubtype.Package, trigger, null);
            data.Add(ActivityLogType.Assignment, ActivityLogSubtype.Resource, trigger, null);
            data.Add(ActivityLogType.Assignment, ActivityLogSubtype.Instance, trigger, null);
            data.Add(ActivityLogType.Delegation, null, trigger, null);
            data.Add(ActivityLogType.Delegation, ActivityLogSubtype.Package, trigger, null);
            data.Add(ActivityLogType.Delegation, ActivityLogSubtype.Resource, trigger, null);
            data.Add(ActivityLogType.Request, null, trigger, null);
        }

        data.Add(ActivityLogType.Assignment, ActivityLogSubtype.Instance, ActivityLogTrigger.Updated, null);

        foreach (var subtype in new[] { ActivityLogSubtype.Package, ActivityLogSubtype.Resource })
        {
            foreach (var trigger in new[] { ActivityLogTrigger.Created, ActivityLogTrigger.Updated, ActivityLogTrigger.Deleted })
            {
                foreach (var status in Enum.GetValues<RequestStatus>())
                {
                    data.Add(ActivityLogType.Request, subtype, trigger, status);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ProducibleEvents))]
    public void Resolve_EveryProducibleEvent_HasCatalogEntry(ActivityLogType type, ActivityLogSubtype? subtype, ActivityLogTrigger trigger, RequestStatus? status)
    {
        var resolved = ActivityTypeConstants.Resolve(type, subtype, trigger, status);

        Assert.NotNull(resolved);
        Assert.Equal(type, resolved.Entity.Type);
        Assert.Equal(subtype, resolved.Entity.Subtype);
        Assert.Equal(trigger, resolved.Entity.Trigger);
    }

    [Fact]
    public void Resolve_StatusSpecificEntryBeatsFallback()
    {
        var approved = ActivityTypeConstants.Resolve(ActivityLogType.Request, ActivityLogSubtype.Package, ActivityLogTrigger.Updated, RequestStatus.Approved);

        Assert.NotNull(approved);
        Assert.Equal(ActivityTypeConstants.RequestPackageUpdatedApproved.Id, approved.Id);
    }

    [Fact]
    public void Resolve_UncoveredStatusFallsBackToStatusNullEntry()
    {
        var fallback = ActivityTypeConstants.Resolve(ActivityLogType.Request, ActivityLogSubtype.Package, ActivityLogTrigger.Updated, RequestStatus.None);

        Assert.NotNull(fallback);
        Assert.Equal(ActivityTypeConstants.RequestPackageUpdated.Id, fallback.Id);
    }

    [Fact]
    public void Resolve_SubtypeNullIsExactNotWildcard()
    {
        var parent = ActivityTypeConstants.Resolve(ActivityLogType.Assignment, null, ActivityLogTrigger.Created, null);
        var child = ActivityTypeConstants.Resolve(ActivityLogType.Assignment, ActivityLogSubtype.Package, ActivityLogTrigger.Created, null);

        Assert.NotNull(parent);
        Assert.NotNull(child);
        Assert.NotEqual(parent.Id, child.Id);
    }

    [Fact]
    public void Catalog_KeysAreUnique()
    {
        var duplicates = ActivityTypeConstants.AllEntities()
            .GroupBy(d => (d.Entity.Type, d.Entity.Subtype, d.Entity.Trigger, d.Entity.Status))
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalog_ConstantIdsAreUnique()
    {
        ConstantGuard.ConstantIdsAreUnique();
    }
}
