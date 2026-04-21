using System.Net;
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
/// Partial test class for ConnectionsController, focused on testing the RemovePackages
/// (DELETE /connections/accesspackages) endpoint which removes a package from a rightholder connection.
/// Tests add a package first, verify it exists, remove it, then verify it's gone.
/// Uses Kaos→Josephine (default seed Rightholder) with Jinx as the delegator
/// and Josephine as the from-others receiver.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemovePackages(Guid, Guid, Guid, Guid?, string, CancellationToken)"/>.
    /// </summary>
    public class RemovePackages : IClassFixture<ApiFixture>
    {
        public RemovePackages(ApiFixture fixture)
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

        private async Task AddPackage(Guid packageId)
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={packageId}",
                null,
                TestContext.Current.CancellationToken);

            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Add package failed: {response.StatusCode}. Body: {content}");
        }

        private async Task<bool> PackageExistsInConnection(Guid packageId)
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<PackagePermissionDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result.Items.Any(p => p.Package.Id == packageId);
        }

        /// <summary>
        /// Jinx (MD of Kaos) adds a package to Kaos→Josephine, then removes it by packageId.
        /// Verifies:
        /// - Package exists after add
        /// - DELETE returns 204 NoContent
        /// - Package is gone after removal
        /// </summary>
        [Fact]
        public async Task RemovePackages_AsJinxByPackageId_ReturnsNoContentAndRemovesPackage()
        {
            Guid packageId = PackageConstants.AccountingAndEconomicReporting.Id;
            await AddPackage(packageId);

            Assert.True(await PackageExistsInConnection(packageId), "Package should exist after add");

            // Remove by packageId (to-others direction)
            HttpClient deleteClient = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={packageId}",
                TestContext.Current.CancellationToken);

            string deleteContent = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {deleteResponse.StatusCode}. Body: {deleteContent}");

            Assert.False(await PackageExistsInConnection(packageId), "Package should be gone after removal");
        }

        /// <summary>
        /// Josephine (receiver, from-others) removes a package from the Kaos→Josephine connection by URN.
        /// Expects 204 NoContent.
        /// </summary>
        [Fact]
        public async Task RemovePackages_AsJosephineByPackageUrn_FromOthersDirection_ReturnsNoContent()
        {
            Guid packageId = PackageConstants.Customs.Id;
            await AddPackage(packageId);

            Assert.True(await PackageExistsInConnection(packageId), "Package should exist after add");

            // Josephine removes from the from-others direction using URN
            HttpClient deleteClient = CreateClient(TestData.JosephineYvonnesdottir.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(
                $"{Route}/accesspackages?party={TestData.JosephineYvonnesdottir.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&package=urn:altinn:accesspackage:toll",
                TestContext.Current.CancellationToken);

            string deleteContent = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {deleteResponse.StatusCode}. Body: {deleteContent}");

            Assert.False(await PackageExistsInConnection(packageId), "Package should be gone after removal");
        }

        /// <summary>
        /// Uses from-others read scope on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemovePackages_WithFromOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses to-others read scope (not write) on the endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemovePackages_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
