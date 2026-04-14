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
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Partial class extending <see cref="ConnectionsControllerTest"/> with integration tests
/// for the <see cref="ConnectionsController.GetAvailableUsers(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/> endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This file contains the nested <see cref="GetAvailableUsers"/> test class which validates that
/// the <c>GET accessmanagement/api/v1/enduser/connections/users</c> endpoint correctly returns
/// users who already have some connection to a given party and are eligible for new delegations.
/// </para>
/// <para>
/// The tests rely on seed data from <see cref="TestDataSeeds"/>, including the Dumbo Adventures
/// organization with Malin Emilie as managing director and Thea as a rightholder.
/// </para>
/// <para>
/// Coverage includes:
/// <list type="bullet">
///   <item><description>Verifying that an authorized caller with the to-others write scope receives a successful response containing expected parties.</description></item>
///   <item><description>Verifying that a caller with only the from-others read scope is denied access (HTTP 403 Forbidden).</description></item>
/// </list>
/// </para>
/// </remarks>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.GetAvailableUsers(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data: Uses the global <see cref="TestDataSeeds"/> which includes Dumbo Adventures
    /// with Malin Emilie as managing director and Thea as rightholder.
    /// </para>
    /// <para>
    /// The tests verify that the endpoint returns persons who already have some connection
    /// to the party and are eligible for new delegations, and that the correct write scope is required.
    /// </para>
    /// </remarks>
    public class GetAvailableUsers : IClassFixture<ApiFixture>
    {
        public GetAvailableUsers(ApiFixture fixture)
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
        /// Tests that available users for Dumbo Adventures includes Thea when authenticated as Malin Emilie (managing director).
        /// </summary>
        [Fact]
        public async Task GetAvailableUsers_AsMalinForDumbo_ContainsThea()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync($"{Route}/users?party={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            PaginatedResult<SimplifiedConnectionDto> result = JsonSerializer.Deserialize<PaginatedResult<SimplifiedConnectionDto>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.NotNull(result.Items);

            List<SimplifiedPartyDto> allParties = [.. result.Items
                .SelectMany(c => new[] { c }.Concat(c.Connections ?? []))
                .Select(c => c.Party)];

            Assert.Contains(allParties, p => p.Id == TestData.Thea.Id);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetAvailableUsers_WithReadScope_ReturnsForbidden()
        {
            var client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await client.GetAsync($"{Route}/users?party={TestData.DumboAdventures.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
