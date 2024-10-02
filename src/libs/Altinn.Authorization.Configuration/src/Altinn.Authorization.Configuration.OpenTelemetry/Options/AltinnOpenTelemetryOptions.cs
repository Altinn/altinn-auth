using Microsoft.AspNetCore.Http;

namespace Altinn.Authorization.Configuration.OpenTelemetry.Options
{
    /// <summary>
    /// Configuration options for setting up OpenTelemetry in the Altinn Authorization service.
    /// This class allows configuring various settings related to tracing, metrics, and logging 
    /// using OpenTelemetry instrumentation.
    /// </summary>
    public class AltinnOpenTelemetryOptions
    {
        /// <summary>
        /// Gets or sets the name of the service being monitored. 
        /// This is typically used to identify the service in OpenTelemetry traces.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Ges or sets OTEL Service version. It's by default set to the container app's revision
        /// </summary>
        public string ServiceVersion { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION");

        /// <summary>
        /// Ges or sets OTEL Service InstanceID. It's by default set to the container's replica name
        /// </summary>
        public string ServiceInstanceId { get; set; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");

        /// <summary>
        /// Gets or sets the connection string for the telemetry backend for application insights. 
        /// </summary>
        public string ApplicationInsightsConnectionString { get; set; }

        /// <summary>
        /// Default Sampling Ratio is 5% of successful traces gets sent
        /// </summary>
        public float SamplingRatio { get; set; } = 0.05F;

        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnOpenTelemetryOptions"/> class 
        /// and applies additional configuration through the provided delegate.
        /// </summary>
        /// <param name="configureOptions">A delegate function that configures the OpenTelemetry options.</param>
        public AltinnOpenTelemetryOptions(Action<AltinnOpenTelemetryOptions> configureOptions) => configureOptions?.Invoke(this);

        /// <summary>
        /// A collection of HTTP trace headers to include in telemetry data. 
        /// These headers are used to enhance trace information with details from incoming requests.
        /// Default trace headers include common forwarded headers such as "x-forwarded-port", 
        /// "x-forwarded-proto", "x-original-host", "x-original-url", and "x-appgw-trace-id".
        /// </summary>
        internal HashSet<string> TraceHeaders { get; } =
        [
            "x-forwarded-port",
            "x-forwarded-proto",
            "x-original-host",
            "x-original-url",
            "x-appgw-trace-id"
        ];

        /// <summary>
        /// A collection of filters that define conditions to exclude specific HTTP requests from telemetry tracking.
        /// Each filter is a function that evaluates an <see cref="HttpContext"/>.
        /// If callback filter returns true, the request should be collected. 
        /// </summary>
        internal List<Func<HttpContext, bool>> Filters { get; } =
        [
            context => !context.Request.Path.StartsWithSegments(new("/health")),
        ];

        /// <summary>
        /// Adds additional trace headers to the OpenTelemetry configuration.
        /// Trace headers are used to track requests and responses through the system. 
        /// This method converts all headers to lowercase for consistency.
        /// </summary>
        /// <param name="traceHeaders">An array of trace headers to add.</param>
        /// <returns>The current instance of <see cref="AltinnOpenTelemetryOptions"/> for method chaining.</returns>
        public AltinnOpenTelemetryOptions AddTraceHeader(params string[] traceHeaders)
        {
            foreach (var traceHeader in traceHeaders)
            {
                TraceHeaders.Add(traceHeader.ToLower());
            }

            return this;
        }

        /// <summary>
        /// Adds filters to exclude specific requests from telemetry tracking.
        /// Each filter is a function that checks an <see cref="HttpContext"/> and returns a boolean.
        /// </summary>
        /// <param name="configureFilter">One or more functions defining the filter criteria.</param>
        /// <returns>The current instance of <see cref="AltinnOpenTelemetryOptions"/> for method chaining.</returns>
        public AltinnOpenTelemetryOptions ConfigureFilter(params Func<HttpContext, bool>[] configureFilter)
        {
            Filters.AddRange(configureFilter);
            return this;
        }
    }
}