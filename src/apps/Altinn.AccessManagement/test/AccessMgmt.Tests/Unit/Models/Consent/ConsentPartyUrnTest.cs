using Altinn.AccessManagement.Core.Models.Consent;

namespace Altinn.AccessManagement.Tests.Unit.Models.Consent;

/// <summary>
/// Pins the hand-written party-id parse guard on <see cref="ConsentPartyUrn"/>.
/// The generated <c>[KeyValueUrn]</c> machinery is library code; these tests cover
/// only the custom <c>TryParsePartyId</c>, which uses <c>NumberStyles.None</c> so a
/// signed or whitespace-padded party id is rejected (a negative party id must never
/// parse).
/// </summary>
[UnitTest]
[Collection("Models Test")]
public class ConsentPartyUrnTest
{
    [Fact]
    public void TryParse_PositivePartyId_ReturnsTrueWithValue()
    {
        ConsentPartyUrn.TryParse("urn:altinn:party:id:5", out var urn).Should().BeTrue();

        urn!.IsPartyId(out var id).Should().BeTrue();
        id.Should().Be(5);
    }

    [Theory]
    [InlineData("urn:altinn:party:id:-5")]   // leading minus
    [InlineData("urn:altinn:party:id:+5")]   // leading plus
    [InlineData("urn:altinn:party:id: 5")]   // leading whitespace
    public void TryParse_SignedOrWhitespacePartyId_ReturnsFalse(string urn)
    {
        // TryParsePartyId parses with NumberStyles.None, so a sign or whitespace must be
        // rejected — guards against a refactor to a plain int.Parse that would accept them.
        ConsentPartyUrn.TryParse(urn, out _).Should().BeFalse();
    }
}
