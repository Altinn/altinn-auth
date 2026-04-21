using Altinn.Authorization.Helpers;

namespace Altinn.Platform.Authorization.Tests;

public class ServiceResourceHelperTest
{
    [Theory]
    [InlineData("abcd", true)]
    [InlineData("a1b2", true)]
    [InlineData("test-resource", true)]
    [InlineData("test_resource", true)]
    [InlineData("a1-_", true)]
    [InlineData("abc", false)]       // too short (< 4)
    [InlineData("", false)]          // empty
    [InlineData("ABCD", false)]      // uppercase not allowed
    [InlineData("ab cd", false)]     // space not allowed
    [InlineData("ab.cd", false)]     // dot not allowed
    [InlineData("abcd!", false)]     // special char not allowed
    public void ResourceIdentifierRegex_ValidatesCorrectly(string input, bool expected)
    {
        bool result = ServiceResourceHelper.ResourceIdentifierRegex().IsMatch(input);
        Assert.Equal(expected, result);
    }
}
