using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
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
/// Partial test class for ConnectionsController, focused on testing the RemoveResource (DELETE resources) endpoint
/// which removes all resource rights delegations for a given resource from one party to another. The tests cover
/// successful removal by Malin (MD of Dumbo Adventures) and Thea (MD of Mille Hundefrisør), scope enforcement,
/// and error scenarios.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveResource(Guid, Guid, Guid, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Dumbo Adventures → Mille Hundefrisør (Rightholder)
    /// </para>
    /// <para>
    /// Pre-seeded via <see cref="TestDataSeeds"/>:
    /// - Resource "Sykmelding til arbeidsgiver" (nav_sykepenger_sykmelding)
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// - <see cref="Altinn2RightsClientMock"/> to prevent real HTTP calls to Altinn 2 SBL Bridge
    /// - <see cref="PolicyFactoryMock"/> captures written XACML policies
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (can act as to-others delegator)
    /// - Thea: managing director of Mille Hundefrisør (can act as from-others receiver)
    /// </para>
    /// <para>
    /// The tests verify that Malin can remove resource rights on behalf of Dumbo Adventures for existing rightholders,
    /// that Thea can remove resource rights on behalf of Mille Hundefrisør from an existing connection,
    /// that correct scopes are enforced, and that invalid resources produce appropriate errors.
    /// </para>
    /// </remarks>
    public class RemoveResource : IClassFixture<ApiFixture>
    {
        public RemoveResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.ResourceDelegationEF);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueueMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromDumboToMille = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.MilleHundefrisor.Id,
                    RoleId = RoleConstants.Rightholder
                };

                db.Assignments.Add(rightholderFromDumboToMille);
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
        /// Helper to get valid right keys via the delegation check endpoint.
        /// Malin (MD of Dumbo) performs a delegation check for the resource to discover delegatable right keys.
        /// </summary>
        private async Task<List<string>> GetDelegatableRightKeys(string resource)
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={resource}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result.Rights
                .Where(r => r.Result)
                .Select(r => r.Right.Key)
                .ToList();
        }

        /// <summary>
        /// Helper to add resource rights delegation before testing removal.
        /// </summary>
        private async Task AddResourceRights(string resource, List<string> rightKeys)
        {
            var body = new RightKeyListDto { DirectRightKeys = rightKeys };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource={resource}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        /// <summary>
        /// Malin (MD of Dumbo Adventures) removes all resource rights for nav_sykepenger_sykmelding delegated to Mille Hundefrisør.
        /// First adds the delegation, then removes it. Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemoveResource_AsMalinForDumboFromDumboToMille_ReturnsNoContent()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("nav_sykepenger_sykmelding");
            Assert.NotEmpty(rightKeys);

            await AddResourceRights("nav_sykepenger_sykmelding", rightKeys);

            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Thea (MD of Mille Hundefrisør) removes all resource rights for nav_sykepenger_sykmelding from the Dumbo → Mille connection,
        /// acting as receiver (from-others direction). Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemoveResource_AsTheaForMilleFromDumboToMille_ReturnsNoContent()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("nav_sykepenger_sykmelding");
            Assert.NotEmpty(rightKeys);

            await AddResourceRights("nav_sykepenger_sykmelding", rightKeys);

            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Malin (MD of Dumbo) tries to remove resource rights for a resource that does not exist in the database.
        /// Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithInvalidResource_ReturnsBadRequest()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nonexistent_resource",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithFromOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveResource_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nav_sykepenger_sykmelding",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
