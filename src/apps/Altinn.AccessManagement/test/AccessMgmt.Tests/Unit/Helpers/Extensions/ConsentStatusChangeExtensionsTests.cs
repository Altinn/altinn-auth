using Altinn.AccessManagement.Api.Enterprise.Extensions;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

// `ConsentRequestEventType` is defined in both namespaces above (Core uses it
// on the domain model `ConsentStatusChange`; Contracts uses it on wire types).
// The alias forces every unqualified `ConsentRequestEventType` reference in
// this file to resolve to the Core version — which is what
// `ConsentStatusChange.EventType` actually expects.
using ConsentRequestEventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType;

namespace Altinn.AccessManagement.Tests.Unit.Helpers.Extensions;

// ---------------------------------------------------------------------------
// Pure-unit-test example.
//
// `ConsentStatusChangeExtensions.ToDto` has no dependencies — no DB, no HTTP,
// no DI graph. The simplest test type per docs/testing/WRITING_TESTS.md
// applies: construct the input, invoke the method, assert on the output. No
// fixture, no Moq.
//
// Conventions used here:
//  - Test naming follows `MethodUnderTest_Scenario_ExpectedResult`
//    (docs/testing/TEST_NAMING_CONVENTION.md).
//  - Assertions use AwesomeAssertions (`.Should()`); both Xunit and
//    AwesomeAssertions are global usings via Directory.Build.targets, so no
//    `using` directives are needed for them.
// ---------------------------------------------------------------------------
[UnitTest]
public class ConsentStatusChangeExtensionsTests
{
    [Fact]
    public void ToDto_WithFullyPopulatedSource_CopiesAllProperties()
    {
        // Arrange — pick non-default values for every property so that any
        // accidental swap between fields will surface in the assertion.
        var source = new ConsentStatusChange
        {
            ConsentRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            EventType = ConsentRequestEventType.Accepted,
            ChangedDate = new DateTimeOffset(2026, 4, 29, 12, 0, 0, TimeSpan.Zero),
            ConsentEventId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        };

        // Act
        ConsentStatusChangeDto dto = source.ToDto();

        // Assert — `BeEquivalentTo` performs a structural compare, which is
        // the preferred form for DTO mappings (see
        // docs/testing/FLUENT_ASSERTIONS_GUIDELINES.md).
        dto.Should().BeEquivalentTo(new ConsentStatusChangeDto
        {
            ConsentRequestId = source.ConsentRequestId,
            EventType = "Accepted",
            ChangedDate = source.ChangedDate,
        });
    }

    [Theory]
    [InlineData(ConsentRequestEventType.Created, "Created")]
    [InlineData(ConsentRequestEventType.Rejected, "Rejected")]
    [InlineData(ConsentRequestEventType.Accepted, "Accepted")]
    [InlineData(ConsentRequestEventType.Revoked, "Revoked")]
    public void ToDto_ForKnownEventType_SerialisesEnumNameAsString(
        ConsentRequestEventType eventType,
        string expected)
    {
        // The DTO's EventType is a string built from `enum.ToString()`. This
        // theory pins the contract for the four event types exposed today
        // (see docs/testing/WRITING_TESTS.md on xUnit v3 `TheoryData`/inline
        // data).
        var source = new ConsentStatusChange
        {
            ConsentRequestId = Guid.NewGuid(),
            EventType = eventType,
            ChangedDate = DateTimeOffset.UtcNow,
            ConsentEventId = Guid.NewGuid(),
        };

        source.ToDto().EventType.Should().Be(expected);
    }

    [Theory]
    [InlineData(ConsentRequestEventType.Deleted, "Deleted")]
    [InlineData(ConsentRequestEventType.Expired, "Expired")]
    [InlineData(ConsentRequestEventType.Used, "Used")]
    public void ToDto_ForDeletedExpiredOrUsedEvent_SerialisesEnumNameAsString(
        ConsentRequestEventType eventType,
        string expected)
    {
        // These event types are not surfaced on the receiver listing today, but the mapping must
        // still serialise them by enum name rather than throwing or emitting a number. Kept separate
        // from the four currently-surfaced cases so a wire-format change points to the right group.
        var source = new ConsentStatusChange
        {
            ConsentRequestId = Guid.NewGuid(),
            EventType = eventType,
            ChangedDate = DateTimeOffset.UtcNow,
            ConsentEventId = Guid.NewGuid(),
        };

        source.ToDto().EventType.Should().Be(expected);
    }

    [Fact]
    public void ToDto_PreservesChangedDateTimeZoneOffset()
    {
        // ChangedDate is a DateTimeOffset; the mapping must carry the original offset through rather
        // than normalising to UTC, so the receiver sees the timestamp as it was recorded.
        var changed = new DateTimeOffset(2026, 4, 29, 14, 30, 0, TimeSpan.FromHours(2));
        var source = new ConsentStatusChange
        {
            ConsentRequestId = Guid.NewGuid(),
            EventType = ConsentRequestEventType.Accepted,
            ChangedDate = changed,
            ConsentEventId = Guid.NewGuid(),
        };

        var dto = source.ToDto();

        dto.ChangedDate.Should().Be(changed);
        dto.ChangedDate.Offset.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void ToDto_DoesNotSurfaceConsentEventId()
    {
        // ConsentEventId is internal — it only feeds the pagination cursor and must never appear on
        // the DTO. Pin it by reflection so adding it to the contract has to break a test on purpose.
        typeof(ConsentStatusChangeDto).GetProperties()
            .Select(p => p.Name)
            .Should().NotContain("ConsentEventId");
    }
}
