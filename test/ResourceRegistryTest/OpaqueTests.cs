#nullable enable

using Altinn.ResourceRegistry.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class OpaqueTests
{
    public static TheoryData<string> StringData => new()
    {
        "",
        "f",
        "fo",
        "foo",
        "ÿïø øû迸",
    };

    [Theory]
    [MemberData(nameof(StringData))]
    public void RoundTrips(string s)
    {
        var data = new Opaque<string>(s);
        var serialized = data.ToString();
        var parsed = Opaque<string>.Parse(serialized, null);

        parsed.Value.Should().Be(s);
    }

    [Theory]
    [MemberData(nameof(StringData))]
    public void RoundTripsJson(string s)
    {
        var data = new Opaque<string>(s);
        var json = JsonSerializer.Serialize(data);
        var parsed = JsonSerializer.Deserialize<Opaque<string>>(json);

        Assert.NotNull(parsed);
        parsed.Value.Should().Be(s);
    }
}
