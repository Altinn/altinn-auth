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
/// Partial test class for ConnectionsController, focused on testing the CheckPackage
/// (GET /connections/accesspackages/delegationcheck) endpoint which checks what packages
/// the authenticated user can delegate on behalf of a party. Malin (MD of Dumbo) and Jinx (MD of Kaos)
/// check which packages they can assign to rightholders.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckPackage(Guid, IEnumerable{Guid}, IEnumerable{string}, CancellationToken)"/>.
    /// </summary>
    public class CheckPackage : IClassFixture<ApiFixture>
    {
        public CheckPackage(ApiFixture fixture)
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
        /// Malin (MD of Dumbo) checks which packages she can delegate by providing specific packageIds.
        /// Expects OK with results for the requested packages.
        /// </summary>
        [Fact]
        public async Task CheckPackage_AsMalinForDumbo_ByPackageIds_ReturnsOkWithResults()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            Guid accountingPackageId = PackageConstants.AccountingAndEconomicReporting.Id;
            Guid salaryPackageId = PackageConstants.SalarySpecialCategory.Id;

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.DumboAdventures.Id}&packageIds={accountingPackageId}&packageIds={salaryPackageId}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // Verify both requested packages are in the response
            Assert.Contains(result.Items, p => p.Package.Id == accountingPackageId);
            Assert.Contains(result.Items, p => p.Package.Id == salaryPackageId);

            // Each result should have a boolean result and a package with URN
            foreach (var item in result.Items)
            {
                Assert.NotNull(item.Package);
                Assert.NotEqual(Guid.Empty, item.Package.Id);
                Assert.False(string.IsNullOrEmpty(item.Package.Urn), "Package URN should not be empty");
            }
        }

        /// <summary>
        /// Jinx (MD of Kaos) checks packages by URN strings.
        /// Expects OK with results.
        /// </summary>
        [Fact]
        public async Task CheckPackage_AsJinxForKaos_ByPackageUrns_ReturnsOkWithResults()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.KaosMagicDesignAndArts.Id}&packages=urn:altinn:accesspackage:toll&packages=urn:altinn:accesspackage:regnskap-okonomi-rapport",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            Assert.Contains(result.Items, p => p.Package.Urn == "urn:altinn:accesspackage:toll");
            Assert.Contains(result.Items, p => p.Package.Urn == "urn:altinn:accesspackage:regnskap-okonomi-rapport");
        }

        /// <summary>
        /// Malin checks without specifying any packageIds or packages (returns all delegatable packages).
        /// Expects OK with a non-empty list.
        /// </summary>
        [Fact]
        public async Task CheckPackage_AsMalinForDumbo_NoFilter_ReturnsOkWithAllDelegatablePackages()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            var result = JsonSerializer.Deserialize<PaginatedResult<AccessPackageDto.AccessPackageDtoCheck>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEmpty(result.Items);

            // Malin as DAGL should have access to many packages
            Assert.True(result.Items.Count() >= 10, $"Expected at least 10 delegatable packages for DAGL, but got {result.Items.Count()}");
        }

        /// <summary>
        /// Uses from-others read scope on the endpoint (requires to-others write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckPackage_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses to-others read scope (not write) on the endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckPackage_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses from-others write scope on the endpoint (requires to-others write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckPackage_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages/delegationcheck?party={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
