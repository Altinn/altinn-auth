#nullable enable

using Altinn.ResourceRegistry.JsonPatch;
using System.Text.Json;
using Xunit;

namespace Altinn.ResourceRegistry.Tests.JsonPatch;

public class JsonPatchOperationTypeTests
{
    [Theory]
    [InlineData(JsonPatchOperationType.Add, "\"add\"")]
    [InlineData(JsonPatchOperationType.Copy, "\"copy\"")]
    [InlineData(JsonPatchOperationType.Move, "\"move\"")]
    [InlineData(JsonPatchOperationType.Remove, "\"remove\"")]
    [InlineData(JsonPatchOperationType.Replace, "\"replace\"")]
    [InlineData(JsonPatchOperationType.Test, "\"test\"")]
    [InlineData(JsonPatchOperationType.Unknown, "null")]
    public void JsonRoundTrips(JsonPatchOperationType operationType, string expectedJson)
    {
        var json = JsonSerializer.Serialize(operationType);
        Assert.Equal(expectedJson, json);

        var parsedOperationType = JsonSerializer.Deserialize<JsonPatchOperationType>(json);
        Assert.Equal(operationType, parsedOperationType);
    }

    [Fact]
    public void JsonDeserializeInvalid()
    {
        var json = "\"foo\"";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonPatchOperationType>(json));

        var tooLongJson = "\"replaceeeee\"";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JsonPatchOperationType>(tooLongJson));
    }
}
