using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partialt class for various connections controller tests. This part focuses on testing the GetResources endpoint, which retrieves resources associated with a connection between two parties. The tests cover different actor perspectives (Dumbo vs Mille), delegation directions (from-others vs to-others), and scope-based authorization scenarios to ensure that the endpoint behaves correctly under various conditions.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetResources(Guid, Guid?, Guid?, AccessManagement.Api.Enduser.Models.PagingInput, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - ResourceType "Test"
    /// - Resource "Skattemelding" (app_skd_skattemelding) and "MVA-melding" (app_skd_mva-melding)
    /// - Assignment: Dumbo Adventures → Mille Hundefrisør (Rightholder)
    /// - AssignmentResource linking both resources to the assignment above
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (views from Dumbo's perspective)
    /// - Thea: managing director of Mille Hundefrisør (views from Mille's perspective)
    /// </para>
    /// <para>
    /// The tests verify scope-based authorization (from-others vs to-others read scopes),
    /// that the correct resources are returned in the response, and that mismatched scopes
    /// result in HTTP 403 Forbidden.
    /// </para>
    /// </remarks>
    public class GetResources : IClassFixture<ApiFixture>
    {
        public GetResources(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.ResourceDelegationEF);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromDumboToMille = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.MilleHundefrisor.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromDumboToMille);
                db.SaveChanges();

                db.AssignmentResources.Add(new AssignmentResource()
                {
                    AssignmentId = rightholderFromDumboToMille.Id,
                    ResourceId = TestData.SkattResource.Id,
                });

                db.AssignmentResources.Add(new AssignmentResource()
                {
                    AssignmentId = rightholderFromDumboToMille.Id,
                    ResourceId = TestData.MvaResource.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(Guid partyUuid, params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries resources delegated to Mille in the to-others direction.
        /// Expects OK with both Skattemelding and MVA-melding resources.
        /// </summary>
        [Fact]
        public async Task GetResources_AsMalinForDumboToMille_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            List<ResourcePermissionDto> result = JsonSerializer.Deserialize<List<ResourcePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.True(result.Count >= 2, $"Expected at least 2 resources but got {result.Count}. Response body: {responseContent}");
            Assert.Contains(result, r => r.Resource.RefId == "app_skd_skattemelding");
            Assert.Contains(result, r => r.Resource.RefId == "app_skd_mva-melding");
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries resources from Mille in the from-others direction.
        /// Expects OK (Mille has no resources delegated toward Dumbo, so the list may be empty).
        /// </summary>
        [Fact]
        public async Task GetResources_AsMalinForDumboFromMille_WithFromOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.DumboAdventures.Id}&to={TestData.DumboAdventures.Id}&from={TestData.MilleHundefrisor.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Thea (MD of Mille) queries resources that Dumbo delegated to Mille, viewed from Mille's perspective (from-others).
        /// Expects OK with both Skattemelding and MVA-melding resources.
        /// </summary>
        [Fact]
        public async Task GetResources_AsTheaForMilleFromDumbo_WithFromOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.MilleHundefrisor.Id}&to={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            List<ResourcePermissionDto> result = JsonSerializer.Deserialize<List<ResourcePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.True(result.Count >= 2, $"Expected at least 2 resources but got {result.Count}. Response body: {responseContent}");
            Assert.Contains(result, r => r.Resource.RefId == "app_skd_skattemelding");
            Assert.Contains(result, r => r.Resource.RefId == "app_skd_mva-melding");
        }

        /// <summary>
        /// Thea (MD of Mille) queries resources in the to-others direction (what Mille gives to Dumbo).
        /// Expects OK (no resources delegated in this direction).
        /// </summary>
        [Fact]
        public async Task GetResources_AsTheaForMilleToDumbo_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.MilleHundefrisor.Id}&from={TestData.MilleHundefrisor.Id}&to={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Thea uses the wrong scope (to-others read) when querying from-others direction for Mille.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResources_AsTheaForMilleFromDumbo_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.MilleHundefrisor.Id}&to={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope for a to-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResources_ToOthersDirection_WithFromOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope for a from-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResources_FromOthersDirection_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.DumboAdventures.Id}&to={TestData.DumboAdventures.Id}&from={TestData.MilleHundefrisor.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses a write scope on the read-only GetResources endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResources_WithWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
