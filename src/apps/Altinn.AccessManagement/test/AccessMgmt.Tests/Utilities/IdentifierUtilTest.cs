using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Utilities;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessManagement.Tests.Utilities;

/// <summary>
/// Pure-logic unit tests for <see cref="IdentifierUtil"/>.
/// All methods are static; no DI or containers required.
/// </summary>
public class IdentifierUtilTest
{
    // ── IsValidOrganizationNumber ─────────────────────────────────────────────
    [Fact]
    public void IsValidOrganizationNumber_KnownValid_ReturnsTrue()
    {
        // 974760673 — Brønnøysundregistrene; passes modulo-11 check
        IdentifierUtil.IsValidOrganizationNumber("974760673").Should().BeTrue();
    }

    [Fact]
    public void IsValidOrganizationNumber_WrongCheckDigit_ReturnsFalse()
    {
        // Flip the last digit of a known-valid number
        IdentifierUtil.IsValidOrganizationNumber("974760674").Should().BeFalse();
    }

    [Fact]
    public void IsValidOrganizationNumber_TooShort_ReturnsFalse()
    {
        IdentifierUtil.IsValidOrganizationNumber("97476067").Should().BeFalse();
    }

    [Fact]
    public void IsValidOrganizationNumber_TooLong_ReturnsFalse()
    {
        IdentifierUtil.IsValidOrganizationNumber("9747606730").Should().BeFalse();
    }

    [Fact]
    public void IsValidOrganizationNumber_ContainsLetter_ReturnsFalse()
    {
        IdentifierUtil.IsValidOrganizationNumber("97476067X").Should().BeFalse();
    }

    [Fact]
    public void IsValidOrganizationNumber_Empty_ReturnsFalse()
    {
        IdentifierUtil.IsValidOrganizationNumber(string.Empty).Should().BeFalse();
    }

    // ── MaskSSN ──────────────────────────────────────────────────────────────
    [Fact]
    public void MaskSSN_ReturnsFirstSixDigitsPlusFiveAsterisks()
    {
        IdentifierUtil.MaskSSN("02056260016").Should().Be("020562*****");
    }

    // ── GetIdentifierAsAttributeMatch — "organization" path ──────────────────
    [Fact]
    public void GetIdentifierAsAttributeMatch_OrgPath_ValidOrgNumber_ReturnsOrgMatch()
    {
        var ctx = MakeContextWithHeader(IdentifierUtil.OrganizationNumberHeader, "974760673");

        var result = IdentifierUtil.GetIdentifierAsAttributeMatch("organization", ctx);

        result.Id.Should().Be(AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationNumberAttribute);
        result.Value.Should().Be("974760673");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_OrgPath_MissingHeader_ThrowsArgumentException()
    {
        var ctx = new DefaultHttpContext();

        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("organization", ctx);

        act.Should().Throw<ArgumentException>().WithMessage("*Altinn-Party-OrganizationNumber*");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_OrgPath_InvalidOrgNumber_ThrowsArgumentException()
    {
        // "123456789" has check digit 5, not 9 — fails modulo-11 validation
        var ctx = MakeContextWithHeader(IdentifierUtil.OrganizationNumberHeader, "123456789");

        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("organization", ctx);

        act.Should().Throw<ArgumentException>().WithMessage("*not provide a well-formed organization number*");
    }

    // ── GetIdentifierAsAttributeMatch — "person" path ────────────────────────
    [Fact]
    public void GetIdentifierAsAttributeMatch_PersonPath_ValidSsn_ReturnsPersonMatch()
    {
        // "02056260016" is a synthetic SSN used throughout the existing test suite
        var ctx = MakeContextWithHeader(IdentifierUtil.PersonHeader, "02056260016");

        var result = IdentifierUtil.GetIdentifierAsAttributeMatch("person", ctx);

        result.Id.Should().Be(AltinnXacmlConstants.MatchAttributeIdentifiers.PersonId);
        result.Value.Should().Be("02056260016");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_PersonPath_MissingHeader_ThrowsArgumentException()
    {
        var ctx = new DefaultHttpContext();

        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("person", ctx);

        act.Should().Throw<ArgumentException>().WithMessage("*Altinn-Party-SocialSecurityNumber*");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_PersonPath_InvalidSsn_ThrowsArgumentException()
    {
        // Non-numeric string is always rejected by PersonIdentifier.TryParse
        var ctx = MakeContextWithHeader(IdentifierUtil.PersonHeader, "not-valid-ssn");

        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("person", ctx);

        act.Should().Throw<ArgumentException>().WithMessage("*not provide a well-formed national identity number*");
    }

    // ── GetIdentifierAsAttributeMatch — numeric party-id path ────────────────
    [Fact]
    public void GetIdentifierAsAttributeMatch_ValidPartyId_ReturnsPartyMatch()
    {
        var result = IdentifierUtil.GetIdentifierAsAttributeMatch("50001337", new DefaultHttpContext());

        result.Id.Should().Be(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute);
        result.Value.Should().Be("50001337");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_PartyIdZero_ThrowsArgumentException()
    {
        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("0", new DefaultHttpContext());

        act.Should().Throw<ArgumentException>().WithMessage("*not a well-formed party id*");
    }

    [Fact]
    public void GetIdentifierAsAttributeMatch_NonNumericParty_ThrowsArgumentException()
    {
        var act = () => IdentifierUtil.GetIdentifierAsAttributeMatch("unknown", new DefaultHttpContext());

        act.Should().Throw<ArgumentException>().WithMessage("*not a well-formed party id*");
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static DefaultHttpContext MakeContextWithHeader(string headerName, string headerValue)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers[headerName] = headerValue;
        return ctx;
    }
}
