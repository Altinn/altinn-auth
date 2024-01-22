using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListControllerTests 
    : WebApplicationTests
{
    private const string ORG_NR = "974761076";

    public AccessListControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture) 
        : base(dbFixture, webApplicationFixture)
    {
    }

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();


    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    #region GetAccessListsByOwner
    [Fact]
    public async Task GetAccessListsByOwner_Returns_EmptyList()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
        Assert.NotNull(content);

        content.Items.Should().BeEmpty();
        content.Links.Next.Should().BeNull();
    }

    [Fact]
    public async Task GetAccessListsByOwner_Returns_ItemsInDatabase_OrderedByIdentifier()
    {
        var def1 = await Repository.CreateAccessList(ORG_NR, "test1", "Test 1", "test 1 description");
        var def2 = await Repository.CreateAccessList(ORG_NR, "test2", "Test 2", "test 2 description");

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
        Assert.NotNull(content);

        content.Items.Should().HaveCount(2);
        content.Items.Should().Contain(al => al.Identifier == "test1")
            .Which.Name.Should().Be("Test 1");
        content.Items.Should().Contain(al => al.Identifier == "test2")
            .Which.Name.Should().Be("Test 2");
        content.Links.Next.Should().BeNull();

        var identifiers = content.Items.Select(al => al.Identifier).ToList();
        identifiers.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAccessListsByOwner_Returns_Paginated()
    {
        // create enough access lists to fill two pages and then some
        for (var i = 41; i > 0; i--)
        {
            await Repository.CreateAccessList(ORG_NR, $"test{i:00}", $"Test {i:00}", $"test {i:00} description");
        }

        using var client = CreateAuthenticatedClient();

        // page 1
        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
        Assert.NotNull(content);

        content.Items.Should().HaveCount(20);
        content.Links.Next.Should().NotBeNull();

        var identifiers = content.Items.Select(al => al.Identifier).ToList();
        identifiers.Should().BeInAscendingOrder()
            .And.StartWith("test01")
            .And.EndWith("test20");

        response = await client.GetAsync(content.Links.Next);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // page 2
        content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
        Assert.NotNull(content);

        content.Items.Should().HaveCount(20);
        content.Links.Next.Should().NotBeNull();

        identifiers = content.Items.Select(al => al.Identifier).ToList();
        identifiers.Should().BeInAscendingOrder()
            .And.StartWith("test21")
            .And.EndWith("test40");

        // page 3
        response = await client.GetAsync(content.Links.Next);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        content = await response.Content.ReadFromJsonAsync<Paginated<AccessListInfoDto>>();
        Assert.NotNull(content);

        content.Items.Should().HaveCount(1);
        content.Links.Next.Should().BeNull();

        identifiers = content.Items.Select(al => al.Identifier).ToList();
        identifiers.Should().BeInAscendingOrder()
            .And.StartWith("test41");
    }
    #endregion

    #region Authorization
    [Fact]
    public async Task Unauthenticated_Returns_Unauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingScope_Returns_Forbidden()
    {
        using var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WrongOwner_Returns_Forbidden()
    {
        using var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CorrectScopeAndOwner_Returns_NotImplemented()
    {
        using var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", AuthzConstants.SCOPE_ACCESS_LIST_READ);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/{ORG_NR}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CorrectScopeAndAdmin_Returns_NotImplemented()
    {
        using var client = CreateClient();

        var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/1234");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    #endregion
}
