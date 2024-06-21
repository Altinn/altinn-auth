#nullable enable

using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Tests.Utils;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests;

public class SwaggerEndpointTest 
    : IClassFixture<CustomWebApplicationFactory<ResourceOwnerController>> 
{
    private readonly CustomWebApplicationFactory<ResourceOwnerController> _factory;

    public SwaggerEndpointTest(CustomWebApplicationFactory<ResourceOwnerController> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Orglist_OK()
    {
        const string RequestUri = "swagger/v1/swagger.json";

        using var client = SetupUtil.GetTestClient(_factory);

        using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, RequestUri);

        using var response = await client.SendAsync(httpRequestMessage);
        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseText);

        Assert.NotNull(jsonDoc);
    }
}