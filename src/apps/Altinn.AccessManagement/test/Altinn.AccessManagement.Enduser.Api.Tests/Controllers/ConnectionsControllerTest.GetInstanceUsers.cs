using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the GetInstanceUsers
/// (GET resources/instances/users) endpoint which returns all users who have access to a specific instance.
/// Reuses the default seed data where Kaos has delegated instance rights to Josephine.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetInstanceUsers(Guid, string, string, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Kaos Magic Design and Arts → Josephine Yvonnesdottir (Rightholder)
    /// - AssignmentInstance: SiriusSkattemelding instance delegated to Josephine
    /// - AssignmentInstance: MattilsynetBakeryService instance delegated to Josephine
    /// </para>
    /// <para>
    /// Actors:
    /// - Jinx Arcane: MD of Kaos (queries who has access to Kaos's instances)
    /// </para>
    /// </remarks>
    public class GetInstanceUsers : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceId = "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
        private const string MattilsynetInstanceId = "urn:altinn:instance-id:50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e";

        public GetInstanceUsers(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
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
        /// Jinx (MD of Kaos) queries who has access to the SiriusSkattemelding instance.
        /// Expects OK with Josephine listed as a user with access.
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_AsJinxForKaos_SiriusSkattemelding_ReturnsJosephine()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<SimplifiedPartyDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            var josephine = result.Items.FirstOrDefault(u => u.Id == TestData.JosephineYvonnesdottir.Id);
            Assert.NotNull(josephine);
            Assert.Equal("Josephine Yvonnesdottir", josephine.Name);
            Assert.Equal("Person", josephine.Type);
            Assert.False(josephine.IsDeleted);
        }

        /// <summary>
        /// Jinx queries who has access to the MattilsynetBakeryService instance.
        /// Expects OK with Josephine listed (same rightholder, different resource instance).
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_AsJinxForKaos_MattilsynetBakery_ReturnsJosephine()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_mat_mattilsynet-baker-konditorvare&instance={MattilsynetInstanceId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<SimplifiedPartyDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.Contains(result.Items, u => u.Id == TestData.JosephineYvonnesdottir.Id);
        }

        /// <summary>
        /// Jinx queries instance users for an instance that has no delegations.
        /// Expects OK with an empty list.
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_AsJinxForKaos_NonExistentInstance_ReturnsEmptyList()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance=urn:altinn:instance-id:50315678/00000000-0000-0000-0000-000000000000",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<SimplifiedPartyDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        /// <summary>
        /// Jinx uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses to-others read scope (not write) on the endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses from-others write scope on the endpoint.
        /// Expects 403 Forbidden (requires to-others write).
        /// </summary>
        [Fact]
        public async Task GetInstanceUsers_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/users?party={TestData.KaosMagicDesignAndArts.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
