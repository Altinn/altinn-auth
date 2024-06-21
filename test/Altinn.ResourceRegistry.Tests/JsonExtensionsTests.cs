#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests;

public class JsonExtensionsTests
{
    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", "null")]
    [InlineData("42", "42")]
    [InlineData("\"foo\"", "\"foo\"")]
    [InlineData("[]", "[]")]
    [InlineData("[1, 2]", "[1, 2]")]
    [InlineData("[1, true]", "[1, true]")]
    [InlineData("{}", "{}")]
    [InlineData("""{"foo": 42}""", """{"foo": 42}""")]
    [InlineData("""{"foo": true}""", """{"foo": true}""")]
    [InlineData("""{"foo": null, "bar": ["bar", {"a":1,"b":2}]}""", """{"bar": ["bar", {"b":2,"a":1}], "foo": null}""")]
    public void EqualityAndHashCode(string json1, string json2)
    {
        var doc1 = JsonDocument.Parse(json1);
        var doc2 = JsonDocument.Parse(json2);

        Assert.Equal(doc1.GetStableHashCode(), doc2.GetStableHashCode());
        Assert.True(doc1.IsEquivalentTo(doc2));

        var root1 = doc1.RootElement;
        var root2 = doc2.RootElement;

        Assert.Equal(root1.GetStableHashCode(), root2.GetStableHashCode());
        Assert.True(root1.IsEquivalentTo(root2));
    }

    [Theory]
    [InlineData("true", "false")]
    [InlineData("false", "null")]
    [InlineData("null", "32")]
    [InlineData("42", "\"42\"")]
    [InlineData("\"foo\"", "\"bar\"")]
    [InlineData("[]", "[1]")]
    [InlineData("[1, 2]", "[1]")]
    [InlineData("[1, true]", "[1, false]")]
    [InlineData("{}", """{"a":2}""")]
    [InlineData("""{"foo": 42}""", """{"foo": 41}""")]
    [InlineData("""{"foo": true}""", """{"foo": false}""")]
    [InlineData("""{"foo": null, "bar": ["bar", {"a":1,"b":2}]}""", """{"bar": ["bar", {"b":2}], "foo": null}""")]
    public void NotEqual(string json1, string json2)
    {
        var doc1 = JsonDocument.Parse(json1);
        var doc2 = JsonDocument.Parse(json2);

        Assert.False(doc1.IsEquivalentTo(doc2));

        var root1 = doc1.RootElement;
        var root2 = doc2.RootElement;

        Assert.False(root1.IsEquivalentTo(root2));
    }
}