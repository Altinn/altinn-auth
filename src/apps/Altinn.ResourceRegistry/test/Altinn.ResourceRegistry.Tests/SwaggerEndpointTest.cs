#nullable enable

using Altinn.ResourceRegistry.TestUtils;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests;

public class SwaggerEndpointTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string SwaggerDocUri = "swagger/v1/swagger.json";

    [Fact]
    public async Task SwaggerDoc_OK()
    {
        using var jsonDoc = await GetSwaggerDoc();

        Assert.NotNull(jsonDoc);
    }

    /// <summary>
    /// Regression test for #4259. The endpoint declared a wrapper object while returning a flat list,
    /// so clients generated from the document failed on the response. Schema contents are otherwise
    /// owned by the endpoints and not pinned here; this one is pinned because it has broken a client once.
    /// </summary>
    [Fact]
    public async Task SwaggerDoc_V2PolicyRights_ResponseIsArrayOfRightDto()
    {
        const string Path = "/resourceregistry/api/v2/resource/{id}/policy/rights";

        using var jsonDoc = await GetSwaggerDoc();

        var schema = jsonDoc.RootElement
            .GetProperty("paths")
            .GetProperty(Path)
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal("#/components/schemas/RightDto", schema.GetProperty("items").GetProperty("$ref").GetString());
    }

    private async Task<JsonDocument> GetSwaggerDoc()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(SwaggerDocUri);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(responseText);
    }
}
