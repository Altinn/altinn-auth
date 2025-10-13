using System.Collections.Concurrent;
using System.Diagnostics;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Host.Pipeline.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host.Pipeline.Services;

/// <summary>
/// Executes pipeline segment transformations with retry logic.
/// </summary>
internal partial class PipelineSegmentService(ILogger<PipelineSegmentService> logger, IServiceProvider serviceProvider)
{
    /// <summary>
    /// Runs the pipeline segment, transforming messages from inbound to outbound queue.
    /// </summary>
    public async Task Run<TIn, TOut>(
        PipelineArgs args,
        PipelineSegment<TIn, TOut> func,
        BlockingCollection<PipelineMessage<TIn>> inbound,
        BlockingCollection<PipelineMessage<TOut>> outbound)
    {
        try
        {
            await EnumerateSegment(args, inbound, outbound, func);
        }
        catch (InvalidOperationException ex)
        {
            // Should only occur if inbound is closed. However, this shouldn't be throwed as we use the enumerator of inbound queue.
            Log.InboundQueueClosed(logger, args.Descriptor.Name, args.Name, ex);
        }
        finally
        {
            outbound.CompleteAdding();
        }
    }

    private async Task EnumerateSegment<TIn, TOut>(
        PipelineArgs args,
        BlockingCollection<PipelineMessage<TIn>> inbound,
        BlockingCollection<PipelineMessage<TOut>> outbound,
        PipelineSegment<TIn, TOut> func)
    {
        using var serviceScope = args.Descriptor.ServiceScope?.Invoke(serviceProvider) ?? serviceProvider.CreateScope();
        foreach (var data in inbound.GetConsumingEnumerable())
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            using var activity = PipelineTelemetry.ActivitySource.StartActivity($"Pipeline Segment: {args.Name}", ActivityKind.Internal, data.ActivityContext ?? default);
            activity.SetSequence(data.Sequence);
            try
            {
                var ctx = new PipelineSegmentContext<TIn>()
                {
                    Data = data.Data,
                    Lease = args.Lease,
                    Services = serviceScope
                };

                Log.SegmentMessageStart(logger, args.Descriptor.Name, args.Name, data.Sequence);
                var result = await DispatchSegment(data.Sequence, args, func, ctx);
                Log.SegmentMessageCompleted(logger, args.Descriptor.Name, args.Name, data.Sequence);
                outbound.Add(new(result, data.ActivityContext, data.CancellationTokenSource)
                {
                    Sequence = data.Sequence,
                });
                Log.SegmentMessageSent(logger, args.Descriptor.Name, args.Name, data.Sequence);
            }
            catch (InvalidOperationException ex)
            {
                // Should only occur if segment fails to process the same data repeatedly.
                activity?.AddException(ex);
                await data.CancellationTokenSource.CancelAsync();
                Log.SegmentMessageAborted(logger, args.Descriptor.Name, args.Name, data.Sequence, ex);
                return;
            }
            catch (Exception ex)
            {
                Log.SegmentUnhandledError(logger, args.Descriptor.Name, args.Name, data.Sequence, ex);
                throw;
            }
        }
    }

    private async Task<TOut> DispatchSegment<TIn, TOut>(
        ulong sequence,
        PipelineArgs args,
        PipelineSegment<TIn, TOut> func,
        PipelineSegmentContext<TIn> item)
    {
        const uint maxAttempts = 3;
        for (uint attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var result = await func(item);
                PipelineTelemetry.RecordSegmentSuccess(args);
                return result;
            }
            catch (Exception ex)
            {
                Log.SegmentAttemptFailed(logger, args.Descriptor.Name, args.Name, attempt, maxAttempts, ex);

                if (attempt >= maxAttempts)
                {
                    Log.SegmentRetriesExhausted(logger, args.Descriptor.Name, args.Name, maxAttempts, sequence, ex);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    PipelineTelemetry.RecordSegmentFailure(args);
                    throw new InvalidOperationException($"Segment failed after {maxAttempts} attempts.", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }

        throw new InvalidOperationException($"Unreachable code and segment failed after {maxAttempts} attempts.");
    }

    static partial class Log
    {
        [LoggerMessage(0, LogLevel.Error, "Inbound queue closed; stopping segment consumption for pipeline '{pipeline}' segment '{segment}'.")]
        internal static partial void InboundQueueClosed(ILogger logger, string pipeline, string segment, Exception ex);

        [LoggerMessage(1, LogLevel.Information, "Pipeline '{pipeline}' segment '{segment}' starting processing message with sequence #{sequence}.")]
        internal static partial void SegmentMessageStart(ILogger logger, string pipeline, string segment, ulong sequence);

        [LoggerMessage(2, LogLevel.Information, "Pipeline '{pipeline}' segment '{segment}' successfully processed message with sequence #{sequence}.")]
        internal static partial void SegmentMessageCompleted(ILogger logger, string pipeline, string segment, ulong sequence);

        [LoggerMessage(3, LogLevel.Warning, "Pipeline '{pipeline}' segment '{segment}' aborted processing message with sequence #{sequence} due to repeated failure.")]
        internal static partial void SegmentMessageAborted(ILogger logger, string pipeline, string segment, ulong sequence, Exception ex);

        [LoggerMessage(4, LogLevel.Information, "Pipeline '{pipeline}' segment '{segment}' successfully sent message with sequence #{sequence}.")]
        internal static partial void SegmentMessageSent(ILogger logger, string pipeline, string segment, ulong sequence);

        [LoggerMessage(5, LogLevel.Warning, "Pipeline '{pipeline}' segment '{segment}' attempt ({attempt} / {maxAttempts}) failed.")]
        internal static partial void SegmentAttemptFailed(ILogger logger, string pipeline, string segment, uint attempt, uint maxAttempts, Exception ex);

        [LoggerMessage(6, LogLevel.Error, "Pipeline '{pipeline}' segment '{segment}' exhausted all {maxAttempts} retry attempts for message with sequence #{sequence}.")]
        internal static partial void SegmentRetriesExhausted(ILogger logger, string pipeline, string segment, uint maxAttempts, ulong sequence, Exception ex);

        [LoggerMessage(7, LogLevel.Error, "Unhandled exception while processing pipeline '{pipeline}' segment '{segment}' message with sequence #{sequence}.")]
        internal static partial void SegmentUnhandledError(ILogger logger, string pipeline, string segment, ulong sequence, Exception ex);
    }
}

/// <summary>
/// Context provided to pipeline segment functions.
/// </summary>
/// <typeparam name="TData">The message data type.</typeparam>
public class PipelineSegmentContext<TData>
{
    /// <summary>
    /// The message data being processed.
    /// </summary>
    public required TData Data { get; init; }

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
/// Delegate for pipeline segment functions that transform messages.
/// </summary>
/// <typeparam name="TIn">The input message type.</typeparam>
/// <typeparam name="TOut">The output message type.</typeparam>
public delegate Task<TOut> PipelineSegment<TIn, TOut>(PipelineSegmentContext<TIn> context);
