using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial class for connections controller tests. This part focuses on testing the GetInstances endpoint,
/// which retrieves instance permissions associated with a connection between two parties. The tests cover
/// different actor perspectives (Jinx as MD of Kaos, Josephine as rightholder), delegation directions
/// (from-others vs to-others), and scope-based authorization scenarios.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetInstances(Guid, Guid?, Guid?, AccessManagement.Api.Enduser.Models.PagingInput, string, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Kaos Magic Design and Arts → Josephine Yvonnesdottir (Rightholder)
    /// - AssignmentInstance: SiriusSkattemelding instance "50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"
    /// - AssignmentInstance: MattilsynetBakeryService instance "50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"
    /// </para>
    /// <para>
    /// Actors:
    /// - Jinx Arcane: managing director of Kaos Magic Design and Arts (views from Kaos's perspective)
    /// - Josephine Yvonnesdottir: rightholder for Kaos (views from Josephine's perspective)
    /// </para>
    /// <para>
    /// The tests verify scope-based authorization (from-others vs to-others read scopes),
    /// that the correct instances are returned in the response, and that mismatched scopes
    /// result in HTTP 403 Forbidden.
    /// </para>
    /// </remarks>
    public class GetInstances : IClassFixture<ApiFixture>
    {
        public GetInstances(ApiFixture fixture)
        {
            Fixture = fixture;
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
        /// Jinx (MD of Kaos) queries instances delegated to Josephine in the to-others direction.
        /// Expects OK with both SiriusSkattemelding and MattilsynetBakeryService instances.
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJinxForKaosToJosephine_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            PaginatedResult<InstancePermissionDto> result = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2, $"Expected at least 2 instances but got {result.Items.Count()}. Response body: {responseContent}");
            Assert.Contains(result.Items, i => i.Instance.RefId == "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
            Assert.Contains(result.Items, i => i.Instance.RefId == "urn:altinn:instance-id:50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
        }

        /// <summary>
        /// Jinx (MD of Kaos) queries instances from Josephine in the from-others direction.
        /// Expects OK (Josephine has no instances delegated toward Kaos, so the list may be empty).
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJinxForKaosFromJosephine_WithFromOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.KaosMagicDesignAndArts.Id}&from={TestData.JosephineYvonnesdottir.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            PaginatedResult<InstancePermissionDto> result = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        /// <summary>
        /// Josephine queries instances that Kaos delegated to her, viewed from her perspective (from-others).
        /// Expects OK with both SiriusSkattemelding and MattilsynetBakeryService instances.
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJosephineFromKaos_WithFromOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.JosephineYvonnesdottir.Id}&to={TestData.JosephineYvonnesdottir.Id}&from={TestData.KaosMagicDesignAndArts.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            PaginatedResult<InstancePermissionDto> result = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.True(result.Items.Count() >= 2, $"Expected at least 2 instances but got {result.Items.Count()}. Response body: {responseContent}");
            Assert.Contains(result.Items, i => i.Instance.RefId == "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d");
            Assert.Contains(result.Items, i => i.Instance.RefId == "urn:altinn:instance-id:50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e");
        }

        /// <summary>
        /// Josephine queries instances in the to-others direction (what Josephine gives to Kaos).
        /// Expects OK (no instances delegated in this direction).
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJosephineToKaos_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.JosephineYvonnesdottir.Id}&from={TestData.JosephineYvonnesdottir.Id}&to={TestData.KaosMagicDesignAndArts.Id}", TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");
        }

        /// <summary>
        /// Josephine uses the wrong scope (to-others read) when querying from-others direction.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJosephineFromKaos_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.JosephineYvonnesdottir.Id}&to={TestData.JosephineYvonnesdottir.Id}&from={TestData.KaosMagicDesignAndArts.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses from-others read scope for a to-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJinxForKaosToJosephine_WithFromOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses a write scope on the read-only GetInstances endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetInstances_WithWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Request without any authentication token.
        /// Expects 401 Unauthorized.
        /// </summary>
        [Fact]
        public async Task GetInstances_WithNoToken_ReturnsUnauthorized()
        {
            var client = Fixture.Server.CreateClient();

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Jinx queries instances with a specific resource filter.
        /// Only SiriusSkattemelding instances should be returned, not MattilsynetBakeryService.
        /// </summary>
        [Fact]
        public async Task GetInstances_AsJinxForKaosToJosephine_FilterByResource_ReturnsOnlyMatchingResource()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            PaginatedResult<InstancePermissionDto> result = JsonSerializer.Deserialize<PaginatedResult<InstancePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // All returned instances should be for SiriusSkattemelding only
            Assert.All(result.Items, i => Assert.Equal("app_skd_sirius-skattemelding-v1", i.Resource.RefId));
            // Should not contain the MattilsynetBakeryService instance
            Assert.DoesNotContain(result.Items, i => i.Resource.RefId == "app_mat_mattilsynet-baker-konditorvare");
        }
    }
}
