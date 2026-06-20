using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Register;

namespace Altinn.AccessManagement.Tests.Unit.Models.Register;

/// <summary>
/// Pure-logic tests for the <see cref="OrganizationNumber"/> in
/// <c>Altinn.AccessManagement.Core.Models.Register</c>. Unlike the
/// checksum-validating type in <c>Altinn.Authorization.Api.Contracts.Register</c>,
/// this one validates length + digits only (no mod-11), and exposes
/// <see cref="OrganizationNumber.CreateUnchecked"/>.
/// </summary>
[UnitTest]
[Collection("Models Test")]
public class OrganizationNumberTest
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Parse_ValidNineDigits_ReturnsOrganizationNumber()
    {
        OrganizationNumber.Parse("987654321").ToString().Should().Be("987654321");
    }

    [Theory]
    [InlineData("12345678")] // 8 digits
    [InlineData("1234567890")] // 10 digits
    public void TryParse_WrongLength_ReturnsFalse(string value)
    {
        OrganizationNumber.TryParse(value, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_NonNumeric_ReturnsFalse()
    {
        OrganizationNumber.TryParse("12345678X", null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        OrganizationNumber.TryParse(null, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_NineDigitBadChecksum_ReturnsTrue()
    {
        // This type validates length + digits only (no mod-11 checksum), so a value the
        // checksum-validating Altinn.Authorization.Api.Contracts.Register.OrganizationNumber
        // rejects is accepted here. Pins the deliberate cross-type validation divergence.
        OrganizationNumber.TryParse("937884118", null, out var result).Should().BeTrue();
        result!.ToString().Should().Be("937884118");
    }

    [Fact]
    public void CreateUnchecked_InvalidValue_BypassesValidation()
    {
        OrganizationNumber.CreateUnchecked("not-an-org").ToString().Should().Be("not-an-org");
    }

    [Fact]
    public void Json_DeserializeInvalid_ThrowsJsonException()
    {
        Action act = () => JsonSerializer.Deserialize<OrganizationNumber>("\"12345\"", JsonOptions);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Json_RoundTrip_PreservesValue()
    {
        var orgNr = OrganizationNumber.Parse("987654321");
        var json = JsonSerializer.Serialize(orgNr, JsonOptions);

        json.Should().Be("\"987654321\"");
        JsonSerializer.Deserialize<OrganizationNumber>(json, JsonOptions)!.ToString().Should().Be("987654321");
    }
}
