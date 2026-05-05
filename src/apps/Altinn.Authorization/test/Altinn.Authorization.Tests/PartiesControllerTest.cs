using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Altinn.Platform.Authorization.IntegrationTests.Fixtures;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Authorization.IntegrationTests.Util;

namespace Altinn.Platform.Authorization.IntegrationTests
{
    public class PartiesControllerTest : IClassFixture<AuthorizationApiFixture>
    {
        private readonly HttpClient _client;

        public PartiesControllerTest(AuthorizationApiFixture fixture)
        {
            _client = fixture.BuildClient();
        }

        /// <summary>
        /// Test case: Get the party list for the authenticated user.
        /// Expected: Should return status code 200 OK with a non-null party list.
        /// </summary>
        [Fact]
        public async Task GetPartyList_AsAuthenticatedUser_Ok()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var partiesList = await response.Content.ReadFromJsonAsync<List<Party>>();
            Assert.NotNull(partiesList);
        }

        /// <summary>
        /// Test case: Get the party list without specifying user id
        /// Expected: Should return 404 NotFound.
        /// </summary>
        [Fact]
        public async Task GetPartyList_WithoutUserQuery_NotFound()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        /// <summary>
        /// Test case: Get the party list for a user not equal to the logged in user.
        /// Expected: Should return 403 Forbidden.
        /// </summary>
        [Fact]
        public async Task GetPartyList_NotAsAuthenticatedUser_Forbidden()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties?userid=1337");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: Validate a party in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: true
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_ValidParty_True()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a subunit party in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: true
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_ValidPartySubUnit_True()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50074838/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a party NOT in the list of the authenticated user
        /// Expected: Should return status code 200 OK with body: false
        /// </summary>
        [Fact]
        public async Task ValidateParty_AsAuthenticatedUser_NotValidParty_False()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/1337/validate?userid=20000490");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(await response.Content.ReadFromJsonAsync<bool>());
        }

        /// <summary>
        /// Test case: Validate a party in the list of a different user than the authenticated user
        /// Expected: Should return status code 403 Forbidden
        /// </summary>
        [Fact]
        public async Task ValidateParty_NotAsAuthenticatedUser_Forbidden()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate?userid=1337");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        /// <summary>
        /// Test case: Validate a party without specifying user id query param
        /// Expected: Should return status code 404 NotFound
        /// </summary>
        [Fact]
        public async Task ValidateParty_WithoutUserQuery_NotFound()
        {
            // Arrange
            string token = PrincipalUtil.GetToken(20000490, 4);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            HttpResponseMessage response = await _client.GetAsync("authorization/api/v1/parties/50002598/validate");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}
