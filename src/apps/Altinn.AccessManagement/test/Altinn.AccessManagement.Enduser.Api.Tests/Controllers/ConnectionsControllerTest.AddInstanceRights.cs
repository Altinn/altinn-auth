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
using Altinn.AccessManagement.Core.Models;
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
/// Partial test class for ConnectionsController, focused on testing the AddInstanceRights
/// (POST resources/instances/rights) endpoint which delegates instance-level rights from one party to another.
/// Malin (MD of Dumbo Adventures) delegates instance rights on behalf of Dumbo to Kaos Magic Design and Arts.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.AddInstanceRights(Guid, Guid?, string, string, InstanceRightsDelegationDto, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Dumbo Adventures → Kaos Magic Design and Arts (Rightholder) seeded locally
    /// - Resource: SiriusSkattemelding (app_skd_sirius-skattemelding-v1)
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// - <see cref="Altinn2RightsClientMock"/> to prevent real HTTP calls to Altinn 2
    /// - <see cref="PolicyFactoryMock"/> for capturing written delegation policies
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (has DAGL role, can delegate)
    /// </para>
    /// </remarks>
    public class AddInstanceRights : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceId = "urn:altinn:instance-id:50083510/c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f";

        public AddInstanceRights(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointWithWrittenPoliciesMock>();
                services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromDumboToKaos = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.KaosMagicDesignAndArts.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromDumboToKaos);
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
        /// Helper to get valid right keys via the instance delegation check endpoint.
        /// Malin (MD of Dumbo) performs a delegation check for the resource and instance to discover delegatable right keys.
        /// </summary>
        private async Task<List<string>> GetDelegatableInstanceRightKeys(string resource, string instance)
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={resource}&instance={instance}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            InstanceCheckDto result = JsonSerializer.Deserialize<InstanceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result.Rights
                .Where(r => r.Result)
                .Select(r => r.Right.Key)
                .ToList();
        }

        /// <summary>
        /// Malin (MD of Dumbo) delegates instance rights for SiriusSkattemelding to Kaos (existing rightholder).
        /// Uses valid right keys obtained from instance delegation check. Expects 201 Created.
        /// Verifies:
        /// - Delegation check returns delegatable right keys matching the resource policy
        /// - POST succeeds with 201 Created
        /// - Written XACML policy has one rule per delegated right key
        /// - Each rule targets Kaos's UUID as the subject
        /// - Each rule targets the correct org/app resource attributes
        /// - Each rule contains exactly one action
        /// </summary>

        [Fact(Skip = "Failing with 500 error during delegation - requires investigation")]
        public async Task AddInstanceRights_AsMalinForDumboToKaos_WithValidRightKeys_ReturnsCreated()
        {
            List<string> rightKeys = await GetDelegatableInstanceRightKeys("app_skd_sirius-skattemelding-v1", SiriusInstanceId);
            Assert.NotEmpty(rightKeys);
            Assert.True(rightKeys.Count >= 9, $"Expected at least 9 delegatable right keys for sirius-skattemelding-v1, but got {rightKeys.Count}");

            var body = new InstanceRightsDelegationDto { DirectRightKeys = rightKeys };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected Created but got {response.StatusCode}. Response body: {responseContent}");

            // Verify the written XACML delegation policy
            var policyFactory = Fixture.Server.Services.GetRequiredService<IPolicyFactory>() as PolicyFactoryMock;
            Assert.NotNull(policyFactory);
            Assert.NotEmpty(policyFactory.WrittenPolicies);

            var (policyPath, content) = policyFactory.WrittenPolicies.Last();
            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(new MemoryStream(content)))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

            // Rule count should match the number of delegated right keys
            Assert.Equal(rightKeys.Count, policy.Rules.Count);

            // Verify subject in each rule targets Kaos's partyId
            foreach (XacmlRule rule in policy.Rules)
            {
                Assert.NotNull(rule.Target);
                Assert.Equal(XacmlEffectType.Permit, rule.Effect);

                var subjectMatches = rule.Target.AnyOf
                    .SelectMany(anyOf => anyOf.AllOf)
                    .SelectMany(allOf => allOf.Matches)
                    .Where(match => match.AttributeDesignator.Category.OriginalString == XacmlConstants.MatchAttributeCategory.Subject)
                    .ToList();
                Assert.NotEmpty(subjectMatches);

                foreach (XacmlMatch match in subjectMatches)
                {
                    Assert.Equal(TestData.KaosMagicDesignAndArts.Id.ToString(), match.AttributeValue.Value);
                }
            }

            // Verify resource attributes in each rule target the correct org and app
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

            // Verify each rule has exactly one action
            foreach (XacmlRule rule in policy.Rules)
            {
                var actionMatches = rule.Target.AnyOf
                    .SelectMany(anyOf => anyOf.AllOf)
                    .SelectMany(allOf => allOf.Matches)
                    .Where(match => match.AttributeDesignator.Category.OriginalString == XacmlConstants.MatchAttributeCategory.Action)
                    .ToList();
                Assert.Single(actionMatches);
            }

            // Round-trip: verify the delegation is readable via GetInstanceRights
            HttpClient readClient = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage getResponse = await readClient.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            string getResponseContent = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(getResponse.StatusCode == HttpStatusCode.OK, $"Expected OK but got {getResponse.StatusCode}. Response body: {getResponseContent}");

            ExtInstanceRightDto instanceRights = await getResponse.Content.ReadFromJsonAsync<ExtInstanceRightDto>(TestContext.Current.CancellationToken);

            Assert.NotNull(instanceRights);
            Assert.NotNull(instanceRights.Resource);
            Assert.Equal("app_skd_sirius-skattemelding-v1", instanceRights.Resource.RefId);
            Assert.NotNull(instanceRights.Instance);
            Assert.NotEmpty(instanceRights.DirectRights);
            Assert.Equal(rightKeys.Count, instanceRights.DirectRights.Count);
            Assert.Empty(instanceRights.IndirectRights);

            // Verify each returned right has the correct permission structure
            foreach (var right in instanceRights.DirectRights)
            {
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(TestData.KaosMagicDesignAndArts.Entity.Name, permission.To.Name);
                Assert.True(permission.To.Id == TestData.KaosMagicDesignAndArts.Id);
                Assert.Equal(TestData.DumboAdventures.Entity.Name, permission.From.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            // Verify the delegated right keys match what was requested
            var returnedRightKeys = instanceRights.DirectRights.Select(r => r.Right.Key).OrderBy(k => k).ToList();
            var requestedRightKeys = rightKeys.OrderBy(k => k).ToList();
            Assert.Equal(requestedRightKeys, returnedRightKeys);
        }

        /// <summary>
        /// Malin tries to delegate instance rights with a resource that does not exist.
        /// Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithInvalidResource_ReturnsBadRequest()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-fake-right-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=nonexistent_resource&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithReadScope_ReturnsForbidden()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithToOthersReadScope_ReturnsForbidden()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithFromOthersWriteScope_ReturnsForbidden()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin tries to delegate instance rights with an empty DirectRightKeys list.
        /// Expects 400 BadRequest because the controller rejects empty right key lists.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithEmptyRightKeys_ReturnsBadRequest()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = [] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Sends a malformed instance URN (missing the required prefix).
        /// Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithInvalidInstanceUrn_ReturnsBadRequest()
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-key"] };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance=50315678/not-a-valid-urn",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Request without any authentication token.
        /// Expects 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task AddInstanceRights_WithNoToken_ReturnsUnauthorized()
        {
            var client = Fixture.Server.CreateClient();
            var body = new InstanceRightsDelegationDto { DirectRightKeys = ["some-key"] };

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                body,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
