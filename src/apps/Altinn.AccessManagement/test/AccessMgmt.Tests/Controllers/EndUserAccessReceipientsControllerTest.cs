using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Util;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccessMgmt.Tests.Controllers
{
    /// <summary>
    /// Test class for <see cref="AccessRecipientsController"></see>
    /// </summary>
    [Collection("EndUserAccessReceipientsController Tests")]
    public class EndUserAccessReceipientsControllerTest : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        private readonly string sblUserToken = PrincipalUtil.GetToken(20000012, 50002120, 3, new Guid("12029316-3FDE-4DE7-B002-384740637BC7"));

        private readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Constructor setting up factory, test client and dependencies
        /// </summary>
        /// <param name="factory">CustomWebApplicationFactory</param>
        public EndUserAccessReceipientsControllerTest(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Test case: RightsQuery returns a list of rights the To userid 20000095 have for the From party 50005545 for the jks_audi_etron_gt resource from the resource registry.
        ///            In this case:
        ///            - The From unit (50005545) has delegated the "Park" action directly to the user.
        ///            - The From unit (50005545) has delegated the "Drive" action to the party 50005545 where the user is DAGL and have keyrole privileges.
        /// Expected: GetRights returns a list of right matching expected
        /// </summary>
        [Fact]
        public async Task AuthorizeTrue()
        {
            // Arrange

            // Act
            HttpResponseMessage response = await GetTestClient(sblUserToken, true).GetAsync($"accessmanagement/api/v1/enduser/access/recipients?party=DE68AC03-BC16-4749-8358-E72CD3E553EA&from=DE68AC03-BC16-4749-8358-E72CD3E553EA&to=12029316-3FDE-4DE7-B002-384740637BC7");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("true", responseContent);
        }

        /// <summary>
        /// Test Case: Authorize returns false when PDP returns deny
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AuthorizeFalse()
        {
            // Arrange

            // Act
            HttpResponseMessage response = await GetTestClient(sblUserToken, false).GetAsync($"accessmanagement/api/v1/enduser/access/recipients?party=DE68AC03-BC16-4749-8358-E72CD3E553EA&from=DE68AC03-BC16-4749-8358-E72CD3E553EA&to=12029316-3FDE-4DE7-B002-384740637BC7");
            string responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("false", responseContent);
        }

        private HttpClient GetTestClient(string token, bool pdpPermit = true, params Action<IServiceCollection>[] actions)
        {
            HttpClient client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
                    services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
                    services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
                    services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                    services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
                    services.AddSingleton<IPartiesClient, PartiesClientMock>();
                    services.AddSingleton<IProfileClient, ProfileClientMock>();
                    services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
                    services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
                    if (pdpPermit)
                    {
                        services.AddSingleton<IPDP, PdpPermitMock>();
                    }
                    else
                    {
                        services.AddSingleton<IPDP, PdpDenyMock>();
                    }

                    services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
                    services.AddSingleton<IDelegationChangeEventQueue>(new DelegationChangeEventQueueMock());
                    services.AddSingleton<IAuthenticationClient>(new AuthenticationMock());
                    services.AddSingleton<IAccessListsAuthorizationClient>(new AccessListsAuthorizationClientMock());

                    foreach (var action in actions)
                    {
                        action(services);
                    }
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
