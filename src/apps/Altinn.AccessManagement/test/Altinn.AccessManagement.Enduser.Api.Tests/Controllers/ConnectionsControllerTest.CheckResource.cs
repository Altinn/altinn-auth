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
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Assignment: Nordis AS → Verdiq AS (Rightholder)
    /// </para>
    /// <para>
    /// Pre-seeded via <see cref="TestDataSeeds"/>:
    /// - Resource "Dialogs for sickness benefits" (nav_sykepenger_dialog)
    /// - Resource "Omsetningsoppgave for alkohol" (app_dihe_omsetningsoppgave-for-alkohol)
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="ResourceRegistryClientMock"/> for resource registry policy lookups
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policy evaluation
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (has both role and package access)
    /// - Thea: rightholder of Dumbo Adventures (has package access only, no role access)
    /// </para>
    /// <para>
    /// The tests verify delegation check results including full access (roles + packages),
    /// package-only access, partial access (some rights granted, some denied), and scope enforcement.
    /// </para>
    /// </remarks>
    public class CheckResource : IClassFixture<ApiFixture>
    {
        public CheckResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.ResourceDelegationEF);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                db.Assignments.Add(rightholderFromNordisToVerdiq);
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
        /// Malin (MD of Dumbo) checks nav_sykepenger_dialog. Has full access via both roles and packages.
        /// Expects OK with 3 rights (read, access, subscribe) all granted with RoleAccess and PackageAccess.
        /// </summary>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_RolesAndPackages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Thea (rightholder of Dumbo) checks nav_sykepenger_dialog
        /// Expects OK with 3 rights all granted with PackageAccess but not RoleAccess.
        /// </summary>
        [Fact]
        public async Task CheckResource_NavSykemeldingDialog_FullAccess_Packages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=nav_sykepenger_dialog", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.DoesNotContain(result.Rights, r => r.Result == false);
            Assert.Equal(3, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto accessRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("access", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(accessRight);
            Assert.True(accessRight.Result, "The 'access' right should have Result = true");
            Assert.NotEmpty(accessRight.ReasonCodes);
            Assert.DoesNotContain(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(accessRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto subscribeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("subscribe", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(subscribeRight);
            Assert.True(subscribeRight.Result, "The 'subscribe' right should have Result = true");
            Assert.NotEmpty(subscribeRight.ReasonCodes);
            Assert.DoesNotContain(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.Contains(subscribeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));
        }

        /// <summary>
        /// Malin (MD of Dumbo) checks app_dihe_omsetningsoppgave-for-alkohol. Has partial access: read and write
        /// granted via roles, but sign denied with MissingRoleAccess and MissingDelegationAccess.
        /// Expects OK with 17 rights (mix of granted and denied).
        /// </summary>
        [Fact]
        public async Task CheckResource_DiheOmsetningsoppgave_PartialAccess_RolesAndPackages_ReturnsOK()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_dihe_omsetningsoppgave-for-alkohol", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.NotNull(result.Rights);
            Assert.Contains(result.Rights, r => r.Result == false);
            Assert.Contains(result.Rights, r => r.Result == true);
            Assert.Equal(17, result.Rights.Count());

            RightCheckDto readRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("read", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(readRight);
            Assert.True(readRight.Result, "The 'read' right should have Result = true");
            Assert.NotEmpty(readRight.ReasonCodes);
            Assert.Contains(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(readRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto writeRight = result.Rights.FirstOrDefault(r => r.Right.Name.Equals("Write (Task_1)", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(writeRight);
            Assert.True(writeRight.Result, "The 'Write' right should have Result = true");
            Assert.NotEmpty(writeRight.ReasonCodes);
            Assert.Contains(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.RoleAccess));
            Assert.DoesNotContain(writeRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.PackageAccess));

            RightCheckDto signRight = result.Rights.FirstOrDefault(r => r.Right.Name.Contains("sign", StringComparison.InvariantCultureIgnoreCase));
            Assert.NotNull(signRight);
            Assert.False(signRight.Result, "The 'sign' right should have Result = false");
            Assert.NotEmpty(signRight.ReasonCodes);
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingRoleAccess));
            Assert.Contains(signRight.ReasonCodes, r => r.Equals(DelegationCheckReasonCode.MissingDelegationAccess));
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithReadScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);
            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithFromOthersWriteScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);
            var response = await client.GetAsync($"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=test-delegation-check-resource", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
