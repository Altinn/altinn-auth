using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Text.Json;

namespace Altinn.ResourceRegistry.Tests;

public abstract class WebApplicationTests
    : IClassFixture<DbFixture>
    , IClassFixture<WebApplicationFixture>
    , IAsyncLifetime
{
    protected const string ORG_CODE = "skd";
    protected const string ORG_NO = "974761076";

    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    protected readonly CompetentAuthority DefaultAuthority = new CompetentAuthority
    {
        Orgcode = ORG_CODE,
        Organization = ORG_NO,
    };

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

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected virtual void ConfigureTestConfiguration(IConfigurationBuilder builder)
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
        _webApp = _webApplicationFixture.CreateServer(
            configureConfiguration: config =>
            {
                _db.ConfigureConfiguration(config, "resource-registry");
                ConfigureTestConfiguration(config);
            },
            configureServices: services =>
            {
                _db.ConfigureServices(services, "resource-registry");
                services.AddSingleton<MockRegisterClient>();
                services.AddSingleton<Altinn2ServicesClientMock>();
                services.AddSingleton<IRegisterClient>(s => s.GetRequiredService<MockRegisterClient>());
                services.AddSingleton<IAltinn2Services>(r => r.GetRequiredService<Altinn2ServicesClientMock>());
                ConfigureTestServices(services);
            });

        _services = _webApp.Services;
        _scope = _services.CreateAsyncScope();
    }

    #region Utils
    protected async Task AddResource(string name, CompetentAuthority? owner = null)
    {
        owner ??= DefaultAuthority;

        await using var resourceCmd = DataSource.CreateCommand(/*strpsql*/"INSERT INTO resourceregistry.resources (identifier, created, serviceresourcejson) VALUES (@name, NOW(), @json);");
        var nameParam = resourceCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
        var jsonParam = resourceCmd.Parameters.Add("json", NpgsqlTypes.NpgsqlDbType.Jsonb);

        jsonParam.Value = 
            $$"""
            {
              "hasCompetentAuthority": {{JsonSerializer.Serialize(owner, JsonOptions)}}
            }
            """;

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
