using System.Text.Json;
using Altinn.Authorization.Models.Register;

namespace Altinn.Platform.Authorization.Tests;

public class OrganizationNumberTest
{
    // --- Parse(string) ---
    [Fact]
    public void Parse_ValidNineDigitString_ReturnsOrganizationNumber()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        Assert.Equal("123456789", orgNo.ToString());
    }

    [Fact]
    public void Parse_InvalidString_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => OrganizationNumber.Parse("abc"));
    }

    // --- Parse(ReadOnlySpan<char>) ---
    [Fact]
    public void Parse_ValidSpan_ReturnsOrganizationNumber()
    {
        ReadOnlySpan<char> span = "987654321".AsSpan();
        var orgNo = OrganizationNumber.Parse(span);
        Assert.Equal("987654321", orgNo.ToString());
    }

    [Fact]
    public void Parse_InvalidSpan_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => OrganizationNumber.Parse("12345".AsSpan()));
    }

    // --- TryParse ---
    [Fact]
    public void TryParse_ValidString_ReturnsTrueAndResult()
    {
        bool success = OrganizationNumber.TryParse("123456789", null, out var result);
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal("123456789", result.ToString());
    }

    [Fact]
    public void TryParse_TooShort_ReturnsFalse()
    {
        bool success = OrganizationNumber.TryParse("12345678", null, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_TooLong_ReturnsFalse()
    {
        bool success = OrganizationNumber.TryParse("1234567890", null, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_ContainsLetters_ReturnsFalse()
    {
        bool success = OrganizationNumber.TryParse("12345678a", null, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        bool success = OrganizationNumber.TryParse((string)null, null, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_Span_ValidInput_ReturnsTrue()
    {
        bool success = OrganizationNumber.TryParse("111222333".AsSpan(), null, out var result);
        Assert.True(success);
        Assert.Equal("111222333", result.ToString());
    }

    // --- CreateUnchecked ---
    [Fact]
    public void CreateUnchecked_ReturnsInstanceWithValue()
    {
        var orgNo = OrganizationNumber.CreateUnchecked("000000000");
        Assert.Equal("000000000", orgNo.ToString());
    }

    // --- ToString overloads ---
    [Fact]
    public void ToString_WithFormat_ReturnsValue()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        Assert.Equal("123456789", orgNo.ToString("G"));
    }

    [Fact]
    public void ToString_WithFormatAndProvider_ReturnsValue()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        Assert.Equal("123456789", orgNo.ToString(null, null));
    }

    // --- TryFormat ---
    [Fact]
    public void TryFormat_SufficientBuffer_WritesValueAndReturnsTrue()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        Span<char> buffer = stackalloc char[20];
        bool success = orgNo.TryFormat(buffer, out int charsWritten, default, null);

        Assert.True(success);
        Assert.Equal(9, charsWritten);
        Assert.Equal("123456789", new string(buffer[..charsWritten]));
    }

    [Fact]
    public void TryFormat_InsufficientBuffer_ReturnsFalse()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        Span<char> buffer = stackalloc char[5];
        bool success = orgNo.TryFormat(buffer, out int charsWritten, default, null);

        Assert.False(success);
        Assert.Equal(0, charsWritten);
    }

    // --- JSON serialization ---
    [Fact]
    public void JsonRoundTrip_ValidOrgNumber_PreservesValue()
    {
        var orgNo = OrganizationNumber.Parse("123456789");
        string json = JsonSerializer.Serialize(orgNo);
        var deserialized = JsonSerializer.Deserialize<OrganizationNumber>(json);

        Assert.Equal("123456789", deserialized.ToString());
    }

    [Fact]
    public void JsonDeserialize_InvalidValue_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<OrganizationNumber>("\"abc\""));
    }

    // --- GetExamples ---
    [Fact]
    public void GetExamples_ReturnsNonEmptyCollection()
    {
        var examples = OrganizationNumber.GetExamples(default);
        Assert.NotNull(examples);
        Assert.NotEmpty(examples);
    }
}
