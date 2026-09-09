using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.IdPortenAuthorization;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Tests.Integration.Controllers.Bff
{
    /// <summary>
    /// Because these tests reuse hard-coded <c>requestId</c> GUIDs and share
    /// entity inserts, the class implements <see cref="IAsyncLifetime"/> and
    /// stands up a fresh <see cref="IdPortenAuthorizationApiFixture"/> (hence a fresh per-test
    /// EF database, cloned from the shared EFPostgresFactory template) for every
    /// <c>[Fact]</c> to keep per-test isolation.
    /// </summary>
    [IntegrationTest]
    public class IdPortenAuthorizationControllerTest : IClassFixture<IdPortenAuthorizationApiFixture>
    {
        private readonly HttpClient _client;

        public IdPortenAuthorizationControllerTest(IdPortenAuthorizationApiFixture fixture)
        {
            _client = fixture.CreateClient(new() { AllowAutoRedirect = false });
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>
        /// Test case: Get IdPorten authorizations
        /// Scenario: User has one IdPorten authorization
        /// Expected: Returns 200 with details
        /// </summary>
        [Fact]
        public async Task GetIdPortenAuthorization_Returns200()
        {
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            using HttpRequestMessage request = new(HttpMethod.Get, "accessmanagement/api/v1/bff/idportenauthorization");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            
            List<IdPortenAuthorization> authorizations = await response.Content.ReadFromJsonAsync<List<IdPortenAuthorization>>(cancellationToken: TestContext.Current.CancellationToken);
            
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(authorizations);
            Assert.Single(authorizations);
            Assert.Equal("DDXh0PZpGwXaUHUhdiHsj6KwqWY_McHgwCZhul5hcBtbEcswPQSIgktfp5eul5_5MgFv0G2VeDXcV25uQ2JgTqN5renlPA", authorizations[0].AuthorizationId);
        }

        /// <summary>
        /// Test case: Get IdPorten authorizations
        /// Scenario: Unknown user (no valid partyUuid) attempts to get IdPorten authorizations
        /// Expected: Returns 400 with details
        /// </summary>
        [Fact]
        public async Task GetIdPortenAuthorization_Returns400ForNonExistentUser()
        {
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse("3a10eb9f-1b80-4788-9f24-2db3c0f16684"), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            using HttpRequestMessage request = new(HttpMethod.Get, "accessmanagement/api/v1/bff/idportenauthorization");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AltinnValidationProblemDetails? problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseText, _jsonOptions);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.NotNull(problemDetails);
            Assert.Equal("AM-00043", problemDetails.ErrorCode.ToString());
        }

        /// <summary>
        /// Test case: Get IdPorten authorization when the external IdPorten API returns a non-success status.
        /// Scenario: The external API responds with 400/401/403/500 (driven by the party's SSN in the mock).
        /// Expected: The status is mapped to the corresponding AM problem and forwarded to the caller.
        /// </summary>
        [Theory]
        [InlineData("00000000-0000-0000-0005-000000002598", HttpStatusCode.BadRequest, "AM-00044")]
        [InlineData("00000000-0000-0000-0005-000000002203", HttpStatusCode.Unauthorized, "AM-00045")]
        [InlineData("00000000-0000-0000-0005-000000003899", HttpStatusCode.Forbidden, "AM-00046")]
        [InlineData("00000000-0000-0000-0005-000000002182", HttpStatusCode.InternalServerError, "AM-00047")]
        public async Task GetIdPortenAuthorizations_ExternalApiReturnsError_ForwardsMappedProblem(string partyUuid, HttpStatusCode expectedStatusCode, string expectedErrorCode)
        {
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse(partyUuid), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            using HttpRequestMessage request = new(HttpMethod.Get, "accessmanagement/api/v1/bff/idportenauthorization");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseText, _jsonOptions);

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorCode, problemDetails.ErrorCode.ToString());
        }

        /// <summary>
        /// Test case: Delete IdPorten authorization when the external IdPorten API returns a non-success status.
        /// Scenario: The external API responds with 400/401/403/500 (driven by the party's SSN in the mock).
        /// Expected: The status is mapped to the corresponding AM problem and forwarded to the caller.
        /// </summary>
        [Theory]
        [InlineData("00000000-0000-0000-0005-000000002598", HttpStatusCode.BadRequest, "AM-00044")]
        [InlineData("00000000-0000-0000-0005-000000002203", HttpStatusCode.Unauthorized, "AM-00045")]
        [InlineData("00000000-0000-0000-0005-000000003899", HttpStatusCode.Forbidden, "AM-00046")]
        [InlineData("00000000-0000-0000-0005-000000002182", HttpStatusCode.InternalServerError, "AM-00047")]
        public async Task DeleteIdPortenAuthorization_ExternalApiReturnsError_ForwardsMappedProblem(string partyUuid, HttpStatusCode expectedStatusCode, string expectedErrorCode)
        {
            string token = PrincipalUtil.GetToken(20001337, 50003899, 2, Guid.Parse(partyUuid), AuthzConstants.SCOPE_PORTAL_ENDUSER);
            using HttpRequestMessage request = new(HttpMethod.Delete, "accessmanagement/api/v1/bff/idportenauthorization?id=some-authorization-id");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
            string responseText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            AltinnValidationProblemDetails problemDetails = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(responseText, _jsonOptions);

            Assert.Equal(expectedStatusCode, response.StatusCode);
            Assert.Equal(expectedErrorCode, problemDetails.ErrorCode.ToString());
        }
    }
}
