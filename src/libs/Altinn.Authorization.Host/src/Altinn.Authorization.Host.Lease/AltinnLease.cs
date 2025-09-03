using Altinn.Authorization.Host.Identity;
using Altinn.Authorization.Host.Lease.Noop;
using Altinn.Authorization.Host.Lease.StorageAccount;
using Altinn.Authorization.Host.Lease.Telemetry;
using Altinn.Authorization.Host.Startup;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Altinn.Authorization.Host.Lease;

/// <summary>
/// Provides extension methods for registering and configuring the Altinn lease functionality in a .NET application.
/// This includes configuring either an in-memory lease provider or an Azure Storage Account-based lease provider.
/// </summary>
public static partial class AltinnLease
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AltinnLease));

    /// <summary>
    /// Adds the Altinn Lease functionality to the application, allowing the configuration of lease storage type.
    /// You can configure whether leases are stored in-memory or in an Azure Storage Account.
    /// </summary>
    /// <param name="builder">The host application builder to add the lease functionality to.</param>
    /// <param name="configureOptions">Optional action to configure the lease options (e.g., type of lease storage, Azure storage settings).</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> with the lease configuration applied.</returns>
    public static IHostApplicationBuilder AddAltinnLease(this IHostApplicationBuilder builder, Action<AltinnLeaseOptions> configureOptions = null)
    {
        var options = new AltinnLeaseOptions();
        configureOptions?.Invoke(options);

        ConfigureAltinnLease(builder.Services, options)();
        return builder;
    }

    /// <summary>
    /// Determines which configuration method to use based on the lease type and configures the services accordingly.
    /// </summary>
    /// <param name="services">The service collection to register the lease-related services.</param>
    /// <param name="options">The lease options that contain the lease type configuration.</param>
    /// <returns>An action that performs the configuration of services.</returns>
    private static Action ConfigureAltinnLease(IServiceCollection services, AltinnLeaseOptions options) => options.Type switch
    {
        AltinnLeaseType.InMemory => () => ConfigureAltinnInMemoryLease(services),
        AltinnLeaseType.AzureStorageAccount => () => ConfigureAltinnLeaseAzureStorageAccount(services, options),
        _ => throw new InvalidOperationException("Unsupported lease type."),
    };

    /// <summary>
    /// Configures services for the in-memory lease provider.
    /// This is suitable for testing or scenarios where persistence of leases is not required.
    /// </summary>
    /// <param name="services">The service collection to add the lease services to.</param>
    public static void ConfigureAltinnInMemoryLease(IServiceCollection services)
    {
        Log.AddAltinnLeaseInMemory(Logger);
        services.AddSingleton<ILeaseService, NoopLeaseService>();
    }

    /// <summary>
    /// Configures services for using an Azure Storage Account to store leases.
    /// This option enables persistence of leases across application restarts.
    /// </summary>
    /// <param name="services">The service collection to add the lease services to.</param>
    /// <param name="options">The lease configuration options, including the Azure Storage Account settings.</param>
    public static void ConfigureAltinnLeaseAzureStorageAccount(IServiceCollection services, AltinnLeaseOptions options)
    {
        Log.AddAltinnLeaseStorageAccount(Logger);
        services.ConfigureOpenTelemetryMeterProvider(provider => provider.AddMeter(LeaseTelemetry.Meter.Name));
        services.ConfigureOpenTelemetryTracerProvider(provider => provider.AddSource(LeaseTelemetry.ActivitySource.Name));
        services.AddAzureClients(builder =>
        {
            builder.UseCredential(AzureToken.Default);
            builder.AddBlobServiceClient(options.StorageAccount.BlobEndpoint)
                .WithName(AltinnLeaseOptions.StorageAccountLease.Name)
                .ConfigureOptions(options =>
                {
                    options.Retry.Mode = RetryMode.Exponential;
                    options.Retry.MaxDelay = TimeSpan.FromSeconds(3);
                    options.Retry.MaxRetries = 3;
                });
        });

        services.AddSingleton<ILeaseService, StorageAccountLeaseService>();
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "add altinn lease using in memory for persistance")]
        internal static partial void AddAltinnLeaseInMemory(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "add altinn lease using azure storage account for persistance")]
        internal static partial void AddAltinnLeaseStorageAccount(ILogger logger);
    }
}
