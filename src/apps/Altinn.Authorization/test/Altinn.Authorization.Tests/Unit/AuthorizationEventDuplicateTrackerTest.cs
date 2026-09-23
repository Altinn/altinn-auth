using System.Text.Json;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Services.Implementation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Altinn.Authorization.Tests.Unit;

[UnitTest]
public class AuthorizationEventDuplicateTrackerTest
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Track_FirstOccurrence_IsNotDuplicate()
    {
        var tracker = CreateTracker();

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent()));
    }

    [Fact]
    public void Track_RepeatInSameTrace_IsSameTrace()
    {
        var tracker = CreateTracker();

        tracker.Track(CreateEvent(traceId: "trace-1"));

        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(CreateEvent(traceId: "trace-1")));
    }

    [Fact]
    public void Track_RepeatInOtherTrace_IsWindow()
    {
        var tracker = CreateTracker();

        tracker.Track(CreateEvent(traceId: "trace-1"));

        Assert.Equal(AuthorizationEventDuplicateKind.Window, tracker.Track(CreateEvent(traceId: "trace-2")));
    }

    [Fact]
    public void Track_FurtherRepeatInOtherTrace_IsSameTrace()
    {
        var tracker = CreateTracker();

        tracker.Track(CreateEvent(traceId: "trace-1"));
        tracker.Track(CreateEvent(traceId: "trace-2"));

        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(CreateEvent(traceId: "trace-2")));
    }

    [Theory]
    [InlineData(nameof(AuthorizationEvent.Resource))]
    [InlineData(nameof(AuthorizationEvent.InstanceId))]
    [InlineData(nameof(AuthorizationEvent.ResourcePartyId))]
    [InlineData(nameof(AuthorizationEvent.SubjectUserId))]
    [InlineData(nameof(AuthorizationEvent.SubjectParty))]
    [InlineData(nameof(AuthorizationEvent.SubjectPartyUuid))]
    [InlineData(nameof(AuthorizationEvent.SubjectOrgCode))]
    [InlineData(nameof(AuthorizationEvent.SubjectOrgNumber))]
    [InlineData(nameof(AuthorizationEvent.SessionId))]
    [InlineData(nameof(AuthorizationEvent.IpAdress))]
    [InlineData(nameof(AuthorizationEvent.Operation))]
    [InlineData(nameof(AuthorizationEvent.Decision))]
    public void Track_EventsDifferingInLoggedField_AreNotDuplicates(string field)
    {
        var tracker = CreateTracker();
        AuthorizationEvent other = CreateEvent();
        switch (field)
        {
            case nameof(AuthorizationEvent.Resource): other.Resource = "app_ttd_other"; break;
            case nameof(AuthorizationEvent.InstanceId): other.InstanceId = "50001/2a0b8c0e-0000-0000-0000-000000000000"; break;
            case nameof(AuthorizationEvent.ResourcePartyId): other.ResourcePartyId = 50002; break;
            case nameof(AuthorizationEvent.SubjectUserId): other.SubjectUserId = 20002; break;
            case nameof(AuthorizationEvent.SubjectParty): other.SubjectParty = 50003; break;
            case nameof(AuthorizationEvent.SubjectPartyUuid): other.SubjectPartyUuid = "6f1a5b77-0000-0000-0000-000000000000"; break;
            case nameof(AuthorizationEvent.SubjectOrgCode): other.SubjectOrgCode = "ttd"; break;
            case nameof(AuthorizationEvent.SubjectOrgNumber): other.SubjectOrgNumber = 991825827; break;
            case nameof(AuthorizationEvent.SessionId): other.SessionId = "session-2"; break;
            case nameof(AuthorizationEvent.IpAdress): other.IpAdress = "51.120.0.115"; break;
            case nameof(AuthorizationEvent.Operation): other.Operation = "write"; break;
            case nameof(AuthorizationEvent.Decision): other.Decision = XacmlContextDecision.Deny; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, null);
        }

        tracker.Track(CreateEvent());

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(other));
    }

    [Fact]
    public void Track_NullAndEmptyString_AreTheSame()
    {
        // The event mapping uses empty strings for missing values, so null carries no extra meaning.
        var tracker = CreateTracker();
        AuthorizationEvent withNull = CreateEvent();
        withNull.InstanceId = null;
        AuthorizationEvent withEmpty = CreateEvent();
        withEmpty.InstanceId = string.Empty;

        tracker.Track(withNull);

        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(withEmpty));
    }

    [Fact]
    public void Track_MissingDecision_IsNotPermit()
    {
        // Permit is 0, so a missing decision must not be hashed as 0.
        var tracker = CreateTracker();
        AuthorizationEvent withoutDecision = CreateEvent();
        withoutDecision.Decision = null;

        tracker.Track(CreateEvent());

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(withoutDecision));
    }

    [Fact]
    public void Track_EventsDifferingOnlyInTimestampAndContextRequest_AreDuplicates()
    {
        var tracker = CreateTracker();
        AuthorizationEvent other = CreateEvent();
        other.Created = other.Created!.Value.AddSeconds(5);
        other.ContextRequestJson = JsonSerializer.SerializeToElement(new { other = true });

        tracker.Track(CreateEvent());

        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(other));
    }

    [Fact]
    public void Track_RepeatJustBeforeWindowEnds_IsDuplicate()
    {
        var tracker = CreateTracker();

        tracker.Track(CreateEvent(traceId: "trace-1"));
        _timeProvider.Advance(Window - TimeSpan.FromSeconds(1));

        Assert.Equal(AuthorizationEventDuplicateKind.Window, tracker.Track(CreateEvent(traceId: "trace-2")));
    }

    [Fact]
    public void Track_RepeatAfterWindow_IsNotDuplicate()
    {
        var tracker = CreateTracker();

        tracker.Track(CreateEvent(traceId: "trace-1"));
        _timeProvider.Advance(Window);

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-2")));
    }

    [Fact]
    public void Track_ContinuousRepeats_AreReportedAsNewOncePerWindow()
    {
        var tracker = CreateTracker();

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-1")));

        _timeProvider.Advance(TimeSpan.FromSeconds(40));
        Assert.Equal(AuthorizationEventDuplicateKind.Window, tracker.Track(CreateEvent(traceId: "trace-2")));

        // The window runs from the first occurrence, so the repeats in between do not extend it.
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-3")));
    }

    [Fact]
    public void Track_RepeatAfterGenerationRotation_IsStillDuplicateWithinWindow()
    {
        var tracker = CreateTracker();
        AuthorizationEvent early = CreateEvent(traceId: "trace-1");
        AuthorizationEvent late = CreateEvent(traceId: "trace-1");
        late.Operation = "write";

        tracker.Track(early);
        _timeProvider.Advance(TimeSpan.FromSeconds(50));
        tracker.Track(late);

        // Starts a new generation. The late event is 20 seconds old, the early one 70.
        _timeProvider.Advance(TimeSpan.FromSeconds(20));

        AuthorizationEvent lateRepeat = CreateEvent(traceId: "trace-2");
        lateRepeat.Operation = "write";
        Assert.Equal(AuthorizationEventDuplicateKind.Window, tracker.Track(lateRepeat));
        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-2")));
    }

    [Fact]
    public void Track_AtCapacity_NewEventIsUntracked()
    {
        // Room for a single event: its event key and its trace key.
        var tracker = CreateTracker(maxTrackedEvents: 2);
        AuthorizationEvent other = CreateEvent();
        other.Operation = "write";

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-1")));
        Assert.Equal(AuthorizationEventDuplicateKind.Untracked, tracker.Track(other));
    }

    [Fact]
    public void Track_NearCapacity_RepeatInOtherTrace_NeedsRoomOnlyForItsTrace()
    {
        // The first event takes two entries, and a repeat from another trace only one more.
        var tracker = CreateTracker(maxTrackedEvents: 3);

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(CreateEvent(traceId: "trace-a")));
        Assert.Equal(AuthorizationEventDuplicateKind.Window, tracker.Track(CreateEvent(traceId: "trace-b")));
        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(CreateEvent(traceId: "trace-b")));
    }

    [Fact]
    public void Track_AtCapacity_RepeatWhoseTraceCannotBeRemembered_IsUntracked()
    {
        // Full after the first event, so the second trace cannot be remembered. Reporting the repeat as
        // Window would make every further repeat in that trace look like Window too.
        var tracker = CreateTracker(maxTrackedEvents: 2);

        tracker.Track(CreateEvent(traceId: "trace-a"));

        Assert.Equal(AuthorizationEventDuplicateKind.Untracked, tracker.Track(CreateEvent(traceId: "trace-b")));
        Assert.Equal(AuthorizationEventDuplicateKind.Untracked, tracker.Track(CreateEvent(traceId: "trace-b")));
        Assert.Equal(AuthorizationEventDuplicateKind.SameTrace, tracker.Track(CreateEvent(traceId: "trace-a")));
    }

    [Fact]
    public void Track_AtCapacity_HasRoomAgainAfterWindow()
    {
        var tracker = CreateTracker(maxTrackedEvents: 2);
        AuthorizationEvent other = CreateEvent();
        other.Operation = "write";

        tracker.Track(CreateEvent());
        _timeProvider.Advance(Window);

        Assert.Equal(AuthorizationEventDuplicateKind.None, tracker.Track(other));
    }

    private AuthorizationEventDuplicateTracker CreateTracker(int maxTrackedEvents = 1000) =>
        new(Options.Create(new AuditLogDeduplicationSettings { Window = Window, MaxTrackedEvents = maxTrackedEvents }), _timeProvider);

    private AuthorizationEvent CreateEvent(string traceId = "trace-1") => new()
    {
        Created = _timeProvider.GetUtcNow(),
        Resource = "app_ttd_test",
        InstanceId = "50001/1d9ef9a5-0000-0000-0000-000000000000",
        ResourcePartyId = 50001,
        SubjectUserId = 20001,
        SubjectParty = 50001,
        SubjectPartyUuid = null,
        SubjectOrgCode = null,
        SubjectOrgNumber = null,
        SessionId = "session-1",
        IpAdress = "51.120.0.114",
        Operation = "read",
        Decision = XacmlContextDecision.Permit,
        ContextRequestJson = JsonSerializer.SerializeToElement(new { }),
        TraceId = traceId,
    };
}
