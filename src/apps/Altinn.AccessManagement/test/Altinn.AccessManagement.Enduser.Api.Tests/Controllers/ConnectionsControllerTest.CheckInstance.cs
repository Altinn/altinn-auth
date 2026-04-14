using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
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
/// Partial test class for ConnectionsController, focused on testing the CheckInstance
/// (GET resources/instances/delegationcheck) endpoint which checks what instance rights
/// an authenticated user can delegate on behalf of a party. The tests verify that Malin (MD of Dumbo)
/// gets a full set of delegatable right keys, and that scope enforcement works correctly.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckInstance(Guid, string, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (can delegate on behalf of Dumbo)
    /// </para>
    /// <para>
    /// The tests verify that the delegation check returns the correct delegatable right keys
    /// for the sirius-skattemelding-v1 resource, and that scope and authorization enforcement works.
    /// </para>
    /// </remarks>
    public class CheckInstance : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceIdForCheck = "urn:altinn:instance-id:50083510/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5f";

        public CheckInstance(ApiFixture fixture)
        {
            Fixture = fixture;
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
        /// Malin (MD of Dumbo) checks which instance rights she can delegate for SiriusSkattemelding.
        /// Expects OK with delegatable right keys matching the resource policy (at least 9 rights covering
        /// instantiate, read, write, confirm across workflow stages).
        /// </summary>
        [Fact]
        public async Task CheckInstance_AsMalinForDumbo_SiriusSkattemelding_ReturnsOkWithDelegatableRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            InstanceCheckDto result = JsonSerializer.Deserialize<InstanceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            // Verify resource info
            Assert.NotNull(result.Resource);
            Assert.Equal("app_skd_sirius-skattemelding-v1", result.Resource.RefId);

            // Verify instance info
            Assert.NotNull(result.Instance);

            // Verify rights
            Assert.NotNull(result.Rights);
            var rights = result.Rights.ToList();
            Assert.True(rights.Count >= 9, $"Expected at least 9 rights for sirius-skattemelding-v1, but got {rights.Count}");

            // All rights should be delegatable (PermitPdpMock always permits)
            Assert.DoesNotContain(rights, r => r.Result == false);

            // Verify each right has a key and a name
            foreach (var right in rights)
            {
                Assert.NotNull(right.Right);
                Assert.False(string.IsNullOrEmpty(right.Right.Key), "Right key should not be empty");
                Assert.False(string.IsNullOrEmpty(right.Right.Name), "Right name should not be empty");
            }
        }

        /// <summary>
        /// Malin checks MattilsynetBakeryService instance delegation rights.
        /// Expects OK with delegatable rights (fewer than SiriusSkattemelding since the resource has fewer actions).
        /// </summary>
        [Fact]
        public async Task CheckInstance_AsMalinForDumbo_MattilsynetBakery_ReturnsOkWithDelegatableRights()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_mat_mattilsynet-baker-konditorvare&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            InstanceCheckDto result = JsonSerializer.Deserialize<InstanceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Resource);
            Assert.Equal("app_mat_mattilsynet-baker-konditorvare", result.Resource.RefId);

            var rights = result.Rights.ToList();
            Assert.NotEmpty(rights);
            Assert.DoesNotContain(rights, r => r.Result == false);

            // Mattilsynet has fewer rights than Sirius (instantiate, read, write, delete, complete, events/read)
            // but more than 2
            Assert.True(rights.Count >= 2, $"Expected at least 2 rights for mattilsynet, but got {rights.Count}");
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses to-others read scope (not write) on the delegation check endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on the delegation check endpoint.
        /// Expects 403 Forbidden (requires to-others write).
        /// </summary>
        [Fact]
        public async Task CheckInstance_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
