using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="Altinn.AccessManagement.Api.Enduser.Controllers.ConnectionsController.AddAssignmentPackage"/>.
    /// Verifies that concurrent requests adding different packages to the same rightholder connection
    /// both succeed — exercising the outbox-unique-constraint retry path in
    /// <c>ConnectionService.AddPackage</c> / <c>SaveChangesWithOutboxRetry</c>.
    /// </summary>
    /// <remarks>
    /// Both requests write a different <c>AssignmentPackage</c> row (no business-entity conflict),
    /// but share the same <c>AccessAddedNotification</c> outbox refId
    /// (<c>access_added_{from}_{to}</c>). When the requests actually race, one will hit the
    /// <c>uq_outboxmessage_refid_pending</c> unique constraint and must recover via the retry path;
    /// both must ultimately return 200 OK.
    /// </remarks>
    [IntegrationTest]
    public class AddAssignmentPackageConcurrent : IClassFixture<ApiFixture>
    {
        public AddAssignmentPackageConcurrent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
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
        /// Two simultaneous requests each add a different package to the same Kaos→Josephine
        /// rightholder connection. Both share the same <c>access_added_{from}_{to}</c> outbox
        /// refId, so one of them will hit the <c>uq_outboxmessage_refid_pending</c> unique
        /// constraint and must recover via <c>SaveChangesWithOutboxRetry</c>.
        /// Both requests must return 200 OK.
        /// </summary>
        [Fact]
        public async Task AddAssignmentPackage_TwoDifferentPackagesConcurrently_BothReturn200()
        {
            var clientA = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            var clientB = CreateClient(TestData.JinxArcane.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            var taskA = clientA.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.CompanyRepresentativeFormTasks.Id}",
                null,
                TestContext.Current.CancellationToken);

            var taskB = clientB.PostAsync(
                $"{Route}/accesspackages?party={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}&packageId={PackageConstants.AccountingAndEconomicReporting.Id}",
                null,
                TestContext.Current.CancellationToken);

            var responses = await Task.WhenAll(taskA, taskB);
            var (responseA, responseB) = (responses[0], responses[1]);

            var bodyA = await responseA.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var bodyB = await responseB.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.True(
                responseA.StatusCode == HttpStatusCode.OK,
                $"Request A (CompanyRepresentativeFormTasks) expected 200 OK but got {responseA.StatusCode}. Body: {bodyA}");

            Assert.True(
                responseB.StatusCode == HttpStatusCode.OK,
                $"Request B (AccountingAndEconomicReporting) expected 200 OK but got {responseB.StatusCode}. Body: {bodyB}");
        }
    }
}
