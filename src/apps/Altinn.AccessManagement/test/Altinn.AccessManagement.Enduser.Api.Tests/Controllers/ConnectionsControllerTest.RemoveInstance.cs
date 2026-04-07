using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
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
/// Partial test class for ConnectionsController, focused on testing the RemoveInstance
/// (DELETE resources/instances) endpoint which removes an instance delegation between two parties.
/// Tests cover removal from both the delegator (Malin/Dumbo, to-others) and receiver (Jinx/Kaos, from-others)
/// perspectives, scope enforcement, and round-trip verification that the instance is actually gone.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveInstance(Guid, Guid, Guid, string, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Dumbo Adventures -> Kaos Magic Design and Arts (Rightholder) seeded locally
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: MD of Dumbo Adventures (delegator, to-others perspective)
    /// - Jinx Arcane: MD of Kaos Magic Design and Arts (receiver, from-others perspective)
    /// </para>
    /// </remarks>
    public class RemoveInstance : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceIdForRemove = "urn:altinn:instance-id:50083510/e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091";
        private const string MattilsynetInstanceIdForRemove = "urn:altinn:instance-id:50083510/f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102";

        public RemoveInstance(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.InstanceDbEf);
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.ResourceDelegationEF);
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

        private async Task<List<string>> GetDelegatableInstanceRightKeys(string resource, string instance)
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={resource}&instance={instance}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Delegation check failed: {response.StatusCode}. Body: {responseContent}");

            InstanceCheckDto result = JsonSerializer.Deserialize<InstanceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result.Rights.Where(r => r.Result).Select(r => r.Right.Key).ToList();
        }

        private async Task AddInstanceRights(string resource, string instance, IEnumerable<string> rightKeys)
        {
            var body = new InstanceRightsDelegationDto { DirectRightKeys = rightKeys };
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsJsonAsync(
                $"{Route}/resources/instances/rights?party={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource={resource}&instance={instance}",
                body,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"Add instance rights failed: {response.StatusCode}. Body: {responseContent}");
        }

        /// <summary>
        /// Malin (MD of Dumbo) adds instance rights for SiriusSkattemelding to Kaos, then removes the
        /// instance delegation. Verifies:
        /// - Instance exists after add (via GetInstances)
        /// - DELETE returns 204 NoContent
        /// - Instance is gone after delete (via GetInstances)
        /// </summary>
        [Fact]
        public async Task RemoveInstance_AsMalinForDumboToKaos_ReturnsNoContentAndRemovesInstance()
        {
            List<string> rightKeys = await GetDelegatableInstanceRightKeys("app_skd_sirius-skattemelding-v1", SiriusInstanceIdForRemove);
            Assert.NotEmpty(rightKeys);

            await AddInstanceRights("app_skd_sirius-skattemelding-v1", SiriusInstanceIdForRemove, rightKeys);

            // Verify the instance exists before removal
            HttpClient readClient = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getBeforeResponse = await readClient.GetAsync(
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, getBeforeResponse.StatusCode);
            string beforeContent = await getBeforeResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeInstances = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(beforeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(beforeInstances.Items, i => i.Instance.RefId == SiriusInstanceIdForRemove);

            // Delete the instance delegation
            HttpClient deleteClient = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForRemove}",
                TestContext.Current.CancellationToken);

            string deleteContent = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {deleteResponse.StatusCode}. Body: {deleteContent}");

            // Verify the instance is gone after removal
            HttpClient readClient2 = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getAfterResponse = await readClient2.GetAsync(
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForRemove}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, getAfterResponse.StatusCode);
            string afterContent = await getAfterResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var afterInstances = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(afterContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.DoesNotContain(afterInstances.Items, i => i.Instance.RefId == SiriusInstanceIdForRemove);
        }

        /// <summary>
        /// Jinx (MD of Kaos, receiver) removes instance rights from the from-others direction.
        /// Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemoveInstance_AsJinxForKaosFromDumbo_WithFromOthersWriteScope_ReturnsNoContent()
        {
            List<string> rightKeys = await GetDelegatableInstanceRightKeys("app_mat_mattilsynet-baker-konditorvare", MattilsynetInstanceIdForRemove);
            Assert.NotEmpty(rightKeys);

            await AddInstanceRights("app_mat_mattilsynet-baker-konditorvare", MattilsynetInstanceIdForRemove, rightKeys);

            // Jinx removes as receiver (from-others)
            HttpClient deleteClient = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(
                $"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_mat_mattilsynet-baker-konditorvare&instance={MattilsynetInstanceIdForRemove}",
                TestContext.Current.CancellationToken);

            string deleteContent = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {deleteResponse.StatusCode}. Body: {deleteContent}");
        }

        /// <summary>
        /// Malin uses from-others read scope on the delete endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveInstance_WithFromOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForRemove}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on the delete endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveInstance_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/resources/instances?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForRemove}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
