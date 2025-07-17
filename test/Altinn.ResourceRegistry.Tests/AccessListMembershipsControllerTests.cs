using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Models;
using Altinn.ResourceRegistry.Tests.Utils;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Altinn.ResourceRegistry.Tests;

public class AccessListMembershipsControllerTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string ORG_CODE = "ttd";

    protected IAccessListsRepository Repository => Services.GetRequiredService<IAccessListsRepository>();
    protected AdvanceableTimeProvider TimeProvider => Services.GetRequiredService<AdvanceableTimeProvider>();

    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();

        var token = PrincipalUtil.GetAccessToken("pdp", "platform");

        client.DefaultRequestHeaders.Add("PlatformAccessToken", token);

        return client;
    }

    [Fact]
    public async Task InvalidPartyUrn_Returns_BadRequest()
    {
        using var client = CreateAuthenticatedClient();
       
        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/memberships?party=invalid");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(new AltinnValidationProblemDetails().ErrorCode);
        problemDetails.Errors.Should().HaveCount(2);
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.AccessListMemberships_Requires_Party.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/party"));
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.InvalidPartyUrn.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/party"));
    }

    [Fact]
    public async Task InvalidResourceUrn_Returns_BadRequest()
    {
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}&resource=invalid");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(new AltinnValidationProblemDetails().ErrorCode);
        problemDetails.Errors.Should().HaveCount(1);
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.InvalidResourceUrn.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/resource"));
    }

    [Fact]
    public async Task No_Party_Returns_BadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/resourceregistry/api/v1/access-lists/memberships");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(new AltinnValidationProblemDetails().ErrorCode);
        problemDetails.Errors.Should().HaveCount(1);
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.AccessListMemberships_Requires_Party.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/party"));
    }

    [Fact]
    public async Task Multiple_Parties_Returns_BadRequest()
    {
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1},{user2}");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(new AltinnValidationProblemDetails().ErrorCode);
        problemDetails.Errors.Should().HaveCount(1);
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.AccessListMemberships_TooManyParties.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/party"));
    }

    [Fact]
    public async Task Multiple_Resources_Returns_BadRequest()
    {
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked("resource1"));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked("resource2"));
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}&resource={resource1},{resource2}");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(new AltinnValidationProblemDetails().ErrorCode);
        problemDetails.Errors.Should().HaveCount(1);
        problemDetails.Errors.Should().ContainSingle(e => e.ErrorCode == ValidationErrors.AccessListMemberships_TooManyResources.ErrorCode)
            .Which.Paths.Should().HaveCount(1)
            .And.ContainSingle(v => string.Equals(v, "/$QUERY/resource"));
    }

    [Fact]
    public async Task NonExistingParty_Returns_BadRequest()
    {
        var nonExistingUser = PartyUrn.PartyUuid.Create(Guid.Empty);

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={nonExistingUser}");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<AltinnValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails.ErrorCode.Should().Be(Problems.PartyReference_NotFound.ErrorCode);
    }

    [Fact]
    public async Task By_Party_Returns_Multiple_Memberships_To_Same_Resource()
    {
        const string RESOURCE1 = "resource1";
        const string ACTION_READ = "read";

        await AddResource(RESOURCE1);
        var resource = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE, 
            identifier: "access-list1", 
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        var list2 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE,
            identifier: "access-list2",
            name: "Access List 2",
            description: "description2");

        list2.AddResourceConnection(RESOURCE1, [ACTION_READ]);
        list2.AddMembers([user1.Value]);
        await list2.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipWithActionFilterDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(2);
        memberships.Items.Should().AllSatisfy(m =>
        {
            m.Party.Should().Be(user1);
            m.Resource.Should().Be(resource);
        });
        memberships.Items.Should().ContainSingle(m => m.ActionFilters == null);
        memberships.Items.Should().ContainSingle(m => m.ActionFilters != null)
            .Which.ActionFilters.Should().BeEquivalentTo([ACTION_READ]);
    }

    [Fact]
    public async Task By_Party_Returns_Multiple_Resources()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(2);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource1);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource2);
    }

    [Fact]
    public async Task By_Party_And_Resource_Returns_Single_Resource()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party={user1}&resource={resource1}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        var memberships = await response.Content.ReadFromJsonAsync<ListObject<AccessListResourceMembershipDto>>();
        Assert.NotNull(memberships);

        memberships.Items.Should().HaveCount(1);
        memberships.Items.Should().Contain(m => m.Party == user1 && m.Resource == resource1);
    }

    [Fact]
    public async Task By_Member_MemberOfOne()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var user1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var user2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([user1.Value, user2.Value]);
        await list1.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/get-by-member?party={user1}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        IReadOnlyList<AccessListInfoDto>? memberships = await response.Content.ReadFromJsonAsync<IReadOnlyList<AccessListInfoDto>>();
        Assert.NotNull(memberships);
        Assert.Single(memberships);
        AccessListInfoDto? list1Resp = memberships.FirstOrDefault(r => r.Identifier.Equals("access-list1"));
        Assert.NotNull(list1Resp);
        Assert.Equal("access-list1", list1Resp.Identifier);
        Assert.Equal("description1", list1Resp.Description);
        Assert.Equal("urn:altinn:access-list:ttd:access-list1", list1Resp.Urn);
    }

    [Fact]
    public async Task By_Member_MemberOfTwo()
    {
        const string RESOURCE1 = "resource1";
        const string RESOURCE2 = "resource2";

        await AddResource(RESOURCE1);
        await AddResource(RESOURCE2);

        var resource1 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE1));
        var resource2 = ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(RESOURCE2));
        var party1 = PartyUrn.PartyUuid.Create(GenerateUserId());
        var party2 = PartyUrn.PartyUuid.Create(GenerateUserId());

        var list1 = await Repository.CreateAccessList(
            resourceOwner: ORG_CODE,
            identifier: "access-list1",
            name: "Access List 1",
            description: "description1");

        list1.AddResourceConnection(RESOURCE1, []);
        list1.AddResourceConnection(RESOURCE2, []);
        list1.AddMembers([party1.Value, party2.Value]);
        await list1.SaveChanges();

        var list2 = await Repository.CreateAccessList(
           resourceOwner: ORG_CODE,
           identifier: "access-list2",
           name: "Access List 2",
           description: "description2");

        list2.AddResourceConnection(RESOURCE1, []);
        list2.AddResourceConnection(RESOURCE2, []);
        list2.AddMembers([party1.Value, party2.Value]);
        await list2.SaveChanges();

        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/get-by-member?party={party1}");
        response.Should().HaveStatusCode(HttpStatusCode.OK);

        IReadOnlyList<AccessListInfoDto>? memberships = await response.Content.ReadFromJsonAsync<IReadOnlyList<AccessListInfoDto>>();
        Assert.NotNull(memberships);
        Assert.Equal(2,memberships.Count);
        AccessListInfoDto? list1Resp = memberships.FirstOrDefault(r => r.Identifier.Equals("access-list1"));
        AccessListInfoDto? list2Resp = memberships.FirstOrDefault(r => r.Identifier.Equals("access-list2"));
        Assert.NotNull(list1Resp);
        Assert.NotNull(list2Resp);
        Assert.Equal("access-list1", list1Resp.Identifier);
        Assert.Equal("description1", list1Resp.Description);
        Assert.Equal("urn:altinn:access-list:ttd:access-list1", list1Resp.Urn);
        Assert.Equal("access-list2", list2Resp.Identifier);
        Assert.Equal("description2", list2Resp.Description);
        Assert.Equal("urn:altinn:access-list:ttd:access-list2", list2Resp.Urn);
    }

    #region Authorization
    public class Authorization(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
        : AccessListMembershipsControllerTests(dbFixture, webApplicationFixture)
    {
        [Fact]
        public async Task Unauthenticated_Returns_Unauthorized()
        {
            using var client = CreateClient();

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.Should().HaveStatusCode(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task MissingScope_Returns_Forbidden()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", "some.scope");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.Should().HaveStatusCode(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CorrectScopeAndAdmin_Returns_Forbidden()
        {
            using var client = CreateClient();

            var token = PrincipalUtil.GetOrgToken("skd", "974761076", $"{AuthzConstants.SCOPE_RESOURCE_ADMIN}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/resourceregistry/api/v1/access-lists/memberships?party=urn:altinn:party:uuid:{GenerateUserId()}");
            response.Should().HaveStatusCode(HttpStatusCode.Forbidden);
        }
    }
    #endregion
}
