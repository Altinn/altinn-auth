using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.RemoveAssignment(Guid, Guid, Guid, bool, CancellationToken)"/>
    /// verifying behavior when only Altinn2 role assignments exist (no Rightholder assignment).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre-seeded via TestData:
    /// - Assignment: HanSoloEnterprise -> BenSolo (ReporterSender / Altinn2 role)
    /// - Assignment: HanSoloEnterprise -> LukeSkyWalker (ReporterSender / Altinn2 role)
    /// </para>
    /// <para>
    /// No Rightholder assignments are seeded for these pairs in this fixture, so
    /// only the Altinn2 role assignments exist.
    /// </para>
    /// <para>
    /// Actors:
    /// - Han Solo: MD of HanSoloEnterprise (to-others perspective)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class RemoveAssignmentA2RoleOnly : IClassFixture<ApiFixture>
    {
        public RemoveAssignmentA2RoleOnly(ApiFixture fixture)
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
        /// When only an Altinn2 role assignment exists (HanSoloEnterprise -> BenSolo,
        /// ReporterSender) and no Rightholder assignment is present, attempting to remove
        /// without cascade should fail with 400 BadRequest because the A2 role is
        /// treated as a connected reference that requires cascade.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_OnlyA2RoleNoCascade_Returns400()
        {
            // Verify A2 role exists and no rightholder is present
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.BenSolo.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(a2Role);

                var rightholder = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.BenSolo.Id)
                    .Where(a => a.RoleId == RoleConstants.Rightholder.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(rightholder);
            });

            // Attempt to remove without cascade
            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.BenSolo.Id}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Body: {responseContent}");

            // Verify A2 role is still present
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.BenSolo.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(a2Role);
            });
        }

        /// <summary>
        /// When only an Altinn2 role assignment exists (HanSoloEnterprise -> LukeSkyWalker,
        /// ReporterSender) and no Rightholder assignment is present, removing with cascade=true
        /// should succeed (204 NoContent), delete the A2 role assignment, and send a
        /// <see cref="RightholderRemovedNotification"/> outbox message.
        /// Verifies that notifications are sent regardless of whether it was a rightholder
        /// or A2 role assignment that was deleted.
        /// </summary>
        [Fact]
        public async Task RemoveAssignment_OnlyA2RoleWithCascade_Returns204DeletesA2RoleAndSendsNotification()
        {
            // Verify A2 role exists and no rightholder is present
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(a2Role);

                var rightholder = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.Rightholder.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(rightholder);
            });

            // Remove with cascade
            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.DeleteAsync(
                $"{Route}?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.LukeSkyWalker.Id}&cascade=true",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Body: {responseContent}");

            // Verify A2 role is deleted
            await Fixture.QueryDb(async db =>
            {
                var a2Role = await db.Assignments
                    .Where(a => a.FromId == TestData.HanSoloEnterprise.Id)
                    .Where(a => a.ToId == TestData.LukeSkyWalker.Id)
                    .Where(a => a.RoleId == RoleConstants.ReporterSender.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.Null(a2Role);
            });

            // Verify notification outbox message was created
            string expectedRefId = $"{RightholderRemovedNotification.Handler}_{TestData.HanSoloEnterprise.Id}_{TestData.LukeSkyWalker.Id}";
            await Fixture.QueryDb(async db =>
            {
                var outbox = await db.OutboxMessages
                    .Where(m => m.RefId == expectedRefId)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
                Assert.NotNull(outbox);
            });
        }
    }
}
