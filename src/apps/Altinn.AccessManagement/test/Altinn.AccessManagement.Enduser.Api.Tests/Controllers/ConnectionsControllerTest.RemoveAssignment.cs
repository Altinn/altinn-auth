using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, focused on testing the RemoveAssignment
/// (DELETE /connections) endpoint which removes a rightholder connection between two parties.
/// Tests cover basic removal, cascade behavior when packages/delegations exist,
/// removal from both delegator and receiver perspectives, and scope enforcement.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveAssignment(Guid, Guid, Guid, bool, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Dumbo Adventures -> BakerJohnsen (Rightholder) seeded locally for basic remove tests
    /// </para>
    /// <para>
    /// Pre-seeded via TestData:
    /// - Assignment: Dumbo Adventures -> Thea (Rightholder) with SalarySpecialCategory package
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: MD of Dumbo Adventures (to-others perspective)
    /// </para>
    /// </remarks>
    public class RemoveAssignment : IClassFixture<ApiFixture>
    {
        public RemoveAssignment(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            });
            Fixture.EnsureSeedOnce<RemoveAssignment>(db =>
            {
                // Create a clean rightholder for basic remove test
                var rightholderFromDumboToBaker = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.BakerJohnsen.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromDumboToBaker);
                db.SaveChanges();

                // Two separate Rightholders with packages for cascade/non-cascade tests.
                // Uses isolated entities to avoid interference with other test fixtures.

                // For non-cascade test (will NOT be deleted)
                var rightholderForNoCascade = new Assignment()
                {
                    FromId = TestData.FredriksonsFabrikk.Id,
                    ToId = TestData.SiljeHaugen.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                // For cascade test (will be deleted)
                var rightholderForCascade = new Assignment()
                {
                    FromId = TestData.FredriksonsFabrikk.Id,
                    ToId = TestData.EinarBerg.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderForNoCascade);
                db.Assignments.Add(rightholderForCascade);
                db.SaveChanges();

                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = rightholderForNoCascade.Id,
                    PackageId = PackageConstants.SalarySpecialCategory.Id,
                });

                db.AssignmentPackages.Add(new AssignmentPackage()
                {
                    AssignmentId = rightholderForCascade.Id,
                    PackageId = PackageConstants.SalarySpecialCategory.Id,
                });

                db.SaveChanges();
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
        /// Malin (MD of Dumbo) removes the rightholder connection to BakerJohnsen.
        /// BakerJohnsen has no packages or delegations on this connection, so non-cascade removal succeeds.
        /// Verifies:
        /// - DELETE returns 204 NoContent
        /// - GetConnections no longer lists BakerJohnsen as a connection from Dumbo
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_AsMalinFromDumboToBaker_ReturnsNoContent()
        {
            // Verify connection exists before removal
            HttpClient readClient = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getBefore = await readClient.GetAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getBefore.StatusCode);
            string beforeContent = await getBefore.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var beforeResult = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(beforeContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Contains(beforeResult.Items, c => c.Party.Id == TestData.BakerJohnsen.Id);

            // Remove the connection
            HttpClient deleteClient = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage deleteResponse = await deleteClient.DeleteAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.BakerJohnsen.Id}",
                TestContext.Current.CancellationToken);

            string deleteContent = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(deleteResponse.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {deleteResponse.StatusCode}. Body: {deleteContent}");

            // Verify connection is gone
            HttpClient readClient2 = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage getAfter = await readClient2.GetAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getAfter.StatusCode);
            string afterContent = await getAfter.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var afterResult = JsonSerializer.Deserialize<PaginatedResult<ConnectionDto>>(afterContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.DoesNotContain(afterResult.Items, c => c.Party.Id == TestData.BakerJohnsen.Id);
        }

        /// <summary>
        /// Silje (MD of Fredriksons Fabrikk) tries to remove the Fredriksons->SiljeHaugen Rightholder
        /// connection without cascade. SiljeHaugen has a SalarySpecialCategory package on this assignment,
        /// so non-cascade removal should fail with a validation error (400 BadRequest).
        /// Uses a dedicated relationship (not the default seed) to avoid interference from other test fixtures.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_WithPackagesNoCascade_ReturnsBadRequest()
        {
            HttpClient client = CreateClient(TestData.SiljeHaugen.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.FredriksonsFabrikk.Id}&from={TestData.FredriksonsFabrikk.Id}&to={TestData.SiljeHaugen.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Body: {responseContent}");
        }

        /// <summary>
        /// Silje removes the Fredriksons->EinarBerg connection with cascade=true.
        /// Even though EinarBerg has a SalarySpecialCategory package, cascade deletes everything.
        /// Expects 204 NoContent. Uses a separate relationship (EinarBerg) from the non-cascade test
        /// (SiljeHaugen) so test ordering doesn't matter.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_WithPackagesCascade_ReturnsNoContent()
        {
            HttpClient client = CreateClient(TestData.SiljeHaugen.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.FredriksonsFabrikk.Id}&from={TestData.FredriksonsFabrikk.Id}&to={TestData.EinarBerg.Id}&cascade=true",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Body: {responseContent}");
        }

        /// <summary>
        /// Remove a connection that doesn't exist (no Rightholder from Dumbo to SvendsenAutomobil).
        /// Service returns null which maps to 204 NoContent (idempotent).
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_NonExistentConnection_ReturnsNoContent()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.SvendsenAutomobil.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        /// <summary>
        /// Uses from-others read scope on an endpoint that requires bidirectional write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_WithFromOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.BakerJohnsen.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Uses to-others read scope (not write) on the endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_WithToOthersReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.BakerJohnsen.Id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
