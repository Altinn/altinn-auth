#nullable enable

using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Models.ModelBinding;
using Altinn.ResourceRegistry.Tests.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ResourceRegistry.Tests.ModelBinding;

public class AccessListIncludesModelBinderTests
    : IClassFixture<AccessListIncludesModelBinderTests.Factory>
{
    private readonly Factory _factory;

    public AccessListIncludesModelBinderTests(Factory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("include=resources", AccessListIncludes.ResourceConnections)]
    [InlineData("include=resources,members", AccessListIncludes.ResourceConnections | AccessListIncludes.Members)]
    [InlineData("include=members", AccessListIncludes.Members)]
    [InlineData("include=members,resources", AccessListIncludes.Members | AccessListIncludes.ResourceConnections)]
    [InlineData("include=members&include=resources", AccessListIncludes.Members | AccessListIncludes.ResourceConnections)]
    [InlineData("include=members&include=resources,members", AccessListIncludes.Members | AccessListIncludes.ResourceConnections)]
    [InlineData("include=resource-actions", AccessListIncludes.ResourceConnectionsActions)]
    public async Task BindModelAsync(string query, AccessListIncludes expected)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/query?{query}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        result.Should().Be(((uint)expected).ToString());
    }

    public class TestController
    {
        [HttpGet("/query")]
        public uint Query([FromQuery(Name = "include")] AccessListIncludes accessListIncludes)
        {
            return (uint)accessListIncludes;
        }
    }

    public class Factory : TestControllerApplicationFactory<TestController>
    {
        protected override WebApplicationBuilder CreateWebApplicationBuilder()
        {
            var builder = base.CreateWebApplicationBuilder();

            builder.Services.Configure<MvcOptions>(options =>
            {
                options.ModelBinderProviders.InsertSingleton<AccessListIncludesModelBinder>(0);
            });

            return builder;
        }
    }
}
