using System.Net;
using System.Net.Http.Json;
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
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the AddAssignmentPackage
/// (POST /connections/accesspackages) endpoint which adds a package to an existing rightholder connection.
/// Uses the default seed where Kaos→Josephine has a Rightholder assignment.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.AddAssignmentPackage(Guid, Guid, Guid?, string, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (from default TestData):
    /// - Assignment: Kaos Magic Design and Arts → Josephine Yvonnesdottir (Rightholder)
    /// </para>
    /// <para>
    /// Actors:
    /// - Jinx Arcane: MD of Kaos (can add packages to Kaos's rightholder connections)
    /// </para>
    /// </remarks>
    public class AddAssignmentPackage : IClassFixture<ApiFixture>
    {
        public AddAssignmentPackage(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
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
        /// Jinx (MD of Kaos) adds AccountingAndEconomicReporting package to Kaos→Josephine connection using packageId.
        /// Expects 200 OK with the created AssignmentPackageDto.
        /// Then verifies the package appears in GetPackages.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_AsJinxForKaosToJosephine_ByPackageId_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                null,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            AssignmentPackageDto result = JsonSerializer.Deserialize<AssignmentPackageDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Equal(PackageConstants.AccountingAndEconomicReporting.Id, result.PackageId);

            // Round-trip: verify the package appears in GetPackages
            HttpClient readClient = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getResponse = await readClient.GetAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}",
                TestContext.Current.CancellationToken);

            string getContent = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var packages = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(getContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(packages.Items, p => p.Package.Id == PackageConstants.AccountingAndEconomicReporting.Id);
        }

        /// <summary>
        /// Jinx adds a package using the URN string instead of packageId.
        /// Expects 200 OK.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_AsJinxForKaosToJosephine_ByPackageUrn_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&package=urn:altinn:accesspackage:toll",
                null,
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            AssignmentPackageDto result = JsonSerializer.Deserialize<AssignmentPackageDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.PackageId);
        }

        /// <summary>
        /// Jinx tries to add a package with an invalid/non-existent package URN.
        /// Expects 400 BadRequest.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_WithInvalidPackageUrn_ReturnsBadRequest()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&package=urn:altinn:accesspackage:nonexistent-package",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses from-others read scope on a write endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses to-others read scope (not write) on the endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Jinx uses from-others write scope on the endpoint (requires to-others write).
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                null,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
