using System.Text.Json;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Unit.Contracts;

/// <summary>
/// Tests how <see cref="ResourceDto"/> writes <see cref="ResourceDto.AccessListMode"/>. The Resource Registry
/// returns the access list mode by name, so Access Management must not return it as a number (#5132).
/// </summary>
[UnitTest]
public class ResourceDtoSerializationTest
{
    [Theory]
    [InlineData(ResourceAccessListMode.Disabled, "Disabled")]
    [InlineData(ResourceAccessListMode.Enabled, "Enabled")]
    public void Serialize_WritesAccessListModeByName(ResourceAccessListMode mode, string expected)
    {
        string json = JsonSerializer.Serialize(new ResourceDto { AccessListMode = mode }, JsonSerializerOptions.Web);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement accessListMode = document.RootElement.GetProperty("accessListMode");
        Assert.Equal(JsonValueKind.String, accessListMode.ValueKind);
        Assert.Equal(expected, accessListMode.GetString());
    }

    /// <summary>
    /// A client, or a cached value, written before the value was serialized by name still reads.
    /// </summary>
    [Theory]
    [InlineData("\"Enabled\"", ResourceAccessListMode.Enabled)]
    [InlineData("1", ResourceAccessListMode.Enabled)]
    [InlineData("0", ResourceAccessListMode.Disabled)]
    public void Deserialize_ReadsAccessListModeByNameOrNumber(string value, ResourceAccessListMode expected)
    {
        ResourceDto resource = JsonSerializer.Deserialize<ResourceDto>($"{{\"accessListMode\":{value}}}", JsonSerializerOptions.Web);

        Assert.Equal(expected, resource.AccessListMode);
    }
}
