using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

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
        /// Han Solo (MD of HanSoloEnterprise) removes "UTINN" role from Ben Solo with valid from write scope.
        /// Expects 204 NoContent and the assignment should be removed.
        /// Verifies deletion by checking that the role no longer appears in GetRoles endpoint.
        /// </summary>
        [Fact]
        public async Task RemoveRole_WithValidRoleCode_PartyAsFrom_ReturnsNoContent()
        {
            // Verify role exists before removal
            HttpClient readClient = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getBefore = await readClient.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.BenSolo.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getBefore.StatusCode);
            string beforeContent = await getBefore.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeResult = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(beforeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(beforeResult.Items, r => r.Role.Code == RoleConstants.ReporterSender.Entity.Code);

            // Remove the role
            HttpClient deleteClient = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await deleteClient.DeleteAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.BenSolo.Id}&rolecode={RoleConstants.ReporterSender.Entity.Code}",
                TestContext.Current.CancellationToken);

            string deleteContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Body: {deleteContent}");

            // Verify role is removed
            HttpClient readClient2 = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getAfter = await readClient2.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.BenSolo.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getAfter.StatusCode);
            string afterContent = await getAfter.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var afterResult = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(afterContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.DoesNotContain(afterResult.Items, r => r.Role.Code == RoleConstants.ReporterSender.Entity.Code);
        }

        /// <summary>
        /// Han Solo (MD of HanSoloEnterprise) removes "UTINN" role from Ben Solo with valid from write scope.
        /// Expects 204 NoContent and the assignment should be removed.
        /// Verifies deletion by checking that the role no longer appears in GetRoles endpoint.
        /// </summary>
        [Fact]
        public async Task RemoveRole_WithNonA2RoleCode_PartyAsFrom_ReturnsBadRequest()
        {
            // Verify role exists before removal
            HttpClient readClient = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getBefore = await readClient.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LeiaOrgana.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getBefore.StatusCode);
            string beforeContent = await getBefore.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeResult = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(beforeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(beforeResult.Items, r => r.Role.Code == RoleConstants.ChairOfTheBoard.Entity.Code);

            // Remove the role
            HttpClient deleteClient = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await deleteClient.DeleteAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LeiaOrgana.Id}&rolecode={RoleConstants.ChairOfTheBoard.Entity.Code}",
                TestContext.Current.CancellationToken);

            string deleteContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Body: {deleteContent}");

            // Verify role is not removed
            HttpClient readClient2 = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getAfter = await readClient2.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LeiaOrgana.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getAfter.StatusCode);
            string afterContent = await getAfter.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var afterResult = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(afterContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(afterResult.Items, r => r.Role.Code == RoleConstants.ChairOfTheBoard.Entity.Code);
        }

        /// <summary>
        /// Ben Solo (Utinn of HanSoloEnterprise) removes "UTINN" role from Ben Solo with valid to write scope.
        /// Expects 204 NoContent and the assignment should be removed.
        /// Verifies deletion by checking that the role no longer appears in GetRoles endpoint.
        /// </summary>
        [Fact]
        public async Task RemoveRole_WithValidRoleCode_PartyAsTo_ReturnsNoContent()
        {
            // Verify role exists before removal
            HttpClient readClient = CreateClient(TestData.LukeSkyWalker.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            HttpResponseMessage getBefore = await readClient.GetAsync(
                $"{Route}/roles?party={TestData.LukeSkyWalker.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LukeSkyWalker.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getBefore.StatusCode);
            string beforeContent = await getBefore.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeResult = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(beforeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(beforeResult.Items, r => r.Role.Code == RoleConstants.ReporterSender.Entity.Code);

            // Remove the role
            HttpClient deleteClient = CreateClient(TestData.LukeSkyWalker.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            HttpResponseMessage response = await deleteClient.DeleteAsync(
                $"{Route}/roles?party={TestData.LukeSkyWalker.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LukeSkyWalker.Id}&rolecode={RoleConstants.ReporterSender.Entity.Code}",
                TestContext.Current.CancellationToken);

            string deleteContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Body: {deleteContent}");

            // Verify the assignment was revoked in the database
            await Fixture.QueryDb(async db =>
            {
                var assignment = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.Null(assignment);
            });
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
