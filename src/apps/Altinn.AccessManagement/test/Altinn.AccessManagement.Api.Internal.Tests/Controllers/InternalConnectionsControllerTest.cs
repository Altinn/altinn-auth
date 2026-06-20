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
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Internal.Tests.Controllers;

public class InternalConnectionsControllerTest
{
    public const string Route = "accessmanagement/api/v1/internal/connections";

    #region POST accessmanagement/api/v1/internal/connections/selfidentifiedusers

    /// <summary>
    /// <see cref="InternalConnectionsController.PostSelfIdentifiedUsers(Guid, Guid, CancellationToken)"/>
    /// </summary>
    [IntegrationTest]
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

        private HttpClient CreateClientWithPlatformToken(string app)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(AuthzConstants.PLATFORM_ACCESSTOKEN_ISSUER_ISPLATFORM, new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("urn:altinn:app", app));
            });
            client.DefaultRequestHeaders.Add("PlatformAccessToken", token);
            return client;
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_FromSIUserToPerson_Returns200WithSelfRegisteredUserAssignment()
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
        public async Task PostSelfIdentifiedUser_FromPersonToPerson_Returns400ForInvalidPersonToPersonAssignment()
        {
            var client = CreateClient();

            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={TestEntities.PersonPaula}&to={TestEntities.PersonOrjan}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_SameGuidFromAndTo_Email_PlatformIssuerWithRegisterAppClaim_Returns200WithSelfRegisteredUserAssignment()
        {
            var client = CreateClientWithPlatformToken("register");
            
            var entityId = TestEntities.EmailUserHarryPotter.Id;
            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={entityId}&to={entityId}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<AssignmentDto>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(entityId, result.FromId);
            Assert.Equal(entityId, result.ToId);
            Assert.Equal(RoleConstants.SelfRegisteredUser, result.RoleId);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_SameGuidFromAndTo_Edu_PlatformIssuerWithRegisterAppClaim_Returns200WithSelfRegisteredUserAssignment()
        {
            var client = CreateClientWithPlatformToken("register");

            var entityId = TestEntities.EduUserHermioneGranger.Id;
            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={entityId}&to={entityId}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<AssignmentDto>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(entityId, result.FromId);
            Assert.Equal(entityId, result.ToId);
            Assert.Equal(RoleConstants.SelfRegisteredUser, result.RoleId);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_SameGuidFromAndTo_Edu_PlatformIssuerWithAuthenticationAppClaim_Returns200WithSelfRegisteredUserAssignment()
        {
            var client = CreateClientWithPlatformToken("authentication");

            var entityId = TestEntities.EduUserHermioneGranger.Id;
            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={entityId}&to={entityId}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<AssignmentDto>(data);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(entityId, result.FromId);
            Assert.Equal(entityId, result.ToId);
            Assert.Equal(RoleConstants.SelfRegisteredUser, result.RoleId);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_SameGuidFromAndTo_PlatformIssuerWithRegisterAppClaim_ReturnsProblem()
        {
            var client = CreateClientWithPlatformToken("register");

            var entityId = TestEntities.UserRonWeasley.Id;
            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={entityId}&to={entityId}", null, TestContext.Current.CancellationToken);

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<AltinnProblemDetails>(data);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errors = (JsonElement)result.Extensions.FirstOrDefault(e => e.Key == "validationErrors").Value;
            var error = errors[0];
            var errorCode = error.GetProperty("code").GetString();
            Assert.Equal("AM.VLD-00008", errorCode);
        }

        [Fact]
        public async Task PostSelfIdentifiedUser_SameGuidFromAndTo_PlatformIssuerWithNotRegisterAppClaim_Returns401ForDisallowedAppClaim()
        {
            var client = CreateClientWithPlatformToken("not-register");

            var entityId = TestEntities.UserRonWeasley.Id;
            var response = await client.PostAsync($"{Route}/selfidentifiedusers?from={entityId}&to={entityId}", null, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

    #endregion

    }
}
