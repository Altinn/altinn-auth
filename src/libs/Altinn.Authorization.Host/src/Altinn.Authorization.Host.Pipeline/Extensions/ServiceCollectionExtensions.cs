using Altinn.Authorization.Host.Pipeline.HostedServices;
using Altinn.Authorization.Host.Pipeline.Services;
using Altinn.Authorization.Host.Pipeline.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Altinn.Authorization.Host.Pipeline.Extensions;

/// <summary>
/// Extension methods for configuring pipelines in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds pipeline telemetry to the OpenTelemetry tracer provider.
    /// </summary>
    public static TracerProviderBuilder AddPipeline(this TracerProviderBuilder builder) =>
        builder.AddSource(PipelineTelemetry.MeterName);

    public static MeterProviderBuilder AddPipeline(this MeterProviderBuilder builder) =>
        builder.AddMeter(PipelineTelemetry.MeterName);

    /// <summary>
    /// Configures OpenTelemetry for pipeline telemetry.
    /// </summary>
    public static IServiceCollection AddPipelinesOtel(this IServiceCollection services)
    {
        services.ConfigureOpenTelemetryMeterProvider(otel => otel.AddPipeline());
        services.ConfigureOpenTelemetryTracerProvider(otel => otel.AddPipeline());
        return services;
    }

    /// <summary>
    /// Registers a pipeline group with the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDescriptor">Action to configure the pipeline group.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPipelines(
        this IServiceCollection services,
        Action<IPipelineGroup> configureDescriptor)
    {
        var descriptor = new PipelineGroup();
        configureDescriptor(descriptor);

        if (!services.Any(s => s.ServiceType == typeof(PipelineMarker)))
        {
            services.AddSingleton<PipelineSourceService>();
            services.AddSingleton<PipelineSegmentService>();
            services.AddSingleton<PipelineSinkService>();
            services.AddSingleton<PipelineMarker>();
            services.AddHostedService<PipelineHostedService>();
        }

        var registry = GetOrCreateRegistry(services);
        registry.Groups.Add(descriptor);

        return services;
    }

    private static PipelineRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPipelineRegistry));

        if (descriptor?.ImplementationInstance is PipelineRegistry existing)
        {
            return existing;
        }

        var registry = new PipelineRegistry();
        services.AddSingleton<IPipelineRegistry>(registry);
        return registry;
    }

    private sealed record PipelineMarker { }
}
