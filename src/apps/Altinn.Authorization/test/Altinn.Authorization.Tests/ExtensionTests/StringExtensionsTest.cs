using Altinn.Platform.Authorization.Helpers.Extensions;

namespace Altinn.Platform.Authorization.Tests.ExtensionTests;

public class StringExtensionsTest
{
    [Fact]
    public void AsFileName_AlphanumericInput_ReturnsSameString()
    {
        string result = "validName123".AsFileName();
        Assert.Equal("validName123", result);
    }

    [Fact]
    public void AsFileName_HyphenatedInput_ReturnsSameString()
    {
        string result = "valid-name".AsFileName();
        Assert.Equal("valid-name", result);
    }

    [Fact]
    public void AsFileName_NullInput_ReturnsNull()
    {
        string result = ((string)null).AsFileName();
        Assert.Null(result);
    }

    [Fact]
    public void AsFileName_EmptyInput_ReturnsEmpty()
    {
        string result = string.Empty.AsFileName();
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void AsFileName_WhitespaceInput_ReturnsWhitespace()
    {
        string result = "   ".AsFileName();
        Assert.Equal("   ", result);
    }

    [Fact]
    public void AsFileName_InvalidChars_ThrowExceptionTrue_Throws()
    {
        // Use the null character which is invalid in file names on all platforms
        // (Path.GetInvalidFileNameChars() returns different sets on Windows vs Linux,
        // e.g. '<' is only invalid on Windows).
        Assert.Throws<ArgumentOutOfRangeException>(() => "invalid\0name".AsFileName(throwExceptionOnInvalidCharacters: true));
    }

    [Fact]
    public void AsFileName_InvalidChars_ThrowExceptionFalse_ReplacesWithHyphen()
    {
        string result = "invalid\0name".AsFileName(throwExceptionOnInvalidCharacters: false);
        Assert.Equal("invalid-name", result);
    }

    [Fact]
    public void AsFileName_UnderscoreAndDot_ReturnsSameString()
    {
        // Underscores and dots are valid file name chars but not alphanumeric/hyphen,
        // so the method falls through to the illegal-chars check which passes.
        string result = "file_name.xml".AsFileName();
        Assert.Equal("file_name.xml", result);
    }
}
