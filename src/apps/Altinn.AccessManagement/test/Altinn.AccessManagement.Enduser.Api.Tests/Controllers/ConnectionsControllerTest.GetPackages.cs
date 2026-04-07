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
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the GetPackages
/// (GET /connections/accesspackages) endpoint which returns access packages between two parties.
/// Reuses the default seed where Dumbo→Thea has a SalarySpecialCategory package assignment.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetPackages(Guid, Guid?, Guid?, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Dumbo Adventures → Thea BFF (Rightholder)
    /// - AssignmentPackage: SalarySpecialCategory (lonn-personopplysninger-saerlig-kategori)
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: MD of Dumbo Adventures (to-others perspective, querying what Dumbo gives to Thea)
    /// - Thea BFF: Rightholder at Dumbo, MD of Mille (from-others perspective, querying what she receives from Dumbo)
    /// </para>
    /// </remarks>
    public class GetPackages : IClassFixture<ApiFixture>
    {
        public GetPackages(ApiFixture fixture)
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
        /// Malin (MD of Dumbo) queries packages delegated from Dumbo to Thea in the to-others direction.
        /// Expects OK with the SalarySpecialCategory package.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsMalinForDumboToThea_WithToOthersScope_ReturnsOkWithPackage()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            var salaryPackage = result.Items.FirstOrDefault(p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
            Assert.NotNull(salaryPackage);
            Assert.NotEmpty(salaryPackage.Permissions);

            PermissionDto perm = salaryPackage.Permissions.First();
            Assert.Equal(TestData.DumboAdventures.Entity.Name, perm.From.Name);
            Assert.True(perm.From.Id == TestData.DumboAdventures.Id);
            Assert.Equal(TestData.Thea.Entity.Name, perm.To.Name);
            Assert.True(perm.To.Id == TestData.Thea.Id);
        }

        /// <summary>
        /// Thea queries packages she receives from Dumbo in the from-others direction.
        /// Expects OK with the same SalarySpecialCategory package.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsTheaFromDumbo_WithFromOthersScope_ReturnsOkWithPackage()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.Thea.Id}&to={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);
            Assert.Contains(result.Items, p => p.Package.Id == PackageConstants.SalarySpecialCategory.Id);
        }

        /// <summary>
        /// Malin queries packages from Dumbo to Mille Hundefrisor.
        /// No packages are assigned to this connection, so the list should be empty.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsMalinForDumboToMille_WithToOthersScope_ReturnsOkEmpty()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Empty(result.Items);
        }

        /// <summary>
        /// Thea uses to-others scope when querying from-others direction (wrong scope for direction).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsTheaFromDumbo_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.Thea.Id}&to={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others scope when querying to-others direction (wrong scope for direction).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetPackages_AsMalinForDumboToThea_WithFromOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses a write scope on the read-only GetPackages endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetPackages_WithWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
