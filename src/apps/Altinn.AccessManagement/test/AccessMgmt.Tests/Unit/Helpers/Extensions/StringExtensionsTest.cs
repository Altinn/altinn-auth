using Altinn.AccessManagement.Core.Helpers.Extensions;

namespace Altinn.AccessManagement.Tests.Unit.Helpers.Extensions;

/// <summary>
/// Pure-unit tests for <see cref="StringExtensions"/>. Covers the file-name
/// sanitization branches (throw vs replace on illegal characters, used when
/// building delegation policy paths), the loose name-similarity compare used in
/// profile lookup, and diacritic removal with the Norwegian-Å special case.
/// '/' is used as the illegal character because it is invalid in a file name on
/// both Windows and Linux.
/// </summary>
[UnitTest]
public class StringExtensionsTest
{
    // ── AsFileName ────────────────────────────────────────────────────────────
    [Fact]
    public void AsFileName_CleanInput_ReturnsUnchanged()
    {
        Assert.Equal("validname", "validname".AsFileName());
    }

    [Fact]
    public void AsFileName_IllegalChar_ThrowsByDefault()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "bad/name".AsFileName());
    }

    [Fact]
    public void AsFileName_IllegalChar_ReplacesWhenNotThrowing()
    {
        Assert.Equal("bad-name", "bad/name".AsFileName(throwExceptionOnInvalidCharacters: false));
    }

    [Fact]
    public void AsFileName_NullOrWhitespace_ReturnsInputUnchanged()
    {
        Assert.Null(((string)null).AsFileName());
        Assert.Equal("   ", "   ".AsFileName());
    }

    // ── IsSimilarTo ───────────────────────────────────────────────────────────
    [Fact]
    public void IsSimilarTo_SameFirstFourChars_ReturnsTrue()
    {
        // Only the first four characters are compared.
        Assert.True("Kristoffer".IsSimilarTo("Kristian"));
    }

    [Fact]
    public void IsSimilarTo_DifferentPrefix_ReturnsFalse()
    {
        Assert.False("Hansen".IsSimilarTo("Johnsen"));
    }

    [Fact]
    public void IsSimilarTo_DiffersInCaseAndDiacritics_ReturnsTrue()
    {
        Assert.True("Café".IsSimilarTo("cafe"));
    }

    [Fact]
    public void IsSimilarTo_BothNull_ReturnsTrue()
    {
        Assert.True(((string)null).IsSimilarTo(null));
    }

    // ── RemoveDiacritics ──────────────────────────────────────────────────────
    [Fact]
    public void RemoveDiacritics_StripsAccents()
    {
        Assert.Equal("cafe", "café".RemoveDiacritics());
        Assert.Equal("Muller", "Müller".RemoveDiacritics());
    }

    [Fact]
    public void RemoveDiacritics_PreservesNorwegianAring()
    {
        // Å/å are kept rather than folded to A, by design.
        Assert.Equal("Ås", "Ås".RemoveDiacritics());
    }
}
