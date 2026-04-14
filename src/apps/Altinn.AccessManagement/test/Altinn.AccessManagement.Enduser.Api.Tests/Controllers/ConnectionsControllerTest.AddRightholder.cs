using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests for <see cref="ConnectionsController.AddRightholder(Guid, Guid, AccessManagement.Api.Enduser.Models.PersonInput, CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mocks:
    /// - <see cref="UserProfileLookupServiceMock"/> for PersonInput-based user lookup by SSN and last name
    /// - <see cref="Altinn2RightsClientMock"/> to prevent real HTTP calls to Altinn 2 SBL Bridge
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures
    /// </para>
    /// <para>
    /// The tests verify three scenarios: adding a person with no existing connection (rejected for persons),
    /// adding an organization as rightholder via the "to" query parameter (accepted),
    /// and adding a person via PersonInput body with SSN + last name lookup (accepted).
    /// Scope enforcement tests ensure only to-others write scope is accepted.
    /// </para>
    /// </remarks>
    public class AddRightholder : IClassFixture<ApiFixture>
    {
        public AddRightholder(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfiureServices(services =>
            {
                services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
                services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            });
            Fixture.EnsureSeedOnce(db =>
            {
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
        /// Malin tries to add Josephine (person with no existing connection to Dumbo) as rightholder.
        /// Persons require an existing connection; expects 400 BadRequest with EntityNotExists validation error.
        /// </summary>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithJosephine_ReturnsProblem()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(problemDetails);
            Assert.Equal("STD-00000", problemDetails.ErrorCode.ToString());
            Assert.Single(problemDetails.Errors, e => e.ErrorCode == ValidationErrors.EntityNotExists.ErrorCode);
            Assert.Single(problemDetails.Errors, e => e.Paths.Contains("QUERY/to"));
        }

        /// <summary>
        /// Malin adds Mille Hundefrisør (organization) as rightholder via the "to" query parameter.
        /// Organizations do not require an existing connection; expects OK with AssignmentDto.
        /// </summary>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithMilleHundefrisor_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.MilleHundefrisor.Id}", null, TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            AssignmentDto result = JsonSerializer.Deserialize<AssignmentDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestData.DumboAdventures.Id, result.FromId);
            Assert.Equal(TestData.MilleHundefrisor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
        }

        /// <summary>
        /// Malin adds Bodil Farmor as rightholder via PersonInput body (SSN + last name "Farmor").
        /// The mock UserProfileLookupService resolves Bodil by SSN; expects OK with AssignmentDto.
        /// </summary>
        [Fact]
        public async Task AddRightholder_AsMalinForDumboWithBodilViaPersonInput_ReturnsOk()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            PersonInput personInput = new() { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = "Farmor" };
            StringContent content = new(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}", content, TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");
            AssignmentDto result = JsonSerializer.Deserialize<AssignmentDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(result);
            Assert.Equal(TestData.DumboAdventures.Id, result.FromId);
            Assert.Equal(TestData.BodilFarmor.Id, result.ToId);
            Assert.Equal(RoleConstants.Rightholder.Id, result.RoleId);
        }

        /// <summary>
        /// Malin uses from-others read scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddRightholder_WithReadScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_READ);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Malin uses from-others write scope on an endpoint that requires to-others write scope.
        /// Expects 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task AddRightholder_WithFromOthersWriteScope_ReturnsForbidden()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_FROMOTHERS_WRITE);

            HttpResponseMessage response = await client.PostAsync($"{Route}?party={TestData.DumboAdventures.Id}&to={TestData.JosephineYvonnesdottir.Id}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
