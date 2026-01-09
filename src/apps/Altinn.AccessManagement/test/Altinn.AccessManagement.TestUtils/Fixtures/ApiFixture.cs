using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessManagement.TestUtils.Factories;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.AccessManagement.TestUtils.Fixtures;

/// <summary>
/// Fixture used to create an in-memory test server for API integration tests.
/// Inherits from <see cref="WebApplicationFactory{Program}"/> and implements
/// <see cref="IAsyncLifetime"/> to manage lifecycle of the underlying
/// resources (for example a PostgreSQL test database).
/// </summary>
/// <remarks>
/// Callbacks registered via <see cref="ConfiureServices"/>,
/// <see cref="WithAppsettings"/>, or <see cref="WithInMemoryAppsettings"/>
/// are collected and applied when the test host is constructed. The host is
/// typically created on first use (for example when calling
/// <c>Server.CreateClient()</c> or <c>CreateClient()</c>), so registering
/// callbacks after the host has been created will have no effect.
///
/// If you require different configuration for a group of tests, create a
/// dedicated test class (or collection) that constructs and configures its
/// own <c>ApiFixture</c> instance in the class/collection constructor.
/// Avoid mutating a shared fixture from individual test methods; such
/// mutations will either be ignored (if the host is already built) or lead
/// to confusing ordering-dependent behavior.
/// </remarks>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// The test database instance returned by <see cref="EFPostgresFactory"/>.
    /// Populated during <see cref="InitializeAsync"/>.
    /// </summary>
    private PostgresDatabase _database = null!;

    /// <summary>
    /// Seed gate used to ensure seed operations run only once.
    /// </summary>
    private int _seedonce = 0;

    /// <summary>
    /// Actions that can modify the <see cref="IConfigurationBuilder"/> used
    /// when constructing the test host configuration. Useful for toggling
    /// feature flags or overriding settings per-test.
    /// </summary>
    private List<Action<IConfigurationBuilder>> ConfigurationBuilderActions { get; } = [];

    /// <summary>
    /// Actions that can modify the <see cref="IServiceCollection"/> before
    /// the test host is built. These actions are executed inside
    /// <see cref="ConfigureWebHost(IWebHostBuilder)"/>.
    /// </summary>
    private List<Action<IServiceCollection>> ConfigureServices { get; } = [];

    /// <summary>
    /// Builds an <see cref="HttpClient"/> from the test server and applies
    /// optional configuration callbacks to it.
    /// </summary>
    /// <param name="configureClient">Optional callbacks to configure the returned <see cref="HttpClient"/>.</param>
    /// <returns>The configured <see cref="HttpClient"/> instance.</returns>
    public HttpClient BuildConfiguration(params Action<HttpClient>[] configureClient)
    {
        var client = Server.CreateClient();
        foreach (var configure in configureClient)
        {
            configure(client);
        }

        return client;
    }

    /// <summary>
    /// Configures the test web host. This method sets up in-memory
    /// configuration values (including the connection strings for the
    /// per-test PostgreSQL database), replaces authentication key providers
    /// with test doubles, and applies any additional configuration or
    /// service modifications registered via helper methods.
    /// </summary>
    /// <param name="builder">The web host builder for the test server.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var appsettings = new ConfigurationBuilder()
           .AddJsonFile("appsettings.default.json", optional: true)
           .AddInMemoryCollection(new Dictionary<string, string>
           {
               ["PostgreSQLSettings:AuthorizationDbAdminPwd"] = _database.Admin.Password,
               ["PostgreSQLSettings:AuthorizationDbPwd"] = _database.User.Password,
               ["PostgreSQLSettings:AdminConnectionString"] = _database.Admin.ToString(),
               ["PostgreSQLSettings:ConnectionString"] = _database.User.ToString(),
               ["Logging:LogLevel:*"] = "Error",
           });

        foreach (var action in ConfigurationBuilderActions)
        {
            action(appsettings);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPublicSigningKeyProvider>();
            services.PostConfigure<JwtCookieOptions>(JwtCookieDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = TestTokenGenerator.SigningKey,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                    };
                });

            services.AddSingleton<IPublicSigningKeyProvider, PublicSigningKeyProviderMock>();
            services.AddSingleton<IPDP, PermitPdpMock>();
            foreach (var configure in ConfigureServices)
            {
                configure(services);
            }
        });

        builder.UseConfiguration(appsettings.Build());
    }

    /// <summary>
    /// Registers a callback that will be executed to modify the
    /// <see cref="IServiceCollection"/> used when building the test host.
    /// </summary>
    /// <param name="configureServices">Action that will receive the service collection.</param>
    /// <remarks>
    /// Register service configuration in the constructor of your xUnit test
    /// class or a collection fixture. If you need different configuration for
    /// a group of tests, construct and configure a dedicated
    /// <c>ApiFixture</c> instance in the test class (or collection) constructor
    /// so the host is created with the intended settings.
    /// </remarks>
    public void ConfiureServices(Action<IServiceCollection> configureServices)
    {
        ConfigureServices.Add(configureServices);
    }

    /// <summary>
    /// Registers a callback that will be executed to modify the
    /// <see cref="IConfigurationBuilder"/> used when building the test host.
    /// Use this when you need full control over how configuration providers
    /// are added to the test host.
    /// </summary>
    /// <param name="configure">Action that will receive the configuration builder.</param>
    /// <remarks>
    /// Apply configuration overrides from the constructor of the test class
    /// or from a collection fixture so the callbacks are registered before
    /// the host is built. Calling this method from test bodies will normally
    /// have no effect because the host has already been created.
    /// </remarks>
    public void WithAppsettings(Action<IConfigurationBuilder> configure)
    {
        ConfigurationBuilderActions.Add(configure);
    }

    /// <summary>
    /// Convenience helper to add an in-memory configuration dictionary to the
    /// test host. The provided <paramref name="configure"/> delegate receives
    /// an initially empty <see cref="Dictionary{String,String}"/> which the
    /// delegate should populate with key/value pairs to be registered as an
    /// in-memory collection configuration.
    /// </summary>
    /// <param name="configure">A delegate that populates a string-to-string dictionary
    /// containing configuration key/value pairs.</param>
    /// <remarks>
    /// - Recommended callsite: the constructor of your xUnit test class or a
    ///   collection fixture constructor. Calling this from individual test
    ///   methods can cause configuration to be applied too late or to leak
    ///   into other tests.
    /// - Order: providers are added in the order <see cref="WithAppsettings"/>
    ///   invocations were registered. If multiple providers set the same key,
    ///   the last provider added wins when the configuration is built.
    /// - Thread-safety: this helper is not synchronized — register
    ///   configuration before tests run to avoid races.
    ///
    /// Example (per-class constructor):
    /// <code>
    /// public class MyApiTests : IClassFixture<ApiFixture>
    /// {
    ///     public MyApiTests(ApiFixture fixture)
    ///     {
    ///         fixture.WithInMemoryAppsettings(dict => {
    ///             dict["FeatureManagement:MyFlag"] = "true";
    ///             dict["SomeSetting"] = "value";
    ///         });
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public void WithInMemoryAppsettings(Action<Dictionary<string, string>> configure)
    {
        var appsettings = new Dictionary<string, string>();
        WithAppsettings(builder =>
        {
            configure(appsettings);
            builder.AddInMemoryCollection(appsettings);
        });
    }

    /// <summary>
    /// Execute asynchronous queries against the application's <see cref="AppDbContext"/>.
    /// The provided delegates will run within a scoped <see cref="AppDbContext"/>,
    /// and are guaranteed to execute only once across the fixture lifetime.
    /// </summary>
    /// <param name="configureDb">One or more async delegates that receive an <see cref="AppDbContext"/>.</param>
    public async Task QueryDb(params Func<AppDbContext, Task>[] configureDb)
    {
        if (Interlocked.Increment(ref _seedonce) == 1)
        {
            var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
            using var scope = Services.CreateEFScope(audit);
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var configure in configureDb)
            {
                await configure(db);
            }
        }
    }

    /// <summary>
    /// Execute synchronous seed actions against the application's <see cref="AppDbContext"/>.
    /// Actions will run only once across the fixture lifetime.
    /// </summary>
    /// <param name="configureDb">One or more actions that receive an <see cref="AppDbContext"/>.</param>
    public void EnsureSeedOnce(params Action<AppDbContext>[] configureDb)
    {
        if (Interlocked.Increment(ref _seedonce) == 1)
        {
            var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
            using var scope = Services.CreateEFScope(audit);
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var configure in configureDb)
            {
                configure(db);
            }
        }
    }

    /// <summary>
    /// Convenience helper to enable a feature flag for the test host by adding
    /// an in-memory configuration override.
    /// </summary>
    /// <param name="featureFlag">Name of the feature flag to enable.</param>
    public void WithEnabledFeatureFlag(string featureFlag)
    {
        WithAppsettings(appsettings =>
        {
            appsettings.AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"FeatureManagement:{featureFlag}"] = "true",
            });
        });
    }

    /// <summary>
    /// Convenience helper to disable a feature flag for the test host by
    /// adding an in-memory configuration override.
    /// </summary>
    /// <param name="featureFlag">Name of the feature flag to disable.</param>
    public void WithDisabledFeatureFlag(string featureFlag)
    {
        WithAppsettings(appsettings =>
        {
            appsettings.AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"FeatureManagement:{featureFlag}"] = "false",
            });
        });
    }

    /// <summary>
    /// Initialize the fixture by creating a new per-test PostgreSQL database
    /// using <see cref="EFPostgresFactory.Create"/>. This is invoked by the
    /// test framework when the fixture is started.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        _database = await EFPostgresFactory.Create();
    }
}
