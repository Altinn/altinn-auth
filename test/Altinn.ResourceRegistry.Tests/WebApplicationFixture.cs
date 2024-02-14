using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class WebApplicationFixture
    : IAsyncLifetime
{
    private readonly WebApplicationFactory _factory = new();

    Task IAsyncLifetime.InitializeAsync()
    {
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    public WebApplicationFactory<Program> CreateServer(Action<IServiceCollection>? configureServices = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            if (configureServices is not null)
            {
                builder.ConfigureTestServices(configureServices);
            }
        });
    }

    private class WebApplicationFactory : WebApplicationFactory<Program>
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
                var timeProvider = new AdvanceableTimeProvider();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
                services.AddSingleton<TimeProvider>(timeProvider);
                services.AddSingleton<AdvanceableTimeProvider>(timeProvider);
            });

            base.ConfigureWebHost(builder);
        }
    }
}
