using System.Net;
using System.Security.Claims;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Common.PEP.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, covering the authorization
/// attributes on the routes: authentication, the scope requirement and the PDP resource requirement.
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Expands the placeholders used by the route tables below into the ids of the seeded test entities.
    /// A template that is empty or starts with a query string addresses the controller's own base
    /// route, and must not pick up a trailing slash.
    /// </summary>
    private static string ExpandRoute(string template)
    {
        var expanded = template
            .Replace("{party}", TestEntities.PersonMatilde.Id.ToString())
            .Replace("{estate}", TestEntities.OrganizationSolsidenSameie.Id.ToString())
            .Replace("{user}", TestEntities.PersonPaula.Id.ToString())
            .Replace("{creditor}", TestEntities.OrganizationOrsta.Id.ToString())
            .Replace("{stranger}", TestEntities.PersonHenrik.Id.ToString());

        return expanded.Length == 0 || expanded.StartsWith('?') ? Route + expanded : $"{Route}/{expanded}";
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string template) =>
        client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), ExpandRoute(template)),
            TestContext.Current.CancellationToken);

    /// <summary>
    /// Authorization tests for every route on the controller.
    /// </summary>
    /// <remarks>
    /// Authorization runs before model binding, so these tests do not need request bodies even for the
    /// routes that require one. Every request here is either rejected before the action runs or is a
    /// no-op, which is what lets the class share the read-only fixture.
    /// </remarks>
    [IntegrationTest]
    [Collection(BankruptcyReadOnlyCollection.Name)]
    public class BankruptcyAuthorization
    {
        public BankruptcyAuthorization(BankruptcyReadOnlyFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeeded();
        }

        public BankruptcyReadOnlyFixture Fixture { get; }

        /// <summary>
        /// Every route on the controller, as method + path template. Kept in one place so a new route
        /// added to the controller without a matching entry here is easy to spot.
        /// </summary>
        public static TheoryData<string, string> AllRoutes =>
            new()
            {
                { "GET", "?party={party}" },
                { "GET", "users?party={party}" },
                { "POST", "users?party={party}&user={user}" },
                { "DELETE", "users?party={party}&user={user}" },
                { "POST", "users/delete?party={party}&user={user}" },
                { "PUT", "users/administrators?party={party}&user={user}" },
                { "DELETE", "users/administrators?party={party}&user={user}" },
                { "POST", "users/administrators/delete?party={party}&user={user}" },
                { "GET", "estates?party={party}" },
                { "GET", "estates/creditors?party={party}&estate={estate}" },
                { "POST", "estates/creditors?party={party}&estate={estate}&creditor={creditor}" },
                { "DELETE", "estates/creditors?party={party}&estate={estate}&creditor={creditor}" },
                { "GET", "estates/users?party={party}&user={user}" },
                { "GET", "estates/users/packages?party={party}&estate={estate}&user={user}" },
                { "POST", "estates/users?party={party}&estate={estate}&user={user}" },
                { "DELETE", "estates/users?party={party}&estate={estate}&user={user}" },
            };

        /// <summary>
        /// No token at all: every route is rejected by authentication.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllRoutes))]
        public async Task AllRoutes_WithNoToken_Return401Unauthorized(string method, string template)
        {
            var client = Fixture.Server.CreateClient();

            var response = await SendAsync(client, method, template);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// A token carrying an unrelated scope fails the ScopeAccessRequirement on every route.
        /// </summary>
        [Theory]
        [MemberData(nameof(AllRoutes))]
        public async Task AllRoutes_WithUnrelatedScope_Return403Forbidden(string method, string template)
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            var response = await SendAsync(client, method, template);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// The bankruptcy read scope does not grant access to the write routes.
        /// </summary>
        [Theory]
        [InlineData("POST", "users?party={party}&user={user}")]
        [InlineData("DELETE", "users?party={party}&user={user}")]
        [InlineData("PUT", "users/administrators?party={party}&user={user}")]
        [InlineData("DELETE", "users/administrators?party={party}&user={user}")]
        [InlineData("POST", "estates/creditors?party={party}&estate={estate}&creditor={creditor}")]
        [InlineData("DELETE", "estates/creditors?party={party}&estate={estate}&creditor={creditor}")]
        [InlineData("POST", "estates/users?party={party}&estate={estate}&user={user}")]
        [InlineData("DELETE", "estates/users?party={party}&estate={estate}&user={user}")]
        public async Task WriteRoutes_WithReadScopeOnly_Return403Forbidden(string method, string template)
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await SendAsync(client, method, template);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// The dedicated bankruptcy read scope is accepted on a read route without the portal scope.
        /// </summary>
        [Fact]
        public async Task ReadRoute_WithDedicatedReadScope_Returns200()
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_READ);

            var response = await SendAsync(client, "GET", "estates?party={party}");

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");
        }

        /// <summary>
        /// The dedicated bankruptcy write scope is accepted on a write route without the portal scope.
        /// The target is <see cref="TestEntities.PersonHenrik"/>, who is not an agent, so the revoke is
        /// a no-op and this asserts the authorization layer only.
        /// </summary>
        [Fact]
        public async Task WriteRoute_WithDedicatedWriteScope_Returns204()
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_ENDUSER_BANKRUPTCYDELEGATION_WRITE);

            var response = await SendAsync(client, "DELETE", "users?party={party}&user={stranger}");

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.NoContent, $"Expected NoContent but got {response.StatusCode}. Response body: {content}");
        }

        /// <summary>
        /// The resource requirement needs a valid party in the query string. A missing or malformed
        /// party fails authorization before the action runs, which surfaces as 403 rather than 400.
        /// </summary>
        [Theory]
        [InlineData("estates")]
        [InlineData("estates?party=")]
        [InlineData("estates?party=not-a-guid")]
        [InlineData("")]
        [InlineData("?party=not-a-guid")]
        public async Task ReadRoute_WithMissingOrMalformedParty_Returns403Forbidden(string template)
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await SendAsync(client, "GET", template);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// Authorization tests where the PDP denies access to the <c>altinn_bankruptcy_estate_admin</c>
    /// resource, which the default <see cref="PermitPdpMock"/> always permits.
    /// </summary>
    [IntegrationTest]
    public class BankruptcyAuthorizationPdpDenied : IClassFixture<ApiFixture>
    {
        public BankruptcyAuthorizationPdpDenied(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPDP, DenyBankruptcyEstateAdminPdpMock>();
            });
        }

        public ApiFixture Fixture { get; }

        /// <summary>
        /// A caller with a valid token and the right scope is still rejected when the PDP denies the
        /// bankruptcy resource.
        /// </summary>
        [Theory]
        [MemberData(nameof(BankruptcyAuthorization.AllRoutes), MemberType = typeof(BankruptcyAuthorization))]
        public async Task AllRoutes_WhenPdpDeniesResource_Return403Forbidden(string method, string template)
        {
            var client = CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await SendAsync(client, method, template);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
