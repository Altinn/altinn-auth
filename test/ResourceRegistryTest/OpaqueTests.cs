#nullable enable

using Altinn.ResourceRegistry.Models;
using FluentAssertions;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class OpaqueTests
{
    [Theory]
    [InlineData("")]
    [InlineData("f")]
    [InlineData("fo")]
    [InlineData("foo")]
    [InlineData("ÿïø øû迸")]
    public void RoundTrips(string s)
    {
        var data = new Opaque<string>(s);
        var serialized = data.ToString();
        var parsed = Opaque<string>.Parse(serialized, null);

        parsed.Value.Should().Be(s);
    }
}
