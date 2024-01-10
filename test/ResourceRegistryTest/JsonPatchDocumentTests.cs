#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class JsonPatchDocumentTests
{
    [Theory]
    [InlineData(/*lang=json,strict*/ """{"op":"add","path":"/a/b/c","value":"foo"}""")]
    [InlineData(/*lang=json,strict*/ """{"path":"/a/b/c","value":["foo"],"op":"add"}""")]
    [InlineData(/*lang=json,strict*/ """{"value":{},"path":"/a/b/c","op":"add"}""")]
    [InlineData(/*lang=json,strict*/ """{"op":"remove","path":"/a/2"}""")]
    [InlineData(/*lang=json,strict*/ """{"op":"replace","path":"/a/b/c","value":null}""")]
    [InlineData(/*lang=json,strict*/ """{"op":"move","from":"/a/b/c","path":"/a/b/d"}""")]
    [InlineData(/*lang=json,strict*/ """{"op":"copy","from":"/a/b/d","path":"/a/b/e"}""")]
    [InlineData(/*lang=json,strict*/ """{"op":"test","path":"/a/b/c","value":"foo"}""")]
    public void JsonPatchOperationRoundTrips(string json)
    {
        var origDoc = JsonDocument.Parse(json);
        var operation = origDoc.Deserialize<JsonPatchOperation>();
        var roundTrippedJson = JsonSerializer.Serialize(operation);
        var roundTrippedDoc = JsonDocument.Parse(roundTrippedJson);

        Assert.True(JsonEquivalenceComparer.Instance.Equals(origDoc, roundTrippedDoc));
    }

    [Theory]
    [InlineData(/*lang=json,strict*/ """true""", "Expected a JSON Patch operation object, but found a 'True' token.")]
    [InlineData(/*lang=json,strict*/ """{"long-key-name":true}""", "Expected a JSON patch operation object, but found key 'long-key-name'.")]
    [InlineData(/*lang=json,strict*/ """{"abc":true}""", "Expected a JSON patch operation object, but found key 'abc'.")]
    [InlineData(/*lang=json,strict*/ """{}""", "Expected a JSON patch operation object, but no 'op' set.")]
    [InlineData(/*lang=json,strict*/ """{"op":"remove"}""", "Expected a JSON patch operation object, but no 'path' set.")]
    [InlineData(/*lang=json,strict*/ """{"op":"remove","op":"remove"}""", "Expected a JSON patch operation object, but 'op' is set more than once.")]
    [InlineData(/*lang=json,strict*/ """{"path":"/remove","path":"/remove"}""", "Expected a JSON patch operation object, but 'path' is set more than once.")]
    [InlineData(/*lang=json,strict*/ """{"value":"remove","value":"remove"}""", "Expected a JSON patch operation object, but 'value' is set more than once.")]
    [InlineData(/*lang=json,strict*/ """{"from":"/remove","from":"/remove"}""", "Expected a JSON patch operation object, but 'from' is set more than once.")]
    [InlineData(/*lang=json,strict*/ """{"op":"add","path":"/foo"}""", "Expected a JSON patch operation object, but no 'value' set for 'add' operation.")]
    [InlineData(/*lang=json,strict*/ """{"op":"replace","path":"/foo"}""", "Expected a JSON patch operation object, but no 'value' set for 'replace' operation.")]
    [InlineData(/*lang=json,strict*/ """{"op":"move","path":"/foo"}""", "Expected a JSON patch operation object, but no 'from' set for 'move' operation.")]
    [InlineData(/*lang=json,strict*/ """{"op":"copy","path":"/foo"}""", "Expected a JSON patch operation object, but no 'from' set for 'copy' operation.")]
    [InlineData(/*lang=json,strict*/ """{"op":"test","path":"/foo"}""", "Expected a JSON patch operation object, but no 'value' set for 'test' operation.")]
    public void InvalidJsonPatchOperation(string json, string errorMessage)
    {
        var exn = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonPatchOperation>(json));
        Assert.Equal(errorMessage, exn.Message);
    }

    [Theory]
    [InlineData(/*lang=json,strict*/ """[]""", new JsonPatchOperationType[] { })]
    [InlineData(
        /*lang=json,strict*/ """
        [{"op":"add","path":"/a/b/c","value":"foo"}]
        """, 
        new[] { JsonPatchOperationType.Add })]
    [InlineData(
        /*lang=json,strict*/ """
        [{"op":"add","path":"/a/b/c","value":"foo"},{"op":"remove","path":"/a/2"}]
        """, 
        new[] { JsonPatchOperationType.Add, JsonPatchOperationType.Remove })]
    [InlineData(
        /*lang=json,strict*/ """
        [{"op":"add","path":"/a/b/c","value":"foo"},{"op":"remove","path":"/a/2"},{"op":"replace","path":"/a/b/c","value":null}]
        """, new[] { JsonPatchOperationType.Add, JsonPatchOperationType.Remove, JsonPatchOperationType.Replace })]
    public void Document(string json, JsonPatchOperationType[] expected)
    { 
        var doc = JsonSerializer.Deserialize<JsonPatchDocument>(json);
        Assert.NotNull(doc);
        Assert.Equal(expected.Length, doc.Operations.Length);

        var actual = doc.Operations.Select(op => op.Type);
        Assert.True(actual.SequenceEqual(expected));

        var roundTrippedJson = JsonSerializer.Serialize(doc);
        var roundTrippedDoc = JsonSerializer.Deserialize<JsonPatchDocument>(roundTrippedJson);
        Assert.NotNull(roundTrippedDoc);
        Assert.Equal(doc, roundTrippedDoc);
        Assert.Equal(doc.GetHashCode(), roundTrippedDoc.GetHashCode());
    }
}