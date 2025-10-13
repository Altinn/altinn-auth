using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Altinn.Authorization.Host.Pipeline.Telemetry;

/// <summary>
/// Telemetry configuration for pipeline operations including distributed tracing and metrics.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class PipelineTelemetry
{
    internal const string MeterName = "Altinn.Authorization.Host.Pipeline";

    /// <summary>
    /// Used as source for distributed tracing activities.
    /// </summary>
    internal static readonly ActivitySource ActivitySource = new(MeterName);

    /// <summary>
    /// Meter for pipeline metrics.
    /// </summary>
    internal static readonly Meter Meter = new(MeterName);

    internal static void SetSequence(this Activity? source, ulong sequence)
    {
        source?.SetTag("sequence", sequence);
    }

    internal static void SetFailedAttempts(this Activity? source, uint attempt)
    {
        source?.SetTag("failures", attempt);
    }

    internal static void RecordSourceFailure(PipelineArgs args)
    {
        var tags = BuildTags(args, "source");
        PipelineFailures.Add(1, [.. tags]);
        PipelinesRunSuccessfully.Record(0, [.. tags]);
    }

    internal static void RecordSegmentFailure(PipelineArgs args)
    {
        var tags = BuildTags(args, "segment");
        PipelineFailures.Add(1, [.. tags]);
        PipelinesRunSuccessfully.Record(0, [.. tags]);
    }

    internal static void RecordSinkFailure(PipelineArgs args)
    {
        var tags = BuildTags(args, "sink");
        PipelineFailures.Add(1, [.. tags]); 
        PipelinesRunSuccessfully.Record(0, [.. tags]);
    }

    internal static void RecordSourceSuccess(PipelineArgs args)
    {
        var tags = BuildTags(args, "source");
        PipelineSuccess.Add(1, [.. tags]);
        PipelinesRunSuccessfully.Record(1, [.. tags]);
    }

    internal static void RecordSegmentSuccess(PipelineArgs args)
    {
        var tags = BuildTags(args, "segment");
        PipelineSuccess.Add(1, [.. tags]);
        PipelinesRunSuccessfully.Record(1, [.. tags]);
    }

    internal static void RecordSinkSuccess(PipelineArgs args)
    {
        var tags = BuildTags(args, "sink");
        PipelineSuccess.Add(1, [.. tags]);
        PipelinesRunSuccessfully.Record(1, [.. tags]);
    }

    internal static readonly Counter<long> PipelineFailures =
        Meter.CreateCounter<long>("pipeline.failures", "runs", "Number of pipeline stage failures");

    internal static readonly Counter<long> PipelineSuccess =
        Meter.CreateCounter<long>("pipeline.runs", "runs", "Number of pipeline executions started");

    internal static readonly Gauge<int> PipelinesRunSuccessfully =
        Meter.CreateGauge<int>("pipeline.runs_ok", unit: "1", description: "1=alive/ok, 0=failing");

    private static IEnumerable<KeyValuePair<string, object?>> BuildTags(PipelineArgs args, string stage)
    {
        yield return new("pipeline", args.Descriptor.Name);
        yield return new("stage", stage);
        yield return new("name", args.Name);
    }
}
