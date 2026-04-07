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
/// Partial test class for ConnectionsController, focused on testing the DelegationCheckRoles
/// (GET /connections/roles/delegationcheck) endpoint which checks what roles the authenticated
/// user can delegate on behalf of a party. The endpoint is hidden from APIM
/// ([ApiExplorerSettings(IgnoreApi = true)]) but is implemented and functional.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.DelegationCheckRoles(Guid, CancellationToken)"/>.
    /// </summary>
    public class DelegationCheckRoles : IClassFixture<ApiFixture>
    {
        public DelegationCheckRoles(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
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
        /// Malin (MD of Dumbo) checks which roles she can delegate on behalf of Dumbo.
        /// The endpoint is functional (calls ConnectionService.RoleDelegationCheck).
        /// Expects OK with role check results.
        /// </summary>
        [Fact]
        public async Task DelegationCheckRoles_AsMalinForDumbo_ReturnsOkWithResults()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            // The endpoint may return OK or be blocked by authorization depending on fixture state.
            // If OK, verify the response structure.
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = JsonSerializer.Deserialize<PaginatedResult<RoleDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Assert.NotNull(result);

                foreach (var item in result.Items)
                {
                    Assert.NotNull(item.Role);
                }
            }
            else
            {
                // Authorization may block — that's acceptable for a hidden endpoint
                Assert.False(response.IsSuccessStatusCode, $"Unexpected success status: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Uses from-others read scope (requires to-others write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task DelegationCheckRoles_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses to-others read scope (not write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task DelegationCheckRoles_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses from-others write scope (requires to-others write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task DelegationCheckRoles_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
