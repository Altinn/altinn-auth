using Altinn.Authorization.Configuration.MassTransit.Options;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.Authorization.Configuration.MassTransit.Extensions;

/// <summary>
/// This class contains methods to integrate Azure App Configuration for Altinn applications, allowing 
/// seamless retrieval of application settings and feature flags.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Initialize MassTransit
    /// </summary>
    /// <param name="builder">extensions</param>
    /// <param name="configureOptions">options</param>
    public static IHostApplicationBuilder UseAltinnMassTransit(this IHostApplicationBuilder builder, Action<MassTransitOptions> configureOptions)
    {
        builder.Services.Configure(configureOptions);
        builder.Services.AddMassTransit(cfg =>
        {
            var options = new MassTransitOptions(configureOptions);
            ConfigureTransport(cfg, options)();
            options.Configure(cfg);
        });

        return builder;
    }

    private static Action ConfigureTransport(IBusRegistrationConfigurator cfg, MassTransitOptions options) => options.Transport switch
    {
        MassTransitTransport.AzureServiceBus => () => cfg.UsingAzureServiceBus(ConfigureAzureServiceBus(options)),
        MassTransitTransport.InMemory => () => cfg.UsingInMemory(),
        _ => throw new ArgumentException("Invalid transport", nameof(options)),
    };

    private static Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator> ConfigureAzureServiceBus(MassTransitOptions options)
    {
        return (ctx, cfg) =>
        {
            cfg.UseServiceBusMessageScheduler();
            cfg.Host(options.AzureServiceBusTransport.Endpoint, sb =>
            {
                sb.TransportType = ServiceBusTransportType.AmqpWebSockets;
                sb.TokenCredential = new ChainedTokenCredential(
                    new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions
                    {
                        ClientId = options.AzureServiceBusTransport.ClientID,
                    }),
                    new DefaultAzureCredential()
                );
            });
        };
    }
}
