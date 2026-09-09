using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Altinn.AccessMgmt.Core.Telemetry;

/// <summary>
/// Telemetry configuration for Core.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CoreTelemetry
{
    internal const string SourceName = "Altinn.AccessMgmt.Core";

    /// <summary>
    /// Used as source for distributed tracing activities.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(SourceName);

    /// <summary>
    /// Meter for pipeline metrics.
    /// </summary>
    internal static readonly Meter Meter = new(SourceName);

    internal static readonly Counter<long> HostedServicesFailures =
        Meter.CreateCounter<long>("hostedservices.failures", unit: "runs", description: "Number of hosted services failures");

    internal static readonly Counter<long> HostedServicesSuccess =
        Meter.CreateCounter<long>("hostedservices.success", unit: "runs", description: "Number of hosted services success");

    internal static readonly Gauge<int> HostedServicesOk =
        Meter.CreateGauge<int>("hostedservices.ok", unit: "1", description: "Whether all hosted services are healthy (1=healthy, 0=unhealthy)");
}
