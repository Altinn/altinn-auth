using Altinn.Common.Authentication.Configuration;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using AltinnCore.Authentication.JwtCookie;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public class IntegrationTestWebApplicationFactory
    : WebApplicationFactory<Program>
    , IAsyncLifetime
{
    DbFixture _dbServer = new();
    DbFixture.OwnedDb? _db;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.test.json")
                    .Build());
        });
        builder.ConfigureServices(services =>
        {
            _db!.ConfigureServices(services);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
            services.AddSingleton<IPostConfigureOptions<OidcProviderSettings>, OidcProviderPostConfigureSettingsStub>();
        });

        base.ConfigureWebHost(builder);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await ((IAsyncLifetime)_dbServer).InitializeAsync();
        _db = await _dbServer.CreateDbAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await ((IAsyncLifetime)_dbServer).DisposeAsync();
    }
}
