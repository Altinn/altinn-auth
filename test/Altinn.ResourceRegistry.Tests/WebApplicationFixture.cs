using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Clients.Interfaces;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    public WebApplicationFactory<Program> CreateServer(
        Action<IConfigurationBuilder>? configureConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            if (configureConfiguration is not null)
            {
                var settings = new ConfigurationBuilder();
                configureConfiguration(settings);
                builder.UseConfiguration(settings.Build());
            }

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
            var settings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json")
                .Build();

            builder.UseConfiguration(settings);

            builder.ConfigureTestServices(services =>
            {
                var timeProvider = new AdvanceableTimeProvider();
                services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
                services.AddSingleton<TimeProvider>(timeProvider);
                services.AddSingleton<AdvanceableTimeProvider>(timeProvider);
                services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                services.AddSingleton<IAltinn2Services, Altinn2ServicesClientMock>();
                services.AddSingleton<IAccessManagementClient, AccessManagementMock>();
            });

            base.ConfigureWebHost(builder);
        }
    }
}
