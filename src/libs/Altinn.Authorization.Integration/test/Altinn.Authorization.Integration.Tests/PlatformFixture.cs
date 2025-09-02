using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Appsettings;
using Altinn.Authorization.Integration.Platform.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Xunit.Sdk;

namespace Altinn.Authorization.Integration.Tests;

/// <summary>
/// Provides a fixture for setting up and managing platform-related configurations and services for testing.
/// </summary>
public class PlatformFixture
{
    /// <summary>
    /// Provides the service provider used to resolve dependencies.
    /// </summary>
    internal IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// Represents the service collection where services are registered.
    /// </summary>
    internal IServiceCollection Services { get; set; } = new ServiceCollection();

    /// <summary>
    /// Holds the application configuration settings, loaded from secrets and a JSON file.
    /// </summary>
    internal IConfiguration Configuration { get; set; } = new ConfigurationBuilder()
            .AddUserSecrets<PlatformFixture>()
            .AddJsonFile("testsettings.json", false)
            .Build();

    /// <summary>
    /// Stores application-specific test settings.
    /// </summary>
    internal TestSettings Appsettings { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformFixture"/> class and configures platform integration services.
    /// </summary>
    public PlatformFixture()
    {
        Configuration.Bind(Appsettings);

        Services.AddAltinnPlatformIntegration(opts =>
        {
            opts.PlatformAccessToken.Issuer = "platform";
            opts.PlatformAccessToken.App = "test";
            opts.PlatformAccessToken.TokenSource = AltinnIntegrationOptions.TokenSource.TestTool;
            opts.PlatformAccessToken.TestTool.Environment = Appsettings.Platform.Token.TestTool.Environment;
            opts.PlatformAccessToken.TestTool.Username = Appsettings.Platform.Token.TestTool.Username;
            opts.PlatformAccessToken.TestTool.Password = Appsettings.Platform.Token.TestTool.Password;
            opts.PlatformAccessToken.TestTool.Endpoint = Appsettings.Platform.Token.TestTool.Endpoint;
        })
        .AddRegister(opts => opts.Endpoint = Appsettings.Platform.Register.Endpoint)
        .AddResourceRegistry(opts => opts.Endpoint = Appsettings.Platform.ResourceRegistry.Endpoint)
        .AddSblBridge(opts => opts.Endpoint = Appsettings.Platform.SblBridge.Endpoint);

        ServiceProvider = Services.BuildServiceProvider();
    }

    public void SkipIfMissingConfiguration<T>(string msg = "Missing configuration skipping test")
        where T : class
    {
        try
        {
            _ = ServiceProvider.GetService<IOptions<T>>().Value;
        }
        catch (OptionsValidationException)
        {
            throw SkipException.ForSkip(msg);
        }
    }

    public void SkipIfDisabled(string section)
    {
        var enabled = Configuration.GetValue<bool>($"Platform:{section}:Enabled");
        if (!enabled)
        {
            throw SkipException.ForSkip($"Integration {section} is set to disabled. Skipping tests.");
        }
    }

    /// <summary>
    /// Resolves and returns a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <returns>The resolved service instance.</returns>
    public T GetService<T>() => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Represents application-specific test settings.
    /// </summary>
    internal class TestSettings
    {
        /// <summary>
        /// Gets or sets the platform settings.
        /// </summary>
        public PlatformSettings Platform { get; set; }
    }
}
