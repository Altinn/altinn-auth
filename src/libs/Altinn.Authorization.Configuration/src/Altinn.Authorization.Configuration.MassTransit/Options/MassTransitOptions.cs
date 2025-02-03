using System.Diagnostics.CodeAnalysis;
using MassTransit;

namespace Altinn.Authorization.Configuration.MassTransit.Options;

/// <summary>
/// Provides the client configuration settings for connecting to a MassTransit bus.
/// </summary>
[ExcludeFromCodeCoverage]
public class MassTransitOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitOptions"/> class.
    /// </summary>
    public MassTransitOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitOptions"/> class.
    /// </summary>
    /// <param name="configureOptions">Configures options</param>
    public MassTransitOptions(Action<MassTransitOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Allows you to further configure Mass Transit
    /// </summary>
    public Action<IBusRegistrationConfigurator> Configure { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the bus health check is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableMetrics { get; set; }

    /// <summary>
    /// Gets or sets the transport to use for the bus.
    /// </summary>
    public MassTransitTransport Transport { get; set; }

    /// <summary>
    /// Gets or sets the Azure Service Bus specific settings.
    /// </summary>
    public MassTransitAzureServiceBusSettings AzureServiceBusTransport { get; set; }
}

/// <summary>
/// Provides the client configuration settings for connecting to an Azure Service Bus.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MassTransitAzureServiceBusSettings
{
    /// <summary>
    /// Service Bus Endpoint
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// App ClientID 
    /// </summary>
    public string ClientID { get; set; }
}

/// <summary>
/// Mass transit transports.
/// </summary>
public enum MassTransitTransport
{
    /// <summary>
    /// In memory transport. Only used for testing.
    /// </summary>
    InMemory = default,

    /// <summary>
    /// Azure Service Bus transport.
    /// </summary>
    AzureServiceBus,
}
