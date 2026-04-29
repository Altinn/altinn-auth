using Altinn.AccessManagement.Api.Enterprise.Extensions;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

// `ConsentRequestEventType` is defined in both namespaces above (Core uses it
// on the domain model `ConsentStatusChange`; Contracts uses it on wire types).
// The alias forces every unqualified `ConsentRequestEventType` reference in
// this file to resolve to the Core version — which is what
// `ConsentStatusChange.EventType` actually expects.
using ConsentRequestEventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType;

namespace AccessMgmt.Tests.Helpers.Extensions;

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
//  - Assertions use FluentAssertions (`.Should()`); both Xunit and
//    FluentAssertions are global usings via Directory.Build.targets, so no
//    `using` directives are needed for them.
// ---------------------------------------------------------------------------
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

    // -----------------------------------------------------------------------
    // TODO — additional cases the developer should add to reach full coverage
    // of `ToDto`. Each one is small (≤ 5 lines once written) and follows the
    // same arrange/act/assert shape as the tests above.
    //
    // 1. ToDto_ForDeletedExpiredOrUsedEvent_SerialisesEnumNameAsString
    //    Extend the [Theory] above with InlineData rows for `Deleted`,
    //    `Expired`, and `Used`. We split it out so that the four "currently
    //    surfaced" cases above stay separate from the "defensive" ones — when
    //    the wire format changes, the failure points to the right group.
    //
    // 2. ToDto_PreservesChangedDateTimeZoneOffset
    //    Construct a source with `ChangedDate` in a non-UTC offset (e.g.
    //    +02:00) and assert the DTO preserves the offset rather than
    //    silently normalising to UTC.
    //
    // 3. ToDto_DoesNotSurfaceConsentEventId
    //    The DTO intentionally omits `ConsentEventId` (see
    //    ConsentStatusChangeDto.cs — it's only used internally to build the
    //    pagination cursor). Assert via `dto.Should().NotBeOfType<...>()` or
    //    by reflection that no property exposes the internal event id. Add
    //    this test so a future "let's add it to the DTO" change has to break
    //    a test on purpose.
    // -----------------------------------------------------------------------
}
