using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Xml;
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
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the UpdateResourceRights (PUT resources/rights) endpoint
/// which updates resource rights delegations from one party to another. The tests cover successful updates by Malin (MD of Dumbo Adventures)
/// to existing rightholders, scope enforcement, and error scenarios.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.UpdateResourceRights(Guid, Guid, string, RightKeyListDto, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - ResourceType "Test"
    /// - Resource "Mattilsynet Bakery Service" (app_mat_mattilsynet-baker-konditorvare)
    /// - Assignment: Dumbo Adventures → Mille Hundefrisør (Rightholder)
    /// - Existing delegation: Dumbo Adventures has delegated some rights to Mille for the bakery service
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// - <see cref="Altinn2RightsClientMock"/> to prevent real HTTP calls to Altinn 2 SBL Bridge
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (has DAGL role, can delegate)
    /// - Mille Hundefrisør: organization with rightholder connection to Dumbo Adventures
    /// </para>
    /// <para>
    /// The tests verify that Malin can update resource rights on behalf of Dumbo Adventures to existing rightholders,
    /// that correct scopes are enforced, and that missing delegations or invalid resources produce appropriate errors.
    /// </para>
    /// </remarks>
    public class UpdateResourceRights : IClassFixture<ApiFixture>
    {
        public UpdateResourceRights(ApiFixture fixture)
        {
            Fixture = fixture;
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
        /// Malin (MD of Dumbo Adventures) performs a delegation check for the resource to discover delegatable right keys.
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
        /// Helper to add initial resource rights delegation before testing updates.
        /// </summary>
        private async Task AddInitialResourceRights(string resource, List<string> rightKeys)
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
        /// Malin (MD of Dumbo Adventures) updates resource rights for Mattilsynet Bakery Service delegated to Mille Hundefrisør (existing rightholder).
        /// Uses valid right keys obtained from delegation check. Expects 200 OK.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_AsMalinForDumboToMille_WithValidRightKeys_ReturnsOk()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("app_mat_mattilsynet-baker-konditorvare");
            Assert.NotEmpty(rightKeys);

            // First add initial delegation
            await AddInitialResourceRights("app_mat_mattilsynet-baker-konditorvare", [rightKeys.First()]);

            // Now update with more rights
            var updateBody = new RightKeyListDto { DirectRightKeys = rightKeys.Take(2).ToList() };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                updateBody,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            // Parse the written XACML policy
            var policyFactory = Fixture.Server.Services.GetRequiredService<IPolicyFactory>() as PolicyFactoryMock;
            Assert.NotNull(policyFactory);
            Assert.NotEmpty(policyFactory.WrittenPolicies);

            var (path, content) = policyFactory.WrittenPolicies.Last();
            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(new MemoryStream(content)))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

            // Assert rule count - should have rules for the updated rights
            Assert.NotEmpty(policy.Rules);

            // Verify subject in rules - should be Mille Hundefrisør
            foreach (XacmlRule rule in policy.Rules)
            {
                Assert.NotNull(rule.Target);
                var subjectMatches = rule.Target.AnyOf
                    .SelectMany(anyOf => anyOf.AllOf)
                    .SelectMany(allOf => allOf.Matches)
                    .Where(match => match.AttributeDesignator.Category.OriginalString == XacmlConstants.MatchAttributeCategory.Subject)
                    .ToList();
                Assert.NotEmpty(subjectMatches);

                foreach (XacmlMatch match in subjectMatches)
                {
                    Assert.Equal(TestData.MilleHundefrisor.Entity.PartyId.ToString(), match.AttributeValue.Value);
                }
            }

            // Verify resource in rules
            foreach (XacmlRule rule in policy.Rules)
            {
                var resourceMatches = rule.Target.AnyOf
                    .SelectMany(anyOf => anyOf.AllOf)
                    .SelectMany(allOf => allOf.Matches)
                    .Where(match => match.AttributeDesignator.Category.OriginalString == XacmlConstants.MatchAttributeCategory.Resource)
                    .ToList();
                Assert.NotEmpty(resourceMatches);

                var orgMatch = resourceMatches.FirstOrDefault(m => m.AttributeDesignator.AttributeId.OriginalString == AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute);
                Assert.NotNull(orgMatch);
                Assert.Equal("mat", orgMatch.AttributeValue.Value);

                var appMatch = resourceMatches.FirstOrDefault(m => m.AttributeDesignator.AttributeId.OriginalString == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute);
                Assert.NotNull(appMatch);
                Assert.Equal("mattilsynet-baker-konditorvare", appMatch.AttributeValue.Value);
            }
        }

        /// <summary>
        /// Malin (MD of Dumbo Adventures) updates resource rights with an empty rights list, effectively removing all delegations.
        /// Expects 200 OK.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_WithEmptyRightKeys_ReturnsOk()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("app_mat_mattilsynet-baker-konditorvare");
            Assert.NotEmpty(rightKeys);

            // First add initial delegation
            await AddInitialResourceRights("app_mat_mattilsynet-baker-konditorvare", [rightKeys.First()]);

            // Update with empty list (remove all rights)
            var updateBody = new RightKeyListDto { DirectRightKeys = [] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                updateBody,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Malin (MD of Dumbo Adventures) tries to update resource rights for a resource that does not exist in the database.
        /// Expects 400 BadRequest with an invalid resource error.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_WithInvalidResource_ReturnsBadRequest()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-fake-right-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nonexistent_resource",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin (MD of Dumbo Adventures) tries to update resource rights for Josephine who has no rightholder connection.
        /// The service requires an existing connection. Expects 400 BadRequest.
        /// </summary>
        // [Fact]  // Enable when https://github.com/Altinn/altinn-authorization-tmp/issues/2716 is fixed
        public async Task UpdateResourceRights_ToPartyWithNoConnection_ReturnsBadRequest()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("app_mat_mattilsynet-baker-konditorvare");
            Assert.NotEmpty(rightKeys);

            var body = new RightKeyListDto { DirectRightKeys = [rightKeys.First()] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_WithReadScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_WithFromOthersWriteScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task UpdateResourceRights_WithToOthersReadScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.PutAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_mat_mattilsynet-baker-konditorvare",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
