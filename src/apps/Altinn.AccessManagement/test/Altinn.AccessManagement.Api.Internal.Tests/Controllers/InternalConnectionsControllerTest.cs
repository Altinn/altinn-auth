using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Internal.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Api.Internal.Tests.Controllers;

public class InternalConnectionsControllerTest
{
    public const string Route = "accessmanagement/api/v1/internal/connections";

    #region POST accessmanagement/api/v1/internal/connections/selfidentifiedusers

    /// <summary>
    /// <see cref="InternalConnectionsController.PostSelfIdentifiedUsers(Guid, Guid, CancellationToken)"/>
    /// </summary>
    public class PostSelfIdentifiedUsers : IClassFixture<ApiFixture>
    {
        public PostSelfIdentifiedUsers(ApiFixture fixture)
        {
            Fixture = fixture;
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(AuthzConstants.PLATFORM_ACCESSTOKEN_ISSUER_BFF, new ClaimsIdentity("mock"), claims => { });
            client.DefaultRequestHeaders.Add("PlatformAccessToken", token);
            return client;
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_FromSIUserToPerson_ReturnsOk()
        {
            var client = CreateClient();

            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={TestEntities.SIUserMarius.Id}&to={TestEntities.EmailUserMarius}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<AssignmentDto>(data);

            Assert.Equal(TestEntities.SIUserMarius.Id, result.FromId);
            Assert.Equal(TestEntities.EmailUserMarius.Id, result.ToId);
            Assert.Equal(RoleConstants.SelfRegisteredUser, result.RoleId);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_FromPersonToPerson_ReturnsBadRequest()
        {
            var client = CreateClient();

            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={TestEntities.PersonPaula}&to={TestEntities.PersonOrjan}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

    #endregion

    }
}
