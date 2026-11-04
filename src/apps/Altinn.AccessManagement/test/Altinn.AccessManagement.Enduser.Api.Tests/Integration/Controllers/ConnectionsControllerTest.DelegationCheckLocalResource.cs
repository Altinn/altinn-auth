using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.AccessList;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.ResourceRegistry.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests that the delegation check reads <c>Delegable</c> and <c>AccessListMode</c> from the
/// imported resource in the database, not from the Resource Registry.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/> and
    /// <see cref="ConnectionsController.CheckInstance(Guid, string, string, CancellationToken)"/> against
    /// resources whose imported flags differ from the Resource Registry mock data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data (this class has its own database, so the changes do not leak into other tests):
    /// - nav_sykepenger_dialog and app_skd_sirius-skattemelding-v1: <c>Delegable = false</c>
    /// - nav_sykepenger_sykmelding and app_mat_mattilsynet-baker-konditorvare: <c>AccessListMode = Enabled</c>
    /// </para>
    /// <para>
    /// The <see cref="ResourceRegistryClientMock"/> data marks all four as delegable with access lists
    /// disabled, so a denial can only come from the local resource.
    /// </para>
    /// <para>
    /// Mocks:
    /// - <see cref="PolicyRetrievalPointMock"/> for XACML policies
    /// - <see cref="IAccessListsAuthorizationClient"/> answering <see cref="AccessListAuthorizationResult.NotAuthorized"/>
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (an organization, so access lists apply)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class DelegationCheckLocalResource : IClassFixture<ApiFixture>
    {
        private const string InstanceId = "urn:altinn:instance-id:50083510/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5f";

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public DelegationCheckLocalResource(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();

                var accessLists = new Mock<IAccessListsAuthorizationClient>();
                accessLists
                    .Setup(c => c.AuthorizePartyForAccessList(It.IsAny<AccessListAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AccessListAuthorizationResponse { Result = AccessListAuthorizationResult.NotAuthorized });
                services.RemoveAll<IAccessListsAuthorizationClient>();
                services.AddSingleton(accessLists.Object);
            });
            Fixture.EnsureSeedOnce<DelegationCheckLocalResource>(db =>
            {
                foreach (var resource in db.Resources.Where(r => r.Id == TestData.NavSykepengerDialog.Id || r.Id == TestData.SiriusSkattemelding.Id))
                {
                    resource.Delegable = false;
                }

                foreach (var resource in db.Resources.Where(r => r.Id == TestData.NavSykepengerSykmelding.Id || r.Id == TestData.MattilsynetBakeryService.Id))
                {
                    resource.AccessListMode = ResourceAccessListMode.Enabled;
                }

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

        private async Task<T> GetOk<T>(string url)
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);
            HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

            T result = JsonSerializer.Deserialize<T>(body, JsonOpts);
            Assert.NotNull(result);
            return result;
        }

        /// <summary>
        /// nav_sykepenger_dialog is delegable in the Resource Registry mock but not in the local resource.
        /// Malin has role and package access to all its rights, so the local flag is the only reason to deny them.
        /// </summary>
        [Fact]
        public async Task CheckResource_WhenLocalResourceNotDelegable_Returns200WithAllRightsDeniedAsResourceNotDelegable()
        {
            ResourceCheckDto result = await GetOk<ResourceCheckDto>(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.NavSykepengerDialog.RefId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
        }

        /// <summary>
        /// nav_sykepenger_sykmelding has access lists disabled in the Resource Registry mock but enabled in the
        /// local resource. Dumbo Adventures is not on any access list, so every right is denied.
        /// </summary>
        [Fact]
        public async Task CheckResource_WhenLocalResourceAccessListModeEnabled_Returns200WithAllRightsDeniedAsAccessListValidationFail()
        {
            ResourceCheckDto result = await GetOk<ResourceCheckDto>(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.NavSykepengerSykmelding.RefId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
            Assert.All(result.Rights, r => Assert.DoesNotContain(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
        }

        /// <summary>
        /// The instance delegation check reads the same local flag: app_skd_sirius-skattemelding-v1 is not
        /// delegable locally, so every instance right is denied.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WhenLocalResourceNotDelegable_Returns200WithAllRightsDeniedAsResourceNotDelegable()
        {
            InstanceCheckDto result = await GetOk<InstanceCheckDto>(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.SiriusSkattemelding.RefId}&instance={InstanceId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.ResourceNotDelegable, r.ReasonCodes));
        }

        /// <summary>
        /// The instance delegation check applies the local access list mode: app_mat_mattilsynet-baker-konditorvare
        /// has access lists enabled locally and Dumbo Adventures is not listed, so every instance right is denied.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WhenLocalResourceAccessListModeEnabled_Returns200WithAllRightsDeniedAsAccessListValidationFail()
        {
            InstanceCheckDto result = await GetOk<InstanceCheckDto>(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource={TestData.MattilsynetBakeryService.RefId}&instance={InstanceId}");

            Assert.NotEmpty(result.Rights);
            Assert.All(result.Rights, r => Assert.False(r.Result, $"Right '{r.Right.Name}' must not be delegable"));
            Assert.All(result.Rights, r => Assert.Contains(DelegationCheckReasonCode.AccessListValidationFail, r.ReasonCodes));
        }
    }
}
