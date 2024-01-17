using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Tests.Mocks;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Tests
{

    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
        where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.test.json")
                    .Build());
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
            });
        }
    }
}
