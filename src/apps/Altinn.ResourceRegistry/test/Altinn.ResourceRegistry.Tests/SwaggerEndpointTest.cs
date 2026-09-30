#nullable enable

using Altinn.ResourceRegistry.TestUtils;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests;

public class SwaggerEndpointTest(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    [Fact]
    public async Task SwaggerDoc_OK()
    {
        const string RequestUri = "swagger/v1/swagger.json";

        using var client = CreateClient();

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUri);

        using var response = await client.SendAsync(httpRequestMessage);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseText);

        Assert.NotNull(jsonDoc);
    }
}
