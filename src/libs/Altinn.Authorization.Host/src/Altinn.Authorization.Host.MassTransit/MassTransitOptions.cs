using System.Diagnostics.CodeAnalysis;
using MassTransit;

namespace Altinn.Authorization.Host.MassTransit;

/// <summary>
/// Provides the configuration settings for connecting to a MassTransit bus.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MassTransitOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the bus health check is disabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>, meaning health checks are enabled.
    /// </value>
    public bool DisableHealthChecks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry tracing is disabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>, meaning tracing is enabled.
    /// </value>
    public bool DisableTracing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OpenTelemetry metrics are disabled.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>, meaning metrics collection is enabled.
    /// </value>
    public bool DisableMetrics { get; set; }

    /// <summary>
    /// Gets or sets the transport type to be used for the MassTransit bus.
    /// </summary>
    public MassTransitTransport Transport { get; set; }

    /// <summary>
    /// Gets or sets the configuration settings for Azure Service Bus transport.
    /// </summary>
    public MassTransitAzureServiceBusSettings AzureServiceBus { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration settings for in-memory transport.
    /// </summary>
    public MassTransitInMemorySettings InMemorySettings { get; set; } = new();

    /// <summary>
    /// Configuration settings for using an in-memory transport in MassTransit.
    /// </summary>
    public class MassTransitInMemorySettings
    {
        /// <summary>
        /// Gets or sets a delegate to configure the in-memory bus.
        /// </summary>
        public Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator> Configure { get; set; }
    }

    /// <summary>
    /// Provides the configuration settings for connecting to an Azure Service Bus.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class MassTransitAzureServiceBusSettings
    {
        /// <summary>
        /// Gets or sets the endpoint URL for the Azure Service Bus.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a delegate to configure the Azure Service Bus factory.
        /// </summary>
        public Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator> Configure { get; set; }
    }
}

/// <summary>
/// Represents the available transport options for MassTransit.
/// </summary>
public enum MassTransitTransport
{
    /// <summary>
    /// In-memory transport, primarily used for testing.
    /// </summary>
    InMemory = default,

    /// <summary>
    /// Azure Service Bus transport.
    /// </summary>
    AzureServiceBus,
}
