using System.Net.Http;

using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Core.Services.Interfaces;

namespace Altinn.ResourceRegistry.Tests.Utils
{
    public static class SetupUtil
    {
        public static HttpClient GetTestClient(
            CustomWebApplicationFactory<ResourceController> customFactory)
        {
            WebApplicationFactory<ResourceController> factory = customFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                    services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
                    services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                    services.AddSingleton<IPRP, PRPMock>();
                    services.AddSingleton<IAltinn2Services, Altinn2ServicesClientMock>();
                    services.AddSingleton<IApplications, ApplicationsClientMock>();
                    services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
                    services.AddSingleton<IAccessManagementClient, AccessManagementMock>();

                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }

        public static HttpClient GetTestClient(CustomWebApplicationFactory<ResourceOwnerController> customFactory)
        {
            WebApplicationFactory<ResourceOwnerController> factory = customFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }
    }
}
