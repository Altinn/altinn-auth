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

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the GetRoles
/// (GET /connections/roles) endpoint which returns roles between two parties.
/// Reuses the default seed where Dumbo→Malin is ManagingDirector and Dumbo→Thea is Rightholder.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetRoles(Guid, Guid, Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Dumbo Adventures → Malin Emilie (ManagingDirector / DAGL)
    /// - Assignment: Dumbo Adventures → Thea BFF (Rightholder)
    /// - Assignment: Kaos Magic Design and Arts → Jinx Arcane (ManagingDirector / DAGL)
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: MD of Dumbo Adventures (to-others perspective)
    /// - Thea BFF: Rightholder at Dumbo, MD of Mille (from-others perspective)
    /// - Jinx Arcane: MD of Kaos (to-others perspective)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    [Collection(ConnectionsReadOnlyCollection.Name)]
    public class GetRoles
    {
        public GetRoles(ApiFixture fixture)
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
        /// Malin (MD of Dumbo) queries roles from Dumbo to Malin in the to-others direction.
        /// Expects OK with ManagingDirector (DAGL) role and correct permission structure.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsManagingDirectorToSelf_WithToOthersScope_ReturnsOkWithDaglRole()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // Malin should have ManagingDirector role from Dumbo
            var daglRole = result.Items.FirstOrDefault(r => r.Role.Id == RoleConstants.ManagingDirector.Id);
            Assert.NotNull(daglRole);
            Assert.NotEmpty(daglRole.Permissions);

            PermissionDto perm = daglRole.Permissions.First();
            Assert.Equal(TestData.DumboAdventures.Entity.Name, perm.From.Name);
            Assert.True(perm.From.Id == TestData.DumboAdventures.Id);
            Assert.Equal(TestData.MalinEmilie.Entity.Name, perm.To.Name);
            Assert.True(perm.To.Id == TestData.MalinEmilie.Id);
        }

        /// <summary>
        /// Malin queries roles from Dumbo to Thea in the to-others direction.
        /// Expects OK with Rightholder role.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsManagingDirectorToRightholder_WithToOthersScope_ReturnsOkWithRightholderRole()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            var rightholderRole = result.Items.FirstOrDefault(r => r.Role.Id == RoleConstants.Rightholder.Id);
            Assert.NotNull(rightholderRole);
        }

        /// <summary>
        /// Thea queries roles she has from Dumbo in the from-others direction.
        /// Expects OK with Rightholder role.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsRightholderFromOrganization_WithFromOthersScope_ReturnsOkWithRightholderRole()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.Contains(result.Items, r => r.Role.Id == RoleConstants.Rightholder.Id);
        }

        /// <summary>
        /// Jinx queries roles from Kaos to Josephine. Josephine has Rightholder role at Kaos.
        /// Expects OK with Rightholder role.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsManagingDirectorToInheritedRightholder_WithToOthersScope_ReturnsOkWithRightholderRole()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            var rightholderRole = Assert.Single(result.Items, r => r.Role.Id == RoleConstants.Rightholder.Id);
            Assert.False(rightholderRole.Role.IsRevocable);
            Assert.All(rightholderRole.Permissions, permission => Assert.NotNull(permission.Reason));
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries roles from Dumbo to Malin. ManagingDirector maps to Altinn2 roles (e.g. UTINN) via RoleMap.
        /// These inherited Altinn2 roles should have IsRevocable set to false since they are not directly assigned.
        /// </summary>
        [Fact]
        public async Task GetRoles_InheritedAltinn2Role_IsRevocableIsFalse()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            // Find Altinn2 roles inherited via RoleMap from ManagingDirector (e.g. UTINN or REGNA)
            var inheritedAltinn2Roles = result.Items.Where(r =>
                r.Role.Provider?.Code == "sys-altinn2"
                && r.Role.IsRevocable == false).ToList();

            Assert.NotEmpty(inheritedAltinn2Roles);
            foreach (var role in inheritedAltinn2Roles)
            {
                Assert.False(role.Role.IsRevocable, $"Inherited Altinn2 role '{role.Role.Code}' should have IsRevocable=false");
            }
        }

        /// <summary>
        /// Han Solo (MD of Dumbo) queries roles from HanSoloEnterprise to Luke SkyWalker. Luke SkyWalker has a directly assigned Altinn2 role (UTINN/ReporterSender).
        /// The directly assigned Altinn2 role should have IsRevocable set to true.
        /// </summary>
        [Fact]
        public async Task GetRoles_DirectAltinn2Role_IsRevocableIsTrue()
        {
            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LukeSkyWalker.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            // Find the directly assigned REGNA (AccountingEmployee) Altinn2 role
            var utinnRole = result.Items.FirstOrDefault(r => r.Role.Code == "utinn");
            Assert.NotNull(utinnRole);
            Assert.True(utinnRole.Role.IsRevocable, "Directly assigned Altinn2 role UTINN should have IsRevocable=true");
        }

        /// <summary>
        /// Test of GetRoles endpoint without specifying 'to' parameter. This should return all roles from the 'from' party to any 'to' party.
        /// </summary>
        [Fact]
        public async Task GetRoles_WithNoToDefined_Returns200OkWithAllToParties()
        {
            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            HashSet<Guid> toIds = [];
            foreach (var role in result.Items)
            {
                foreach (var permission in role.Permissions)
                {
                    toIds.Add(permission.To.Id);
                }
            }

            Assert.NotEmpty(toIds);
            Assert.Equal(4, toIds.Count);
            Assert.Contains(TestData.HanSolo.Id, toIds);
            Assert.Contains(TestData.LeiaOrgana.Id, toIds);
            Assert.Contains(TestData.BenSolo.Id, toIds);
            Assert.Contains(TestData.LukeSkyWalker.Id, toIds);
        }

        /// <summary>
        /// Thea uses to-others scope when querying from-others direction (wrong scope for direction).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsRightholderFromOrganization_WithToOthersScope_Returns403ForToOthersScopeOnFromOthersDirection()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others scope when querying to-others direction (wrong scope for direction).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetRoles_AsManagingDirectorToSelf_WithFromOthersScope_Returns403ForFromOthersScopeOnToOthersDirection()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses a write scope on the read-only GetRoles endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetRoles_WithWriteScope_Returns403ForWriteScope()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MalinEmilie.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
