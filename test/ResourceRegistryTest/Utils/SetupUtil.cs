using Altinn.ResourceRegistry;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Persistence;
using Altinn.ResourceRgistryTest.Tests.Mocks.Authentication;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ResourceRegistry.Controllers;
using ResourceRegistryTest.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
