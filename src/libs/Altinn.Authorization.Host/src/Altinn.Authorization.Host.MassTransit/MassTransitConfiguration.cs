using Altinn.Authorization.Host.Identity;
using Azure.Messaging.ServiceBus;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Altinn.Authorization.Host.MassTransit;

/// <summary>
/// MassTransitConfiguration
/// </summary>
public static class MassTransitConfiguration
{
    /// <summary>
    /// ConfigureMassTransit
    /// </summary>
    /// <param name="builder">builder</param>
    /// <param name="configureOptions">configureOptions</param>
    /// <returns></returns>
    public static IHostApplicationBuilder ConfigureMassTransit(IHostApplicationBuilder builder, Action<MassTransitOptions> configureOptions = null)
    {
        builder.Services.AddMassTransit(cfg =>
        {
            var options = new MassTransitOptions();
            configureOptions.Invoke(options);
            ConfigureTransport(options, cfg);
        });

        return builder;
    }

    private static Action ConfigureTransport(MassTransitOptions options, IBusRegistrationConfigurator configurator) => options.Transport switch
    {
        MassTransitTransport.AzureServiceBus => () => ConfigureTransportAzureServiceBus(options, configurator),
        MassTransitTransport.InMemory => () => ConfigureTransportInMemory(options, configurator),
        _ => throw new InvalidOperationException($"Provided transport method is invalid"),
    };

    private static Action ConfigureTransportInMemory(MassTransitOptions options, IBusRegistrationConfigurator configurator) => () =>
    {
        configurator.UsingInMemory((ctx, cfg) =>
        {
            cfg.ConfigureEndpoints(ctx);
            options?.InMemorySettings?.Configure(ctx, cfg);
        });
    };

    private static Action ConfigureTransportAzureServiceBus(MassTransitOptions options, IBusRegistrationConfigurator configurator) => () =>
    {
        configurator.AddServiceBusMessageScheduler();
        configurator.UsingAzureServiceBus((ctx, cfg) =>
        {
            cfg.ConfigureEndpoints(ctx);
            cfg.UseServiceBusMessageScheduler();
            cfg.Host(options.AzureServiceBus.Endpoint, host =>
            {
                host.TokenCredential = AzureToken.Default;
                host.TransportType = ServiceBusTransportType.AmqpWebSockets;
            });

            options?.AzureServiceBus?.Configure(ctx, cfg);
        });
    };
}
