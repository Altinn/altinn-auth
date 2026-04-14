using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial class for connections controller tests. This part focuses on testing the GetInstanceRights endpoint,
/// which returns direct and indirect rights for a specific instance delegation between two parties.
/// The tests reuse seeded instance delegation data (Kaos → Josephine) with XACML delegation policies
/// that define the specific rights (read, write) delegated for each instance.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetInstanceRights(Guid, Guid, Guid, string, string, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Kaos Magic Design and Arts → Josephine Yvonnesdottir (Rightholder)
    /// - AssignmentInstance: SiriusSkattemelding with delegation policy granting read+write (+ Task_1 read+write = 4 rights)
    /// - AssignmentInstance: MattilsynetBakeryService with delegation policy granting read (1 right)
    /// </para>
    /// <para>
    /// Actors:
    /// - Jinx Arcane: managing director of Kaos (views from Kaos's perspective, to-others)
    /// - Josephine Yvonnesdottir: rightholder for Kaos (views from her perspective, from-others)
    /// </para>
    /// </remarks>
    public class GetInstanceRights : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceId = "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
        private const string MattilsynetInstanceId = "urn:altinn:instance-id:50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e";

        public GetInstanceRights(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
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
        /// Jinx (MD of Kaos) queries instance rights for SiriusSkattemelding delegated to Josephine in the to-others direction.
        /// Expects OK with direct rights (read, write from the delegation policy).
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_AsJinxForKaosToJosephine_SiriusSkattemelding_WithToOthersScope_ReturnsOkWithDirectRights()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ExtInstanceRightDto result = await response.Content.ReadFromJsonAsync<ExtInstanceRightDto>(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.NotNull(result.Resource);
            Assert.Equal("app_skd_sirius-skattemelding-v1", result.Resource.RefId);
            Assert.NotEmpty(result.DirectRights);
            Assert.Empty(result.IndirectRights);

            foreach (var right in result.DirectRights)
            {
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(TestData.JosephineYvonnesdottir.Entity.Name, permission.To.Name);
                Assert.True(permission.To.Id == TestData.JosephineYvonnesdottir.Id);
                Assert.Equal(TestData.KaosMagicDesignAndArts.Entity.Name, permission.From.Name);
                Assert.True(permission.From.Id == TestData.KaosMagicDesignAndArts.Id);
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
            }
        }

        /// <summary>
        /// Josephine queries instance rights for SiriusSkattemelding received from Kaos in the from-others direction.
        /// Expects OK with the same direct rights.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_AsJosephineFromKaos_SiriusSkattemelding_WithFromOthersScope_ReturnsOkWithDirectRights()
        {
            HttpClient client = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.JosephineYvonnesdottir.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ExtInstanceRightDto result = await response.Content.ReadFromJsonAsync<ExtInstanceRightDto>(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.NotNull(result.Resource);
            Assert.Equal("app_skd_sirius-skattemelding-v1", result.Resource.RefId);
            Assert.NotEmpty(result.DirectRights);
            Assert.Empty(result.IndirectRights);
        }

        /// <summary>
        /// Jinx queries instance rights for MattilsynetBakeryService delegated to Josephine.
        /// The delegation policy grants only read, so fewer rights than SiriusSkattemelding.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_AsJinxForKaosToJosephine_MattilsynetBakery_WithToOthersScope_ReturnsOkWithDirectRights()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_mat_mattilsynet-baker-konditorvare&instance={MattilsynetInstanceId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ExtInstanceRightDto result = await response.Content.ReadFromJsonAsync<ExtInstanceRightDto>(TestContext.Current.CancellationToken);

            Assert.NotNull(result);
            Assert.NotNull(result.Resource);
            Assert.Equal("app_mat_mattilsynet-baker-konditorvare", result.Resource.RefId);
            Assert.NotEmpty(result.DirectRights);
            Assert.Empty(result.IndirectRights);
        }

        /// <summary>
        /// Josephine uses the wrong scope (to-others read) when querying from-others direction.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_AsJosephineFromKaos_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.JosephineYvonnesdottir.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses from-others read scope for a to-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_AsJinxForKaosToJosephine_WithFromOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses a write scope on the read-only GetInstanceRights endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_WithWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        /// <summary>
        /// Sends a malformed instance URN (missing the required prefix).
        /// Expects 400 BadRequest with a validation error.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_WithInvalidInstanceUrn_ReturnsBadRequest()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance=invalid-format-no-urn-prefix",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Request without any authentication token.
        /// Expects 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_WithNoToken_ReturnsUnauthorized()
        {
            var client = Fixture.Server.CreateClient();

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/rights?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceId}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
