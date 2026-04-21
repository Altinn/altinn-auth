using System.Collections.Concurrent;
using Altinn.AccessManagement.TestUtils.Factories;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
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
/// Callbacks registered via <see cref="ConfigureServices"/>,
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
    /// Per-key seed gate. Each key (typically the calling test class type) is
    /// added at most once, ensuring its seed actions run exactly once per
    /// fixture lifetime even when the fixture is shared via
    /// <see cref="Xunit.ICollectionFixture{TFixture}"/>.
    /// </summary>
    private readonly ConcurrentDictionary<Type, bool> _seededKeys = new();

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
    private List<Action<IServiceCollection>> _configureServicesActions { get; } = [];

    /// <summary>
    /// Builds an <see cref="HttpClient"/> from the test server and applies
    /// optional configuration callbacks to it (for example setting default
    /// request headers such as <c>Authorization</c>).
    /// </summary>
    /// <param name="configureClient">Optional callbacks to configure the returned <see cref="HttpClient"/>.</param>
    /// <returns>The configured <see cref="HttpClient"/> instance.</returns>
    /// <remarks>
    /// <strong>Call from the constructor only.</strong> Calling this method
    /// triggers the web host to be built if it has not been already, so
    /// <see cref="ConfigureServices"/> and <see cref="EnsureSeedOnce{TKey}"/>
    /// must be invoked before <see cref="BuildConfiguration"/>.
    /// </remarks>
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
            foreach (var configure in _configureServicesActions)
            {
                configure(services);
            }
        });

        builder.UseConfiguration(appsettings.Build());
    }

    /// <summary>
    /// Registers a callback that modifies the <see cref="IServiceCollection"/>
    /// used when building the test host. Use this to replace production
    /// services with test doubles (mocks, stubs, fakes).
    /// </summary>
    /// <param name="configureServices">Action that receives the service collection.</param>
    /// <remarks>
    /// <para>
    /// <strong>Call from the constructor only.</strong> By the time a test
    /// method runs the host is already built; callbacks registered inside
    /// <c>[Fact]</c> methods are silently ignored.
    /// </para>
    /// <para>
    /// Ordering constraint: <see cref="ConfigureServices"/> →
    /// <see cref="EnsureSeedOnce{TKey}"/> →
    /// <see cref="BuildConfiguration"/> must all be called before the first
    /// access to the web host (e.g. <c>Server.CreateClient()</c>).
    /// </para>
    /// <para>
    /// A test class that calls <c>ConfigureServices</c> must use its own
    /// <see cref="Xunit.IClassFixture{TFixture}"/> — it cannot safely share
    /// a fixture instance with other test classes via
    /// <see cref="Xunit.ICollectionFixture{TFixture}"/> because the DI
    /// container is sealed once the host is built.
    /// </para>
    /// </remarks>
    public void ConfigureServices(Action<IServiceCollection> configureServices)
    {
        _configureServicesActions.Add(configureServices);
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
    /// Executes asynchronous queries against the application's
    /// <see cref="AppDbContext"/>. The provided delegates each receive a
    /// scoped <see cref="AppDbContext"/> and may read or write data.
    /// </summary>
    /// <param name="configureDb">One or more async delegates that receive an <see cref="AppDbContext"/>.</param>
    /// <remarks>
    /// Unlike <see cref="EnsureSeedOnce{TKey}"/>, this method is <em>not</em>
    /// guarded — every call will open a new scope and execute the delegates.
    /// It is intended for per-test or per-query ad-hoc reads, not for seeding.
    /// </remarks>
    public async Task QueryDb(params Func<AppDbContext, Task>[] configureDb)
    {
        var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
        using var scope = Services.CreateEFScope(audit);
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var configure in configureDb)
        {
            await configure(db);
        }
    }

    /// <summary>
    /// Executes synchronous seed actions against the application's
    /// <see cref="AppDbContext"/> exactly once per <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TKey">
    /// A type that uniquely identifies this seed operation — typically the
    /// calling test class itself (e.g. <c>EnsureSeedOnce&lt;MyTest&gt;(...)</c>).
    /// Using the test-class type as the key means the seed runs once per
    /// fixture even when the fixture is shared across multiple test classes
    /// via <see cref="Xunit.ICollectionFixture{TFixture}"/>.
    /// </typeparam>
    /// <param name="configureDb">One or more actions that receive an <see cref="AppDbContext"/>.</param>
    /// <remarks>
    /// <para>
    /// <strong>Call from the constructor only.</strong> Ordering constraint:
    /// <see cref="ConfigureServices"/> → <see cref="EnsureSeedOnce{TKey}"/> →
    /// <see cref="BuildConfiguration"/> must all precede the first host access.
    /// </para>
    /// <para>
    /// Seed operations should be <em>additive only</em> (INSERT, never DELETE
    /// or UPDATE rows that other test classes may be reading). Classes that
    /// need to mutate shared rows must use their own isolated
    /// <see cref="Xunit.IClassFixture{TFixture}"/> instead of joining a
    /// shared collection.
    /// </para>
    /// </remarks>
    public void EnsureSeedOnce<TKey>(params Action<AppDbContext>[] configureDb)
    {
        if (_seededKeys.TryAdd(typeof(TKey), true))
        {
            var audit = new AuditValues(SystemEntityConstants.StaticDataIngest);
            using var scope = Services.CreateEFScope(audit);
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            foreach (var configure in configureDb)
                configure(db);
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
