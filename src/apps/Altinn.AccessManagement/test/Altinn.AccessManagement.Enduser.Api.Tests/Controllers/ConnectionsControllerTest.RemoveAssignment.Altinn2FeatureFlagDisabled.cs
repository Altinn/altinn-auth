using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveAssignment"/> that verify
    /// behavior when the <see cref="AccessMgmtFeatureFlags.Altinn2RoleRevoke"/> feature
    /// flag is <strong>disabled</strong>.
    /// </summary>
    /// <remarks>
    /// Uses its own <see cref="ApiFixture"/> so that the feature flag is explicitly
    /// disabled, in contrast to the sibling <see cref="RemoveAssignment"/> class
    /// which enables it.
    /// <para>
    /// Pre-seeded via TestData:
    /// - Assignment: HanSoloEnterprise -> LukeSkyWalker (ReporterSender / Altinn2 role)
    /// </para>
    /// </remarks>
    public class RemoveAssignmentAltinn2FeatureFlagDisabled : IClassFixture<ApiFixture>
    {
        public RemoveAssignmentAltinn2FeatureFlagDisabled(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            });

            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.Altinn2RoleRevoke);
            Fixture.EnsureSeedOnce<RemoveAssignmentAltinn2FeatureFlagDisabled>(db =>
            {
                // Seed a rightholder so there is an assignment to revoke
                db.Assignments.Add(new Assignment
                {
                    FromId = TestData.HanSoloEnterprise.Id,
                    ToId = TestData.LukeSkyWalker.Id,
                    RoleId = RoleConstants.Rightholder,
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
        /// When revoking a rightholder assignment (HanSoloEnterprise -> LukeSkyWalker)
        /// and the <see cref="AccessMgmtFeatureFlags.Altinn2RoleRevoke"/> feature flag is
        /// <strong>disabled</strong>, the Altinn2 ReporterSender role that LukeSkyWalker
        /// holds from HanSoloEnterprise must <strong>not</strong> be deleted.
        /// Expects 204 NoContent for the rightholder removal itself, but the Altinn2
        /// role assignment must remain intact in the database.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_WithAltinn2RolePresentAndFeatureFlagDisabled_DoesNotRevokeAltinn2Role()
        {
            // Verify Altinn2 ReporterSender role exists before removal
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(a2Role);
            });

            // Remove the rightholder assignment
            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LukeSkyWalker.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Body: {responseContent}");

            // Verify the Altinn2 ReporterSender role assignment is NOT removed
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(a2Role);
            });
        }
    }
}
