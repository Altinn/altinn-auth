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

    /// <summary>
    /// A provider/resource filter (orgCode or anyOfResourceIds) narrows which parties are returned,
    /// but must not change how the returned parties are enriched. The include-flags gate enrichment of
    /// the response fields (authorizedRoles, authorizedAccessPackages, authorizedResources, authorizedInstances)
    /// and are honored independently of whether a resource filter is present.
    /// Josephine is a rightholder on Kaos Magic Design and Arts with resource and instance access to
    /// SiriusSkattemelding, so a filter on that resource returns Kaos.
    /// </summary>
    [IntegrationTest]
    public class GetAuthorizedPartiesWithResourceFilter : IClassFixture<ApiFixture>
    {
        public GetAuthorizedPartiesWithResourceFilter(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(FeatureFlags.RightsDelegationApi);
        }

        public ApiFixture Fixture { get; }

        private static AuthorizedPartyRequestDto JosephineSubject => new()
        {
            Type = "urn:altinn:person:uuid",
            Value = TestData.JosephineYvonnesdottir.Id.ToString(),
        };

        [Fact]
        public async Task GetAuthorizedParties_WithResourceFilterAndIncludeFlagsFalse_Returns200WithPartyButNoEnrichment()
        {
            // Arrange: NAV asks on behalf of Josephine, filtering on a resource she has access to on Kaos,
            // while opting out of every enrichment field.
            var client = CreateServiceOwnerClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var query = $"{OldRoute}?anyOfResourceIds={TestData.SiriusSkattemelding.RefId}" +
                        "&includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=false";

            // Act
            var response = await client.PostAsJsonAsync(query, JosephineSubject, TestContext.Current.CancellationToken);

            // Assert: the matching party is still returned, but no enrichment fields are populated.
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parties = JsonSerializer.Deserialize<List<AuthorizedPartyDto>>(body, JsonOptions);
            Assert.NotNull(parties);

            var kaos = parties.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
            Assert.NotNull(kaos);
            Assert.Empty(kaos.AuthorizedRoles);
            Assert.Empty(kaos.AuthorizedAccessPackages);
            Assert.Empty(kaos.AuthorizedResources);
            Assert.Empty(kaos.AuthorizedInstances);
        }

        [Fact]
        public async Task GetAuthorizedParties_WithResourceFilterAndIncludeFlagsTrue_Returns200WithPartyAndEnrichment()
        {
            // Arrange: same filter, but the caller asks for resource and instance enrichment.
            var client = CreateServiceOwnerClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var query = $"{OldRoute}?anyOfResourceIds={TestData.SiriusSkattemelding.RefId}" +
                        "&includeRoles=true&includeAccessPackages=true&includeResources=true&includeInstances=true";

            // Act
            var response = await client.PostAsJsonAsync(query, JosephineSubject, TestContext.Current.CancellationToken);

            // Assert: the party is returned with the requested enrichment populated.
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parties = JsonSerializer.Deserialize<List<AuthorizedPartyDto>>(body, JsonOptions);
            Assert.NotNull(parties);

            var kaos = parties.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
            Assert.NotNull(kaos);
            Assert.Contains(TestData.SiriusSkattemelding.RefId, kaos.AuthorizedResources);
            Assert.NotEmpty(kaos.AuthorizedInstances);
        }

        [Fact]
        public async Task GetAuthorizedParties_WithResourceFilterAndOnlyIncludeInstances_Returns200WithInstancesButNoResources()
        {
            // Arrange: the include-flags must be honored per field, not collapsed to all-or-nothing.
            var client = CreateServiceOwnerClient(Fixture, TestData.NAV.Entity.OrganizationIdentifier);

            var query = $"{OldRoute}?anyOfResourceIds={TestData.SiriusSkattemelding.RefId}" +
                        "&includeRoles=false&includeAccessPackages=false&includeResources=false&includeInstances=true";

            // Act
            var response = await client.PostAsJsonAsync(query, JosephineSubject, TestContext.Current.CancellationToken);

            // Assert: only instances populated; roles, access packages and resources stay empty.
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var parties = JsonSerializer.Deserialize<List<AuthorizedPartyDto>>(body, JsonOptions);
            Assert.NotNull(parties);

            var kaos = parties.FirstOrDefault(p => p.PartyUuid == TestData.KaosMagicDesignAndArts.Id);
            Assert.NotNull(kaos);
            Assert.Empty(kaos.AuthorizedRoles);
            Assert.Empty(kaos.AuthorizedAccessPackages);
            Assert.Empty(kaos.AuthorizedResources);
            Assert.NotEmpty(kaos.AuthorizedInstances);
        }
    }
}
