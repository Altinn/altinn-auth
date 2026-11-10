using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Outbox;

/// <summary>
/// Pure unit tests for the email text built by <see cref="RequestReviewedNotificationHandler"/>.
/// </summary>
[UnitTest]
public class RequestReviewedNotificationHandlerTest
{
    // ── GetStatusLabel ──────────────────────────────────────────────────────
    [Theory]
    [InlineData(RequestStatusV2.Approved, "Godkjent.")]
    [InlineData(RequestStatusV2.Rejected, "Avslått.")]
    [InlineData(RequestStatusV2.PartialApprove, "Godkjent med justeringer.")]
    public void GetStatusLabel_ReviewOutcome_ReturnsLabel(RequestStatusV2 status, string expected)
    {
        RequestReviewedNotificationHandler.GetStatusLabel(status, isApproved: false).Should().Be(expected);
        RequestReviewedNotificationHandler.GetStatusLabel(status, isApproved: true).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "Godkjent.")]
    [InlineData(false, "Avslått.")]
    public void GetStatusLabel_NoStatus_FallsBackToIsApproved(bool isApproved, string expected)
    {
        RequestReviewedNotificationHandler.GetStatusLabel(null, isApproved).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "Godkjent.")]
    [InlineData(false, "Avslått.")]
    public void GetStatusLabel_StatusThatIsNotAReviewOutcome_FallsBackToIsApproved(bool isApproved, string expected)
    {
        RequestReviewedNotificationHandler.GetStatusLabel(RequestStatusV2.Pending, isApproved).Should().Be(expected);
    }

    // ── BuildEmailBody ──────────────────────────────────────────────────────
    [Fact]
    public void BuildEmailBody_SketchFromIssue_MatchesExactly()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [
                Package("Lønn", RequestStatusV2.Approved),
                Package("Ordinær post til virksomheten", RequestStatusV2.Rejected),
                Resource("Melding fra Regnskapsregisteret", RequestStatusV2.PartialApprove),
                Resource("Motta nabo- og planvarsel", RequestStatusV2.Approved),
            ]);

        var expected = """
            <p>Baker Hansen AS med Org.nr 313955966 har svart på din forespørsel om fullmakter.</p>
            <strong>Tilgangspakker:</strong><ul>
            <li>Lønn. <strong><em>Godkjent.</em></strong></li>
            <li>Ordinær post til virksomheten. <strong><em>Avslått.</em></strong></li>
            </ul>
            <strong>Enkelttjenester:</strong><ul>
            <li>Melding fra Regnskapsregisteret. <strong><em>Godkjent med justeringer.</em></strong></li>
            <li>Motta nabo- og planvarsel. <strong><em>Godkjent.</em></strong></li>
            </ul>
            <p>Med vennlig hilsen,<br>Altinn</p>
            <em>Denne meldingen er automatisk generert. Svar til denne adressen vil ikke bli behandlet.</em>

            """;

        Normalize(body).Should().Be(Normalize(expected));
    }

    [Fact]
    public void BuildEmailBody_PersonReviewer_OpensWithoutOrgNumber()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Person("Kari Nordmann"),
            [Resource("Motta nabo- og planvarsel", RequestStatusV2.Approved)]);

        body.Should().StartWith("<p>Kari Nordmann har svart på din forespørsel om fullmakter.</p>");
        body.Should().NotContain("Org.nr");
    }

    [Fact]
    public void BuildEmailBody_UnsupportedReviewerType_Throws()
    {
        var reviewer = new Entity { Id = Guid.CreateVersion7(), Name = "System", TypeId = EntityTypeConstants.SystemUser };

        var act = () => RequestReviewedNotificationHandler.BuildEmailBody(
            reviewer,
            [Resource("Motta nabo- og planvarsel", RequestStatusV2.Approved)]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void BuildEmailBody_MixedOutcomesWithinType_ListsThemUnderOneHeading()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [
                Resource("Melding fra Regnskapsregisteret", RequestStatusV2.PartialApprove),
                Resource("Motta nabo- og planvarsel", RequestStatusV2.Rejected),
                Resource("Byggesøknad", RequestStatusV2.Approved),
            ]);

        CountOccurrences(body, "Enkelttjenester:").Should().Be(1);
        body.Should().Contain("<li>Melding fra Regnskapsregisteret. <strong><em>Godkjent med justeringer.</em></strong></li>");
        body.Should().Contain("<li>Motta nabo- og planvarsel. <strong><em>Avslått.</em></strong></li>");
        body.Should().Contain("<li>Byggesøknad. <strong><em>Godkjent.</em></strong></li>");
    }

    [Fact]
    public void BuildEmailBody_DoesNotUseTheOldOutcomeSections()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [
                Package("Lønn", RequestStatusV2.Approved),
                Package("Ordinær post til virksomheten", RequestStatusV2.Rejected),
            ]);

        body.Should().NotContain("Aksepterte");
        body.Should().NotContain("Avslåtte");
    }

    [Fact]
    public void BuildEmailBody_OnlyPackages_LeavesOutServicesHeading()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [Package("Lønn", RequestStatusV2.Approved)]);

        body.Should().Contain("Tilgangspakker:");
        body.Should().NotContain("Enkelttjenester:");
    }

    [Fact]
    public void BuildEmailBody_OnlyServices_LeavesOutPackagesHeading()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [Resource("Motta nabo- og planvarsel", RequestStatusV2.PartialApprove)]);

        body.Should().Contain("Enkelttjenester:");
        body.Should().NotContain("Tilgangspakker:");
    }

    [Fact]
    public void BuildEmailBody_KeepsItemOrderWithinType_AndListsPackagesFirst()
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [
                Resource("Motta nabo- og planvarsel", RequestStatusV2.Approved),
                Package("Ordinær post til virksomheten", RequestStatusV2.Approved),
                Resource("Byggesøknad", RequestStatusV2.Approved),
                Package("Lønn", RequestStatusV2.Approved),
            ]);

        var positions = new[] { "Tilgangspakker:", "Ordinær post til virksomheten", "Lønn", "Enkelttjenester:", "Motta nabo- og planvarsel", "Byggesøknad" }
            .Select(text => body.IndexOf(text, StringComparison.Ordinal))
            .ToList();

        positions.Should().NotContain(-1);
        positions.Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData(true, "Godkjent.")]
    [InlineData(false, "Avslått.")]
    public void BuildEmailBody_ItemWithoutStatus_UsesIsApproved(bool isApproved, string expectedLabel)
    {
        var body = RequestReviewedNotificationHandler.BuildEmailBody(
            Organization("Baker Hansen AS", "313955966"),
            [new ReviewedItem("Lønn", ReviewedItemType.Package, null, isApproved)]);

        body.Should().Contain($"<li>Lønn. <strong><em>{expectedLabel}</em></strong></li>");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static Entity Organization(string name, string orgNo) =>
        new() { Id = Guid.CreateVersion7(), Name = name, OrganizationIdentifier = orgNo, TypeId = EntityTypeConstants.Organization };

    private static Entity Person(string name) =>
        new() { Id = Guid.CreateVersion7(), Name = name, TypeId = EntityTypeConstants.Person };

    private static ReviewedItem Package(string name, RequestStatusV2 status) =>
        new(name, ReviewedItemType.Package, status, IsApproved: status != RequestStatusV2.Rejected);

    private static ReviewedItem Resource(string name, RequestStatusV2 status) =>
        new(name, ReviewedItemType.Resource, status, IsApproved: status != RequestStatusV2.Rejected);

    private static string Normalize(string text) => text.ReplaceLineEndings("\n");

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
