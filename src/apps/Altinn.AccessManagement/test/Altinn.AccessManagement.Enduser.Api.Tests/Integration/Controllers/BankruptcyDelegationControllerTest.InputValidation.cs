using System.Net;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, covering the input validation
/// the three "add" routes share through <c>IInputValidation.SanitizeToInput</c>, plus the estate
/// connection gate on the creditor routes.
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Tests for the shared party/person input validation.
    /// </summary>
    /// <remarks>
    /// Reads the shared seed described on <see cref="BankruptcyReadOnlyFixture"/>. Two entities carry
    /// the meaning here: <see cref="TestEntities.PersonMargit"/> is deceased but connected to the
    /// party, so she reaches the date-of-death check instead of being rejected as unknown, and
    /// <see cref="TestEntities.PersonHenrik"/> has no connection at all. Every request in this class
    /// is rejected, so nothing is written.
    /// </remarks>
    [IntegrationTest]
    [Collection(BankruptcyReadOnlyCollection.Name)]
    public class BankruptcyInputValidation
    {
        public BankruptcyInputValidation(BankruptcyReadOnlyFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeeded();
        }

        public BankruptcyReadOnlyFixture Fixture { get; }

        /// <summary>
        /// The three routes that resolve a person or organization to add, as method, path and the name
        /// of the query parameter that carries the uuid.
        /// </summary>
        public static TheoryData<string, string, string> AddRoutes =>
            new()
            {
                { "POST", "estates/creditors?party={party}&estate={estate}", "creditor" },
                { "POST", "users?party={party}", "user" },
                { "PUT", "users/administrators?party={party}", "user" },
            };

        private HttpClient CreateAdministratorClient() =>
            CreateClient(Fixture, TestEntities.PersonMatilde.Id, AuthzConstants.SCOPE_PORTAL_ENDUSER);

        private async Task<AltinnValidationProblemDetails> SendAndAssertBadRequest(string method, string template, HttpContent content = null)
        {
            var client = CreateAdministratorClient();

            var request = new HttpRequestMessage(new HttpMethod(method), ExpandRoute(template)) { Content = content };
            var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected BadRequest but got {response.StatusCode}. Response body: {responseContent}");

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseContent, JsonOptions);
            Assert.NotNull(problem);
            return problem;
        }

        /// <summary>
        /// Neither the uuid query parameter nor a person body: the caller is told that one of the two
        /// is required.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithNeitherUuidNorPersonBody_Returns400Required(string method, string template, string toParameter)
        {
            _ = toParameter;

            var problem = await SendAndAssertBadRequest(method, template);

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.Required.ErrorCode);
        }

        /// <summary>
        /// A uuid that does not resolve to any entity.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithUnknownUuid_Returns400EntityNotExists(string method, string template, string toParameter)
        {
            var problem = await SendAndAssertBadRequest(method, $"{template}&{toParameter}={Guid.NewGuid()}");

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.EntityNotExists.ErrorCode);
        }

        /// <summary>
        /// System users are not accepted by these routes, only persons and organizations.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithSystemUser_Returns400DisallowedEntityType(string method, string template, string toParameter)
        {
            var problem = await SendAndAssertBadRequest(method, $"{template}&{toParameter}={TestEntities.SystemUserStandard.Id}");

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.DisallowedEntityType.ErrorCode);
        }

        /// <summary>
        /// A person registered as deceased is not available for delegation.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithDeceasedPerson_Returns400EntityNotExists(string method, string template, string toParameter)
        {
            var problem = await SendAndAssertBadRequest(method, $"{template}&{toParameter}={TestEntities.PersonMargit.Id}");

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.EntityNotExists.ErrorCode && e.Extensions != null && e.Extensions.ContainsKey("to") && e.Extensions["to"].ToString() == "Person not available for delegation (deceased).");
        }

        /// <summary>
        /// A person that exists but has no connection to the party cannot be added by uuid.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithUnconnectedPerson_Returns400EntityNotExists(string method, string template, string toParameter)
        {
            var problem = await SendAndAssertBadRequest(method, $"{template}&{toParameter}={TestEntities.PersonHenrik.Id}");

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.EntityNotExists.ErrorCode);
        }

        /// <summary>
        /// A person body without a last name is rejected before the profile lookup happens.
        /// </summary>
        [Theory]
        [MemberData(nameof(AddRoutes))]
        public async Task AddRoutes_WithPersonBodyMissingLastName_Returns400Required(string method, string template, string toParameter)
        {
            _ = toParameter;

            var personInput = new PersonInput { PersonIdentifier = TestData.BodilFarmor.Entity.PersonIdentifier, LastName = null };
            var content = new StringContent(JsonSerializer.Serialize(personInput), Encoding.UTF8, "application/json");

            var problem = await SendAndAssertBadRequest(method, template, content);

            Assert.Single(problem.Errors, e => e.ErrorCode == ValidationErrors.Required.ErrorCode);
        }

        /// <summary>
        /// All three creditor routes are gated on the party actually administrating the estate.
        /// </summary>
        [Theory]
        [InlineData("GET", "estates/creditors?party={party}&estate={stranger}")]
        [InlineData("POST", "estates/creditors?party={party}&estate={stranger}&creditor={creditor}")]
        [InlineData("DELETE", "estates/creditors?party={party}&estate={stranger}&creditor={creditor}")]
        public async Task CreditorRoutes_WhenEstateNotAdministratedByParty_ReturnsForbidden(string method, string template)
        {
            var client = CreateAdministratorClient();

            // The template points 'estate' at an entity the party is not EstateAdministrator for.
            var response = await client.SendAsync(
                new HttpRequestMessage(new HttpMethod(method), ExpandRoute(template)),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
