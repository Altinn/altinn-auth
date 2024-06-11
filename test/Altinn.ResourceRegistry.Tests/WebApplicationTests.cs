using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Net.Http;
using System.Threading;
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
    private DbFixture.OwnedDb? _db;

    private int _nextUserId = 1;

    protected IServiceProvider Services => _scope!.ServiceProvider;
    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();

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
        if (_services is IAsyncDisposable iad) await iad.DisposeAsync();
        else if (_services is IDisposable id) id.Dispose();
        if (_webApp is { } webApp) await webApp.DisposeAsync();
         
        if (_db is { } db) await db.DisposeAsync();

    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        _db = await _dbFixture.CreateDbAsync();
        _webApp = _webApplicationFixture.CreateServer(services =>
        {
            _db.ConfigureServices(services);
            services.AddSingleton<MockRegisterClient>();
            services.AddSingleton<IRegisterClient>(s => s.GetRequiredService<MockRegisterClient>());
            ConfigureServices(services);
        });

        _services = _webApp.Services;
        _scope = _services.CreateAsyncScope();
    }

    #region Utils
    protected async Task AddResource(string name)
    {
        await using var resourceCmd = DataSource.CreateCommand(/*strpsql*/"INSERT INTO resourceregistry.resources (identifier, created, serviceresourcejson) VALUES (@name, NOW(), @json);");
        var nameParam = resourceCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
        var jsonParam = resourceCmd.Parameters.Add("json", NpgsqlTypes.NpgsqlDbType.Jsonb);
        jsonParam.Value = "{}";

        nameParam.Value = name;
        await resourceCmd.ExecuteNonQueryAsync();
    }

    protected Guid GenerateUserId()
    {
        var id = Interlocked.Increment(ref _nextUserId) - 1;
        var lastGuidPart = id.ToString("D12");
        var guidString = $"00000000-0000-0000-0000-{lastGuidPart}";

        return Guid.Parse(guidString);
    }
    #endregion
}
