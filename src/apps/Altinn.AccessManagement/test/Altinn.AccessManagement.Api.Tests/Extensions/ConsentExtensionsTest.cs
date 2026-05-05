using System.Security.Claims;
using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Api.Internal.Models;
using Altinn.AccessManagement.Api.Internal.Utils;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Api.Tests.Extensions;

public class ConsentExtensionsTest
{
    // ── ConsentResourceAttributeExtensions ──────────────────────────────────
    [Fact]
    public void ToConsentResourceAttributeExternal_MapsTypeAndValue()
    {
        var core = new ConsentResourceAttribute { Type = "urn:altinn:resource", Value = "ttd-some-app" };

        var dto = core.ToConsentResourceAttributeExternal();

        dto.Type.Should().Be("urn:altinn:resource");
        dto.Value.Should().Be("ttd-some-app");
    }

    // ── ConsentRightExtensions ───────────────────────────────────────────────
    [Fact]
    public void ToConsentRightExternal_EmptyResourceList_MapsToEmptyList()
    {
        var core = new ConsentRight { Action = ["read"], Resource = [] };

        var dto = core.ToConsentRightExternal();

        dto.Action.Should().ContainSingle("read");
        dto.Resource.Should().BeEmpty();
        dto.Metadata.Should().BeNull();
    }

    [Fact]
    public void ToConsentRightExternal_WithResources_MapsEachAttribute()
    {
        var core = new ConsentRight
        {
            Action = ["read", "write"],
            Resource =
            [
                new ConsentResourceAttribute { Type = "urn:altinn:resource", Value = "app1" },
                new ConsentResourceAttribute { Type = "urn:altinn:org", Value = "ttd" },
            ],
        };

        var dto = core.ToConsentRightExternal();

        dto.Resource.Should().HaveCount(2);
        dto.Resource[0].Type.Should().Be("urn:altinn:resource");
        dto.Resource[1].Value.Should().Be("ttd");
    }

    // ── ConsentContextExternalExtensions ────────────────────────────────────
    [Fact]
    public void ToConsentContext_MapsLanguage()
    {
        var contextDto = new ConsentContextDto { Language = "nb" };

        var context = contextDto.ToConsentContext();

        context.Language.Should().Be("nb");
    }

    // ── ConsentRequestEventExtensions ───────────────────────────────────────
    [Fact]
    public void ToConsentRequestEventExternal_PerformedByOrganizationId_MapsToOrganizationIdUrn()
    {
        var orgNo = OrganizationNumber.Parse("937884117");
        var core = new ConsentRequestEvent
        {
            ConsentEventID = Guid.NewGuid(),
            ConsentRequestID = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            EventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType.Accepted,
            PerformedBy = Core.Models.Consent.ConsentPartyUrn.OrganizationId.Create(orgNo),
        };

        var dto = core.ToConsentRequestEventExternal();

        dto.ConsentEventID.Should().Be(core.ConsentEventID);
        dto.ConsentRequestID.Should().Be(core.ConsentRequestID);
        dto.PerformedBy.IsOrganizationId(out _).Should().BeTrue();
        dto.EventType.Should().Be(Authorization.Api.Contracts.Consent.ConsentRequestEventType.Accepted);
    }

    [Fact]
    public void ToConsentRequestEventExternal_PerformedByPersonId_MapsToPersonIdUrn()
    {
        var personId = PersonIdentifier.Parse("01025161013");
        var core = new ConsentRequestEvent
        {
            ConsentEventID = Guid.NewGuid(),
            ConsentRequestID = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            EventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType.Revoked,
            PerformedBy = Core.Models.Consent.ConsentPartyUrn.PersonId.Create(personId),
        };

        var dto = core.ToConsentRequestEventExternal();

        dto.PerformedBy.IsPersonId(out _).Should().BeTrue();
    }

    [Fact]
    public void ToConsentRequestEventExternal_PerformedByPartyUuid_MapsToPartyUuidUrn()
    {
        var uuid = Guid.NewGuid();
        var core = new ConsentRequestEvent
        {
            ConsentEventID = Guid.NewGuid(),
            ConsentRequestID = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            EventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType.Rejected,
            PerformedBy = Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(uuid),
        };

        var dto = core.ToConsentRequestEventExternal();

        dto.PerformedBy.IsPartyUuid(out var resultUuid).Should().BeTrue();
        resultUuid.Should().Be(uuid);
    }

    [Fact]
    public void ToConsentRequestEventExternal_PerformedByPartyId_ThrowsArgumentException()
    {
        var core = new ConsentRequestEvent
        {
            ConsentEventID = Guid.NewGuid(),
            ConsentRequestID = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            EventType = Altinn.AccessManagement.Core.Models.Consent.ConsentRequestEventType.Accepted,
            PerformedBy = Core.Models.Consent.ConsentPartyUrn.PartyId.Create(12345),
        };

        var act = () => core.ToConsentRequestEventExternal();
        act.Should().Throw<ArgumentException>().WithMessage("Unknown consent party urn");
    }

    // ── ConsentRequestDetailsExtensions ─────────────────────────────────────
    [Fact]
    public void ToConsentRequestDetailsBFF_HappyPath_MapsAllScalars()
    {
        var toUuid = Guid.NewGuid();
        var fromUuid = Guid.NewGuid();
        var details = BuildMinimalDetails(toUuid, fromUuid);

        var dto = details.ToConsentRequestDetailsBFF();

        dto.Id.Should().Be(details.Id);
        dto.To.IsPartyUuid(out var t).Should().BeTrue();
        t.Should().Be(toUuid);
        dto.From.IsPartyUuid(out var f).Should().BeTrue();
        f.Should().Be(fromUuid);
        dto.RequiredDelegator.Should().BeNull();
        dto.HandledBy.Should().BeNull();
        dto.ValidTo.Should().Be(details.ValidTo);
        dto.RedirectUrl.Should().Be(details.RedirectUrl);
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_WithOptionalUuids_MapsRequiredDelegatorAndHandledBy()
    {
        var delegatorUuid = Guid.NewGuid();
        var handledByUuid = Guid.NewGuid();
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.RequiredDelegator = Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(delegatorUuid);
        details.HandledBy = Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(handledByUuid);

        var dto = details.ToConsentRequestDetailsBFF();

        dto.RequiredDelegator!.IsPartyUuid(out var d).Should().BeTrue();
        d.Should().Be(delegatorUuid);
        dto.HandledBy!.IsPartyUuid(out var h).Should().BeTrue();
        h.Should().Be(handledByUuid);
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_PortalViewModeHide_MapsToHide()
    {
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.PortalViewMode = Altinn.AccessManagement.Core.Models.Consent.ConsentPortalViewMode.Hide;

        var dto = details.ToConsentRequestDetailsBFF();

        dto.PortalViewMode.Should().Be(Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Hide);
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_PortalViewModeShow_MapsToShow()
    {
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.PortalViewMode = Altinn.AccessManagement.Core.Models.Consent.ConsentPortalViewMode.Show;

        var dto = details.ToConsentRequestDetailsBFF();

        dto.PortalViewMode.Should().Be(Authorization.Api.Contracts.Consent.ConsentPortalViewMode.Show);
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_PortalViewModeUnknown_MapsToNull()
    {
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.PortalViewMode = (Altinn.AccessManagement.Core.Models.Consent.ConsentPortalViewMode)99;

        var dto = details.ToConsentRequestDetailsBFF();

        dto.PortalViewMode.Should().BeNull();
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_ToIsNotPartyUuid_ThrowsArgumentException()
    {
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.To = Core.Models.Consent.ConsentPartyUrn.PartyId.Create(9999);

        var act = () => details.ToConsentRequestDetailsBFF();
        act.Should().Throw<ArgumentException>().WithMessage("Unknown consent party urn");
    }

    [Fact]
    public void ToConsentRequestDetailsBFF_FromIsNotPartyUuid_ThrowsArgumentException()
    {
        var details = BuildMinimalDetails(Guid.NewGuid(), Guid.NewGuid());
        details.From = Core.Models.Consent.ConsentPartyUrn.PartyId.Create(9999);

        var act = () => details.ToConsentRequestDetailsBFF();
        act.Should().Throw<ArgumentException>().WithMessage("Unknown consent party urn");
    }

    // ── UserUtil ─────────────────────────────────────────────────────────────
    [Fact]
    public void GetUserUuid_NullPrincipal_ReturnsNull()
    {
        UserUtil.GetUserUuid(null).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_NoMatchingClaim_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("urn:altinn:userid", "123")]));
        UserUtil.GetUserUuid(principal).Should().BeNull();
    }

    [Fact]
    public void GetUserUuid_ValidUuidClaim_ReturnsUuid()
    {
        var uuid = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("urn:altinn:party:uuid", uuid.ToString())]));
        UserUtil.GetUserUuid(principal).Should().Be(uuid);
    }

    [Fact]
    public void GetUserUuid_ClaimNotValidGuid_ReturnsNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("urn:altinn:party:uuid", "not-a-guid")]));
        UserUtil.GetUserUuid(principal).Should().BeNull();
    }

    // ── PagingInput ──────────────────────────────────────────────────────────
    [Fact]
    public void ToOpaqueToken_RoundTrip_RestoresValues()
    {
        var input = new PagingInput { PageSize = 25, PageNumber = 3 };

        var token = input.ToOpaqueToken();
        var restored = PagingInput.CreateFromToken(token);

        restored.PageSize.Should().Be(25);
        restored.PageNumber.Should().Be(3);
    }

    [Fact]
    public void ToOpaqueToken_DefaultValues_RoundTripSucceeds()
    {
        var input = new PagingInput();
        var token = input.ToOpaqueToken();
        var restored = PagingInput.CreateFromToken(token);

        restored.PageSize.Should().Be(100);
        restored.PageNumber.Should().Be(0);
    }

    [Fact]
    public void GetExamples_ReturnsNonNullInstance()
    {
        var example = new PagingInput().GetExamples();
        example.Should().NotBeNull();
        example.PageNumber.Should().Be(2);
        example.PageSize.Should().Be(56);
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static ConsentRequestDetails BuildMinimalDetails(Guid toUuid, Guid fromUuid) =>
        new()
        {
            Id = Guid.NewGuid(),
            To = Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(toUuid),
            From = Core.Models.Consent.ConsentPartyUrn.PartyUuid.Create(fromUuid),
            ValidTo = DateTimeOffset.UtcNow.AddDays(30),
            ConsentRights = [],
            ConsentRequestEvents = [],
            RedirectUrl = "https://example.com/redirect",
        };
}
