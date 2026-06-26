using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.ServiceOwner.Api.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for <see cref="Altinn.AccessManagement.Api.ServiceOwner.Controllers.AuthorizedPartiesController"/>
/// exercising the full HTTP pipeline against the containerized PostgreSQL test database.
/// </summary>
public class AuthorizedPartiesControllerTest
{
    public const string NewRoute = "accessmanagement/api/v1/serviceowner/authorizedparties";
    public const string OldRoute = "accessmanagement/api/v1/resourceowner/authorizedparties";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static HttpClient CreateServiceOwnerClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_AUTHORIZEDPARTIES_RESOURCEOWNER));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    /// <summary>
    /// Happy flow: a registered tjenesteier (NAV) asks for the authorized parties of
    /// Malin Emilie — the daglig leder (managing director) of Dumbo Adventures AS.
    /// The request travels through the real ASP.NET Core pipeline, the keyed
    /// <c>IAuthorizedPartiesService</c> implementation, and the PostgreSQL container,
    /// and is expected to return Dumbo Adventures with the DAGL role.
    /// </summary>
    [IntegrationTest]
    public class GetAuthorizedPartiesAsServiceOwnerHappyFlow : IClassFixture<ApiFixture>
    {
        public GetAuthorizedPartiesAsServiceOwnerHappyFlow(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(FeatureFlags.RightsDelegationApi);
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetAuthorizedParties_ForDagligLeder_UsingNewRoute_Returns200WithRepresentedOrgWithDaglRole()
        {
            // Arrange: NAV is a registered tjenesteier asking on behalf of Malin Emilie (DAGL of Dumbo Adventures)
            var client = CreateServiceOwnerClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var subject = new AuthorizedPartyRequestDto
            {
                Type = "urn:altinn:person:uuid",
                Value = TestData.MalinEmilie.Id.ToString(),
            };

            // Act
            var response = await client.PostAsJsonAsync(
                $"{NewRoute}?includeRoles=true",
                subject,
                TestContext.Current.CancellationToken);

            // Assert: HTTP 200 + DAGL relationship to Dumbo Adventures present in the materialized response
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parties = JsonSerializer.Deserialize<List<AuthorizedPartyDto>>(body, JsonOptions);
            Assert.NotNull(parties);

            var dumbo = parties.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
            Assert.NotNull(dumbo);
            Assert.Equal("Dumbo Adventures AS", dumbo.Name);
            Assert.Contains("daglig-leder", dumbo.AuthorizedRoles);
            Assert.Contains("dagl", dumbo.AuthorizedRoles);
        }

        [Fact]
        public async Task GetAuthorizedParties_ForDagligLeder_UsingOldRoute_Returns200WithRepresentedOrgWithDaglRole()
        {
            // Arrange: NAV is a registered tjenesteier asking on behalf of Malin Emilie (DAGL of Dumbo Adventures)
            var client = CreateServiceOwnerClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var subject = new AuthorizedPartyRequestDto
            {
                Type = "urn:altinn:person:uuid",
                Value = TestData.MalinEmilie.Id.ToString(),
            };

            // Act
            var response = await client.PostAsJsonAsync(
                $"{OldRoute}?includeRoles=true",
                subject,
                TestContext.Current.CancellationToken);

            // Assert: HTTP 200 + DAGL relationship to Dumbo Adventures present in the materialized response
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parties = JsonSerializer.Deserialize<List<AuthorizedPartyDto>>(body, JsonOptions);
            Assert.NotNull(parties);

            var dumbo = parties.FirstOrDefault(p => p.PartyUuid == TestData.DumboAdventures.Id);
            Assert.NotNull(dumbo);
            Assert.Equal("Dumbo Adventures AS", dumbo.Name);
            Assert.Contains("daglig-leder", dumbo.AuthorizedRoles);
            Assert.Contains("dagl", dumbo.AuthorizedRoles);
        }
    }
}
