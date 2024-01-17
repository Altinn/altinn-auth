using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Tests.Utils;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListControllerTests 
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public AccessListControllerTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthenticated_Returns_Unauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingScope_Returns_Forbidden()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WrongOwner_Returns_Forbidden()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CorrectScopeAndOwner_Returns_NotImplemented()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/974761076");
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task CorrectScopeAndAdmin_Returns_NotImplemented()
    {
        using var client = _factory.CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }
}