using Altinn.ResourceRegistry.TestUtils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.ResourceRegistry.Tests;

public abstract class WebApplicationTests
    : IClassFixture<DbFixture>
    , IClassFixture<WebApplicationFixture>
    , IAsyncLifetime
{
    private readonly DbFixture _dbFixture;
    private readonly WebApplicationFixture _webApplicationFixture;

    public WebApplicationTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    {
        _dbFixture = dbFixture;
        _webApplicationFixture = webApplicationFixture;
    }

    private WebApplicationFactory<Program>? _webApp;
    private IServiceProvider? _services;
    private AsyncServiceScope _scope;

    protected IServiceProvider Services => _scope!.ServiceProvider;

    protected HttpClient CreateClient()
        => _webApp!.CreateClient();

    protected virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (_scope is { } scope) await scope.DisposeAsync();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        var db = await _dbFixture.CreateDbAsync();
        _webApp = _webApplicationFixture.CreateServer(services =>
        {
            db.ConfigureServices(services);
            ConfigureServices(services);
        });

        _services = _webApp.Services;
        _scope = _services.CreateAsyncScope();
    }
}
