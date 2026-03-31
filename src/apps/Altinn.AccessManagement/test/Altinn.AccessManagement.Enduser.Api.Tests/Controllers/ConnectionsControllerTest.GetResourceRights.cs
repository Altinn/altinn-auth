using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
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
/// Partial test class for ConnectionsController, focused on testing the GetResourceRights endpoint which returns direct and indirect rights for a specific resource between two parties. The tests cover both to-others and from-others query directions, verifying correct scope requirements and response content based on seeded data and actor perspectives.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetResourceRights(Guid, Guid, Guid, string, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - ResourceType "Test"
    /// - Resource "Skattemelding" (app_skd_skattemelding)
    /// - Assignment: Dumbo Adventures → Mille Hundefrisør (Rightholder)
    /// - AssignmentResource linking Skattemelding to the assignment above
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (views from Dumbo's perspective)
    /// - Thea: managing director of Mille Hundefrisør (views from Mille's perspective)
    /// </para>
    /// <para>
    /// The tests verify that the endpoint returns direct and indirect rights for a specific resource
    /// between two parties, and that the correct bidirectional read scope is required depending on
    /// the direction of the query (to-others vs from-others). Mismatched scopes result in HTTP 403 Forbidden.
    /// </para>
    /// </remarks>
    public class GetResourceRights : IClassFixture<ApiFixture>
    {
        public GetResourceRights(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerConnections);
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
                services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderFromDumboToMille = new Assignment()
                {
                    FromId = TestData.DumboAdventures.Id,
                    ToId = TestData.MilleHundefrisor.Id,
                    RoleId = RoleConstants.Rightholder
                };

                db.Assignments.Add(rightholderFromDumboToMille);
                db.SaveChanges();

                db.AssignmentResources.Add(new AssignmentResource()
                {
                    AssignmentId = rightholderFromDumboToMille.Id,
                    ResourceId = TestData.SiriusSkattemelding.Id,
                    PolicyPath = "sirius-skattemelding-v1/50083510/p50155461/delegationpolicy.xml"
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
        /// Malin (MD of Dumbo) queries resource rights for Skattemelding delegated to Mille in the to-others direction.
        /// Expects OK with a valid response containing the resource.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsMalinForDumboToMille_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.IndirectRights);
            Assert.NotEmpty(resourceRightsDto.DirectRights);
            Assert.Equal(9, resourceRightsDto.DirectRights.Count); // 9 inherited rights from Dumbo to Mille via the Rightholder role
            foreach (var right in resourceRightsDto.DirectRights)
            {
                // All rights to Mille should be direct via Dumbo's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.MilleHundefrisor.Entity.Name);
                Assert.True(permission.To.Id == TestData.MilleHundefrisor.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Thea (MD of Mille) queries resource rights for Skattemelding received from Dumbo in the from-others direction.
        /// Expects OK with a valid response containing the resource.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsTheaForMilleFromDumbo_WithFromOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);

            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.IndirectRights);
            Assert.NotEmpty(resourceRightsDto.DirectRights);
            Assert.Equal(9, resourceRightsDto.DirectRights.Count); // 9 inherited rights from Dumbo to Mille via the Rightholder role
            foreach (var right in resourceRightsDto.DirectRights)
            {
                // All rights to Mille should be direct via Dumbo's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.MilleHundefrisor.Entity.Name);
                Assert.True(permission.To.Id == TestData.MilleHundefrisor.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.Direct), $"Expected Direct but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Malin (MD of Dumbo) queries resource rights for Skattemelding delegated to Mille in the to-others direction.
        /// Expects OK with a valid response containing the resource.
        /// 
        /// Mille gets rights indirectly since Thea is MD of Mille. (nøkkelrollearv)
        /// She should get the same rights as query for Mille, bit indirect not direct.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_AsMalinForDumboToThea_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Thea should be indirect via Mille's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Thea.Entity.Name);
                Assert.True(permission.To.Id == TestData.Thea.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        [Fact]
        public async Task GetResourceRights_AsMalinForDumboToMilena_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Milena.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Milena should be indirect via Mille's Rightholder role Chair of board, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Milena.Entity.Name);
                Assert.True(permission.To.Id == TestData.Milena.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        [Fact]
        public async Task GetResourceRights_AsTheaForDumboToThea_WithToOthersScope_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.Thea.Id}&from={TestData.DumboAdventures.Id}&to={TestData.Thea.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            ExternalResourceRightDto resourceRightsDto = await response.Content.ReadFromJsonAsync<ExternalResourceRightDto>(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}.");
            Assert.NotNull(resourceRightsDto);
            Assert.NotNull(resourceRightsDto.Resource);
            Assert.Empty(resourceRightsDto.DirectRights);
            Assert.NotEmpty(resourceRightsDto.IndirectRights);
            Assert.Equal(9, resourceRightsDto.IndirectRights.Count); // 9 inherited rights from Mille's Rightholder role
            foreach (var right in resourceRightsDto.IndirectRights)
            {
                // All rights to Thea should be indirect via Mille's Rightholder role, so we expect the same permission and reason for all rights
                Assert.True(right.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {right.Reason.Flag}.");
                Assert.Single(right.Permissions);
                PermissionDto permission = right.Permissions[0];
                Assert.Equal(permission.To.Name, TestData.Thea.Entity.Name);
                Assert.True(permission.To.Id == TestData.Thea.Id);
                Assert.True(permission.From.Name == TestData.DumboAdventures.Entity.Name);
                Assert.True(permission.From.Id == TestData.DumboAdventures.Id);
                Assert.True(permission.Reason.Flag.Equals(AccessReasonFlag.KeyRole), $"Expected KeyRole but got {permission.Reason.Flag}.");
                Assert.True(permission.Role.Id == RoleConstants.Rightholder, $"Expected Rightholder role but got {permission.Role.Id}.");
                Assert.Null(permission.Via);
            }

            Assert.Equal("app_skd_sirius-skattemelding-v1", resourceRightsDto.Resource.RefId);
        }

        /// <summary>
        /// Malin uses from-others read scope for a to-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_ToOthersDirection_WithFromOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Thea uses to-others read scope for a from-others direction query.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_FromOthersDirection_WithToOthersScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.Thea.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.MilleHundefrisor.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses a write scope on the read-only GetResourceRights endpoint.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_WithWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/rights?party={TestData.DumboAdventures.Id}&from={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

    }
}
