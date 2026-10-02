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
        var properties = jsonDoc.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("ServiceResource").GetProperty("properties");
        foreach (var name in new[] { "createdAt", "updatedAt" })
        {
            var timestamp = properties.GetProperty(name);
            Assert.True(timestamp.GetProperty("readOnly").GetBoolean());
            Assert.Equal("date-time", timestamp.GetProperty("format").GetString());
            Assert.Contains("Ignored on POST and PUT", timestamp.GetProperty("description").GetString());
        }
    }
}
