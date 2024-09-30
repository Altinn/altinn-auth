using System.Reflection;
using Altinn.Authorization.Configuration.AppSettings;
using Altinn.Authorization.Configuration.OpenTelemetry.Options;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Altinn.Authorization.Configuration.OpenTelemetry.Extensions;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry with Azure Monitor integration 
/// in an ASP.NET Core application using the <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Altinn's Default OpenTelemetry tracing, metrics, and logging configuration for ASP.NET Core applications. 
    /// This method configures instrumentation for HTTP requests, HTTP clients, and integrates with Azure Monitor.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> used to configure the application's services.</param>
    /// <param name="configureOptions">
    /// A delegate to configure additional options for OpenTelemetry instrumentation, such as service name,
    /// trace headers, and the Application Insights connection string.
    /// </param>
    /// <returns>The <see cref="WebApplicationBuilder"/> with OpenTelemetry services configured.</returns>
    public static IHostApplicationBuilder AddAltinnDefaultOpenTelemetry(this IHostApplicationBuilder builder, Action<AltinnOpenTelemetryOptions> configureOptions = null)
    {
        return builder.AddAltinnOpenTelemetry(opts =>
        {
            var altinnAppSettings = new AltinnAppSettings(builder.Configuration);
            opts.ApplicationInsightsConnectionString = altinnAppSettings.ApplicationInsights.ConnectionString;
            configureOptions?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds OpenTelemetry tracing, metrics, and logging to the ASP.NET Core application. 
    /// This method configures instrumentation for HTTP requests, HTTP clients, and integrates with Azure Monitor.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> used to configure the application's services.</param>
    /// <param name="configureOptions">
    /// A delegate to configure additional options for OpenTelemetry instrumentation, such as service name,
    /// trace headers, and the Application Insights connection string.
    /// </param>
    /// <returns>The <see cref="WebApplicationBuilder"/> with OpenTelemetry services configured.</returns>
    public static IHostApplicationBuilder AddAltinnOpenTelemetry(this IHostApplicationBuilder builder, Action<AltinnOpenTelemetryOptions> configureOptions = null)
    {
        var config = new AltinnOpenTelemetryOptions(configureOptions);
        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(config.ServiceName, config.ServiceName, config.ServiceVersion, string.IsNullOrEmpty(config.ServiceInstanceId), config.ServiceInstanceId);
            })
            .WithLogging()
            .WithTracing(tracing =>
                tracing.AddAspNetCoreInstrumentation(instrumentation =>
                {
                    instrumentation.Filter = (context) => config.Filters.All(filter => filter(context));
                    instrumentation.EnrichWithHttpRequest = (activity, request) =>
                    {
                        foreach (var header in config.TraceHeaders)
                        {
                            if (request.Headers.TryGetValue(header, out var value))
                            {
                                activity.AddTag(header, value);
                            }
                        }
                    };
                })
            )
            .WithMetrics(options =>
                options
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
            );

        if (!builder.Environment.IsDevelopment())
        {
            otel.UseAzureMonitor(options =>
            {
                options.SamplingRatio = config.SamplingRatio;
                options.ConnectionString = config.ApplicationInsightsConnectionString;
            });
        }

        return builder;
    }
}
