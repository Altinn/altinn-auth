#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using System.Text.Json;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.JsonPatch;

public class JsonPointerTest
{
    [Fact]
    public void Parse()
    {
        Assert.True(JsonPointer.TryParse("/foo/bar", out var pointer));
        Assert.Equal(2, pointer.Count);
        Assert.Equal("foo", pointer[0].ToString());
        Assert.Equal("bar", pointer[1].ToString());
    }

    [Fact]
    public void ParseEmpty()
    {
        Assert.True(JsonPointer.TryParse(string.Empty, out var pointer));
        Assert.Empty(pointer);
    }

    [Fact]
    public void ParseInvalid()
    {
        Assert.False(JsonPointer.TryParse("foo", out _));
    }

    [Fact]
    public void ParseInvalidEscape()
    {
        Assert.False(JsonPointer.TryParse("/foo~", out _));
    }

    [Fact]
    public void ParseEscaped()
    {
        Assert.True(JsonPointer.TryParse("/foo~1baz/bar~0~0/~1~0abc~0~0def~1~1", out var pointer));
        Assert.NotEmpty(pointer);
        Assert.Equal("foo/baz", pointer[0].ToString());
        Assert.Equal("bar~~", pointer[1].ToString());
        Assert.Equal("/~abc~~def//", pointer[2].ToString());
    }

    [Fact]
    public void ParseEmptyProps()
    {
        Assert.True(JsonPointer.TryParse("/", out var pointer));
        Assert.Single(pointer);
        Assert.True(pointer[0].IsEmpty);

        Assert.True(JsonPointer.TryParse("//", out pointer));
        Assert.Equal(2, pointer.Count);
        Assert.True(pointer[0].IsEmpty);
        Assert.True(pointer[1].IsEmpty);
    }

    [Fact]
    public void JsonDeserialize()
    {
        var json = "null";
        var pointer = JsonSerializer.Deserialize<JsonPointer>(json);
        Assert.Null(pointer);

        json = "\"/foo/bar\"";
        pointer = JsonSerializer.Deserialize<JsonPointer>(json);
        Assert.NotNull(pointer);
        Assert.Equal(2, pointer.Count);

        json = "[\"\", \"invalid~\"]";
        var exn = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonPointer[]>(json));
        Assert.Equal("$[1]", exn.Path);
    }

    [Fact]
    public void JsonSerialize()
    {
        JsonPointer? pointer = null;
        var json = JsonSerializer.Serialize(pointer);
        Assert.Equal("null", json);

        pointer = JsonPointer.Empty;
        json = JsonSerializer.Serialize(pointer);
        Assert.Equal("\"\"", json);

        pointer = JsonPointer.Parse("/");
        json = JsonSerializer.Serialize(pointer);
        Assert.Equal("\"/\"", json);

        pointer = JsonPointer.Parse("/foo/bar");
        json = JsonSerializer.Serialize(pointer);
        Assert.Equal("\"/foo/bar\"", json);

        pointer = JsonPointer.Parse("/foo~1baz/bar~0~0/~1~0abc~0~0def~1~1");
        json = JsonSerializer.Serialize(pointer);
        Assert.Equal("\"/foo~1baz/bar~0~0/~1~0abc~0~0def~1~1\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    [InlineData("/foo/bar/")]
    [InlineData("/foo/bar/0")]
    [InlineData("/foo/bar/-")]
    [InlineData("/foo/bar/-/")]
    [InlineData("/foo/bar/-/baz")]
    [InlineData("/foo~1baz/bar~0~0/~1~0abc~0~0def~1~1")]
    public void Equality(string pointerString)
    {
        var pointer1 = JsonPointer.Parse(pointerString);
        var pointer2 = JsonPointer.Parse(pointerString);
        var notEqual = JsonPointer.Parse("/" + pointerString);

        Assert.Equal(pointer1, pointer2);
        Assert.True(pointer1 == pointer2);
        Assert.Equal(pointer1.GetHashCode(), pointer2.GetHashCode());
        Assert.NotEqual(pointer1, notEqual);
        Assert.True(pointer1 != notEqual);
    }
}
