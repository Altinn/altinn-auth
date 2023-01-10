using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Tests.Mocks;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ResourceRegistry.Controllers;
using ResourceRegistryTest.Mocks;
using System.Net.Http;

namespace ResourceRegistryTest.Utils
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
                    services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }


        public static HttpClient GetTestClient(
        CustomWebApplicationFactory<ExportController> customFactory)
        {
            WebApplicationFactory<ExportController> factory = customFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
                    services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                    services.AddSingleton <IPRP, PRPMock>();
                });
            });
            factory.Server.AllowSynchronousIO = true;
            return factory.CreateClient();
        }
    }
}
