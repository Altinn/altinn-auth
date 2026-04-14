using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the RemoveRole
/// (DELETE /connections/roles) endpoint. This endpoint is currently not implemented —
/// it always returns 404 NotFound. These tests document the current behavior and will
/// detect when the endpoint is activated.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveRole(Guid, Guid, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// The endpoint is marked with [ApiExplorerSettings(IgnoreApi = true)] and returns NotFound()
    /// unconditionally. The tests verify this behavior and scope enforcement.
    /// </remarks>
    public class RemoveRole : IClassFixture<ApiFixture>
    {
        public RemoveRole(ApiFixture fixture)
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
        /// RemoveRole is not yet implemented — the method body returns NotFound() unconditionally.
        /// However, the authorization middleware may reject before the action runs depending on
        /// fixture state. This test verifies that the endpoint does NOT return a success status,
        /// confirming it is not operational. When the endpoint is implemented, this test should be
        /// replaced with proper functional tests.
        /// </summary>
        [Fact]
        public async Task RemoveRole_IsNotOperational()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}&roleCode=DAGL",
                TestContext.Current.CancellationToken);

            // The endpoint is not implemented — it should never return a success status (2xx)
            Assert.False(response.IsSuccessStatusCode, $"RemoveRole should not be operational, but got {response.StatusCode}");
        }

        /// <summary>
        /// Uses read scope — authorization still runs before the NotFound return.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveRole_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}&roleCode=DAGL",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
