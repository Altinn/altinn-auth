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
/// Partial test class for ConnectionsController, focused on testing the AddResourceRights (POST resources/rights) endpoint
/// which delegates resource rights from one party to another. The tests cover successful delegation by Malin (MD of Dumbo)
/// to existing rightholders, scope enforcement, and error scenarios.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.AddResourceRights(Guid, Guid, string, RightKeyListDto, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - ResourceType "Test"
    /// - Resource "Skattemelding" (app_skd_sirius-skattemelding-v1)
    /// - Assignment: Dumbo Adventures → Mille Hundefrisør (Rightholder)
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
    /// - Thea: managing director of Mille Hundefrisør and rightholder of Dumbo Adventures
    /// </para>
    /// <para>
    /// The tests verify that Malin can delegate resource rights on behalf of Dumbo to existing rightholders,
    /// that correct scopes are enforced, and that missing connections or invalid resources produce appropriate errors.
    /// </para>
    /// </remarks>
    public class AddResourceRights : IClassFixture<ApiFixture>
    {
        public AddResourceRights(ApiFixture fixture)
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
        /// Malin (MD of Dumbo) delegates resource rights for Skattemelding to Mille Hundefrisør (existing rightholder).
        /// Uses valid right keys obtained from delegation check. Expects 201 Created.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_AsMalinForDumboToMille_WithValidRightKeys_ReturnsCreated()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("app_skd_sirius-skattemelding-v1");
            Assert.NotEmpty(rightKeys);

            // Use all delegatable right keys
            var body = new RightKeyListDto { DirectRightKeys = rightKeys };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                body,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected Created but got {response.StatusCode}. Response body: {responseContent}");

            // Parse the written XACML policy
            var policyFactory = Fixture.Server.Services.GetRequiredService<IPolicyFactory>() as PolicyFactoryMock;
            Assert.NotNull(policyFactory);
            Assert.NotEmpty(policyFactory.WrittenPolicies);

            var (_, content) = policyFactory.WrittenPolicies.Single();
            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(new MemoryStream(content)))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

            // Assert rule count
            Assert.Equal(rightKeys.Count, policy.Rules.Count);

            // Verify subject in each rule
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
                   Assert.Equal(TestData.MilleHundefrisor.Entity.PartyId.ToString(),match.AttributeValue.Value);
                }
            }

            // Verify resource in each rule
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
                Assert.Equal("skd", orgMatch.AttributeValue.Value);

                var appMatch = resourceMatches.FirstOrDefault(m => m.AttributeDesignator.AttributeId.OriginalString == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute);
                Assert.NotNull(appMatch);
                Assert.Equal("sirius-skattemelding-v1", appMatch.AttributeValue.Value);
            }
        }

        /// <summary>
        /// Malin (MD of Dumbo) delegates resource rights for a resource that does not exist in the database.
        /// Expects 400 BadRequest with an invalid resource error.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_WithInvalidResource_ReturnsBadRequest()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-fake-right-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=nonexistent_resource",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin (MD of Dumbo) tries to delegate resource rights to Josephine who has no rightholder connection.
        /// The service requires an existing connection. Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_ToPartyWithNoConnection_ReturnsBadRequest()
        {
            List<string> rightKeys = await GetDelegatableRightKeys("app_skd_sirius-skattemelding-v1");
            Assert.NotEmpty(rightKeys);

            var body = new RightKeyListDto { DirectRightKeys = [rightKeys.First()] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_WithReadScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_WithFromOthersWriteScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddResourceRights_WithToOthersReadScope_ReturnsForbidden()
        {
            var body = new RightKeyListDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
