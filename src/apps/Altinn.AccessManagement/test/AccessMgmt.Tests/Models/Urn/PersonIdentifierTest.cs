using System.Text.Json;
using Altinn.Authorization.Api.Contracts.Register;

// See: overhaul part-2 step 20
namespace Altinn.AccessMgmt.Tests.Models.Urn;

/// <summary>
/// Pure-unit tests for <see cref="PersonIdentifier"/>. Covers the SSN
/// validation algorithm (modulo-11 over both control digits, with the
/// k1 control accepting any of 4 candidate values per the new
/// PersonIdentifier algorithm), length and content guards, JSON
/// round-tripping, and equality semantics.
/// </summary>
public class PersonIdentifierTest
{
    // ── Length and content guards ─────────────────────────────────────────────
    [Theory]
    [InlineData("")]
    [InlineData("0123456789")]    // 10 digits
    [InlineData("012345678901")]  // 12 digits
    public void TryParse_WrongLength_ReturnsFalse(string input)
    {
        Assert.False(PersonIdentifier.TryParse(input, null, out _));
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        Assert.False(PersonIdentifier.TryParse((string?)null, null, out _));
    }

    [Theory]
    [InlineData("0123456789a")]       // letter
    [InlineData("01234 678901")]      // space (note: 12 chars total when including space, but trimmed view here is wrong length)
    [InlineData("01-2345-6789")]      // dash + dash → 11 chars but non-numeric
    [InlineData("0123456789-")]       // 11 chars, last is non-digit
    public void TryParse_NonNumeric_ReturnsFalse(string input)
    {
        Assert.False(PersonIdentifier.TryParse(input, null, out _));
    }

    // ── Modulo-11 algorithm ──────────────────────────────────────────────────
    [Theory]
    [InlineData("02013299997")]
    [InlineData("30108299939")]
    [InlineData("42013299980")]
    public void TryParse_KnownValidExamples_ReturnsTrue(string input)
    {
        Assert.True(PersonIdentifier.TryParse(input, null, out var result));
        Assert.NotNull(result);
        Assert.Equal(input, result!.ToString());
    }

    [Fact]
    public void TryParse_FlippedFinalControlDigit_ReturnsFalse()
    {
        // 02013299997 is valid; flipping the last digit produces an invalid k2.
        Assert.False(PersonIdentifier.TryParse("02013299996", null, out _));
    }

    [Fact]
    public void Parse_InvalidValue_Throws()
    {
        // 02013299996 is the valid 02013299997 with the last (k2) digit
        // flipped — passes length / content guards, fails the modulo-11
        // algorithm. ("00000000000" actually validates as the algorithm
        // accepts all-zero digits.)
        Assert.Throws<FormatException>(() => PersonIdentifier.Parse("02013299996"));
    }

    // ── Equality ──────────────────────────────────────────────────────────────
    [Fact]
    public void Equality_TwoIdentifiersWithSameValue_AreEqual()
    {
        var a = PersonIdentifier.Parse("02013299997");
        var b = PersonIdentifier.Parse("02013299997");
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Equality_AgainstString_UsesUnderlyingValue()
    {
        var a = PersonIdentifier.Parse("02013299997");
        Assert.True(a == "02013299997");
        Assert.False(a == "30108299939");
        Assert.True(a.Equals("02013299997"));
    }

    [Fact]
    public void Equality_NullVsNull_AreEqual()
    {
        PersonIdentifier? x = null;
        PersonIdentifier? y = null;
        Assert.True(x == y);
    }

    [Fact]
    public void Equality_NullVsValue_AreNotEqual()
    {
        PersonIdentifier? x = null;
        var y = PersonIdentifier.Parse("02013299997");
        Assert.False(x == y);
        Assert.False(y == x);
    }

    // ── JSON round-trip ───────────────────────────────────────────────────────
    [Fact]
    public void Json_RoundTrip_PreservesValue()
    {
        var original = PersonIdentifier.Parse("02013299997");
        var json = JsonSerializer.Serialize(original);
        var decoded = JsonSerializer.Deserialize<PersonIdentifier>(json);

        Assert.NotNull(decoded);
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Json_DeserializeInvalid_Throws()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonIdentifier>("\"02013299996\""));
    }

    // ── TryFormat ─────────────────────────────────────────────────────────────
    [Fact]
    public void TryFormat_BufferTooSmall_ReturnsFalse()
    {
        var pid = PersonIdentifier.Parse("02013299997");
        Span<char> buffer = stackalloc char[5];
        Assert.False(pid.TryFormat(buffer, out var charsWritten, default, null));
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void TryFormat_BufferLargeEnough_WritesValue()
    {
        var pid = PersonIdentifier.Parse("02013299997");
        Span<char> buffer = stackalloc char[11];
        Assert.True(pid.TryFormat(buffer, out var charsWritten, default, null));
        Assert.Equal(11, charsWritten);
        Assert.Equal("02013299997", new string(buffer));
    }
}
