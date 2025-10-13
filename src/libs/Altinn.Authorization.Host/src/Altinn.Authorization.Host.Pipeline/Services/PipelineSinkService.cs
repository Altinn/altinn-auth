using System.Collections.Concurrent;
using System.Diagnostics;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Host.Pipeline.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host.Pipeline.Services;

/// <summary>
/// Executes pipeline sink functions with retry logic and scoped services.
/// </summary>
internal partial class PipelineSinkService(
    ILogger<PipelineSinkService> logger,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Runs the pipeline sink, consuming messages from the inbound queue.
    /// </summary>
    public async Task Run<TIn>(
        PipelineArgs args,
        PipelineSink<TIn> func,
        BlockingCollection<PipelineMessage<TIn>> inbound)
    {
        try
        {
            await EnumerateSink(args, func, inbound);
        }
        catch (InvalidOperationException ex)
        {
            Log.InboundQueueClosed(logger, args.Descriptor.Name, args.Name, ex);
        }
    }

    private async Task EnumerateSink<TIn>(
        PipelineArgs args,
        PipelineSink<TIn> func,
        BlockingCollection<PipelineMessage<TIn>> inbound)
    {
        using var serviceScope = args.Descriptor.ServiceScope?.Invoke(serviceProvider) ?? serviceProvider.CreateScope();
        foreach (var data in inbound.GetConsumingEnumerable())
        {
            using var activity = PipelineTelemetry.ActivitySource.StartActivity($"Pipeline Sink: {args.Name}", ActivityKind.Internal, data.ActivityContext ?? default);
            activity.SetSequence(data.Sequence);

            try
            {
                var ctx = new PipelineSinkContext<TIn>()
                {
                    Lease = args.Lease,
                    Data = data.Data,
                    Services = serviceScope,
                };

                Log.SinkMessageStart(logger, args.Descriptor.Name, args.Name, data.Sequence);
                await DispatchSegment(data.Sequence, args, func, ctx);
                Log.SinkMessageCompleted(logger, args.Descriptor.Name, args.Name, data.Sequence);
            }
            catch (InvalidOperationException ex)
            {
                // Should only occur if sink fails to process the same data repeatedly
                activity?.AddException(ex);
                await data.CancellationTokenSource.CancelAsync();
                Log.SinkMessageAborted(logger, args.Descriptor.Name, args.Name, data.Sequence, ex);
                return;
            }
            catch (Exception ex)
            {
                activity?.AddException(ex);
                Log.SinkUnhandledError(logger, args.Descriptor.Name, args.Name, data.Sequence, ex);
                throw;
            }
        }
    }

    private async Task DispatchSegment<TIn>(
        ulong sequence,
        PipelineArgs args,
        PipelineSink<TIn> func,
        PipelineSinkContext<TIn> item)
    {
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await func(item);
                PipelineTelemetry.RecordSinkSuccess(args);
                return;
            }
            catch (Exception ex)
            {
                Log.SinkAttemptFailed(logger, args.Descriptor.Name, args.Name, attempt, maxAttempts, ex);

                if (attempt >= maxAttempts)
                {
                    Log.SinkRetriesExhausted(logger, args.Descriptor.Name, args.Name, maxAttempts, sequence, typeof(TIn).Name, ex);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    PipelineTelemetry.RecordSinkFailure(args);
                    throw new InvalidOperationException($"Sink failed after {maxAttempts} attempts.", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }

        throw new InvalidOperationException($"Unreachable code and sink failed after {maxAttempts} attempts.");
    }

    static partial class Log
    {
        [LoggerMessage(0, LogLevel.Error, "Inbound queue closed; stopping sink consumption for pipeline '{pipeline}' sink '{sink}'.")]
        internal static partial void InboundQueueClosed(ILogger logger, string pipeline, string sink, Exception ex);

        [LoggerMessage(1, LogLevel.Information, "Pipeline '{pipeline}' sink '{sink}' starting processing message with sequence #{sequence}.")]
        internal static partial void SinkMessageStart(ILogger logger, string pipeline, string sink, ulong sequence);

        [LoggerMessage(2, LogLevel.Information, "Pipeline '{pipeline}' sink '{sink}' successfully processed message with sequence #{sequence}.")]
        internal static partial void SinkMessageCompleted(ILogger logger, string pipeline, string sink, ulong sequence);

        [LoggerMessage(3, LogLevel.Warning, "Pipeline '{pipeline}' sink '{sink}' aborted processing message with sequence #{sequence} due to repeated failure.")]
        internal static partial void SinkMessageAborted(ILogger logger, string pipeline, string sink, ulong sequence, Exception ex);

        [LoggerMessage(4, LogLevel.Warning, "Pipeline '{pipeline}' sink '{sink}' attempt ({attempt}/{maxAttempts}) failed.")]
        internal static partial void SinkAttemptFailed(ILogger logger, string pipeline, string sink, int attempt, int maxAttempts, Exception ex);

        [LoggerMessage(5, LogLevel.Error, "Pipeline '{pipeline}' sink '{sink}' exhausted all {maxAttempts} retry attempts for message with sequence #{sequence} (type={messageType}).")]
        internal static partial void SinkRetriesExhausted(ILogger logger, string pipeline, string sink, int maxAttempts, ulong sequence, string messageType, Exception ex);

        [LoggerMessage(6, LogLevel.Error, "Unhandled exception while processing pipeline '{pipeline}' sink '{sink}' message with sequence #{sequence}.")]
        internal static partial void SinkUnhandledError(ILogger logger, string pipeline, string sink, ulong sequence, Exception ex);
    }
}

/// <summary>
/// Context provided to pipeline sink functions.
/// </summary>
/// <typeparam name="TIn">The message data type.</typeparam>
public class PipelineSinkContext<TIn>
{
    /// <summary>
    /// The message data being processed.
    /// </summary>
    public required TIn Data { get; init; }

    /// <summary>
    /// The distributed lease associated with this pipeline, if configured.
    /// </summary>
    public ILease? Lease { get; set; }

    /// <summary>
    /// The scoped service provider for resolving dependencies.
    /// </summary>
    public IServiceScope Services { get; set; }
}

/// <summary>
/// Delegate for pipeline sink functions that perform final message processing.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
public delegate Task PipelineSink<TIn>(PipelineSinkContext<TIn> context);
