using System.Collections.Concurrent;
using System.Reflection;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Host.Pipeline.Builders;
using Altinn.Authorization.Host.Pipeline.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.Authorization.Host.Pipeline.HostedServices;

/// <summary>
/// Hosted service that orchestrates pipeline execution with support for recurring schedules, distributed leases, and feature flags.
/// </summary>
internal partial class PipelineHostedService(
    ILogger<PipelineHostedService> logger,
    ILeaseService leaseService,
    FeatureManager featureManager,
    PipelineSourceService pipelineSourceJob,
    PipelineSegmentService pipelineSegmentJob,
    PipelineSinkService pipelineSinkJob,
    IPipelineRegistry registry
) : IHostedService
{
    private readonly CancellationTokenSource _stopCts = new();

    internal List<Task> DispatchedPipelineGroups { get; set; } = [];

    private static readonly MethodInfo _pipelineSourceRunMethodInfo = typeof(PipelineSourceService).GetMethod(nameof(PipelineSourceService.Run));

    private static readonly MethodInfo _pipelineSegmentRunMethodInfo = typeof(PipelineSegmentService).GetMethod(nameof(PipelineSegmentService.Run));

    private static readonly MethodInfo _pipelineSinkRunMethodInfo = typeof(PipelineSinkService).GetMethod(nameof(PipelineSinkService.Run));

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.HostedServiceStarting(logger);

        if (registry.Groups.Count == 0)
        {
            Log.NoPipelinesRegistered(logger);
            return Task.CompletedTask;
        }

        foreach (var group in registry.Groups)
        {
            try
            {
                Log.PipelineGroupRegistered(logger, group.GroupName, group.Builders.Count, group.Recurring?.ToString() ?? "none");
                DispatchedPipelineGroups.Add(DispatchPipelines(group, _stopCts.Token));
            }
            catch (Exception ex)
            {
                Log.UnhandledPipelineGroupError(logger, group.GroupName, ex);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.HostedServiceStopping(logger);

        try
        {
            await _stopCts.CancelAsync();
            await Task.WhenAll(DispatchedPipelineGroups);
        }
        catch (Exception ex)
        {
            Log.FailedToStopPipelines(logger, ex);
        }
        finally
        {
            _stopCts.Dispose();
        }
    }

    private async Task DispatchPipelines(PipelineGroup group, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var descriptor in group.Builders)
            {
                var problem = await DispatchPipeline(group, descriptor, cancellationToken);
                if (problem)
                {
                    break;
                }
            }

            // If this group is not specified recurring. exit.
            if (group.Recurring is null)
            {
                return;
            }

            Log.WaitingForNextRun(logger, group.GroupName, group.Recurring.Value);
            await Task.Delay(group.Recurring.Value, cancellationToken);
        }
    }

    private async Task<bool> DispatchPipeline(PipelineGroup group, PipelineDescriptor descriptor, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var args = new PipelineArgs()
        {
            Descriptor = descriptor,
        };

        try
        {
            if (!await IsPipelineEnabled(group, cancellationToken))
            {
                Log.PipelineDisabled(logger, group.GroupName, descriptor.Name);
                return true;
            }

            if (!string.IsNullOrEmpty(descriptor.LeaseName))
            {
                var lease = await leaseService.TryAcquireNonBlocking(descriptor.LeaseName, cancellationToken);
                if (lease is null)
                {
                    Log.LeaseUnavailable(logger, descriptor.LeaseName);
                    return true;
                }

                args.Lease = lease;
                Log.LeaseAcquired(logger, descriptor.LeaseName);
            }

            Log.PipelineStarting(logger, group.GroupName, descriptor.Name);
            var pipelineTasks = BuildPipeline([], args, descriptor.Source, null, cts);
            var tasks = pipelineTasks.Select(task => Task.Run(async () => await task())).ToList();
            await Task.WhenAll(tasks);
            Log.PipelineCompleted(logger, group.GroupName, descriptor.Name);
        }
        catch (InvalidOperationException ex)
        {
            Log.FailedToBuildPipeline(logger, ex);
        }
        catch (OperationCanceledException)
        {
            Log.PipelineCanceled(logger, group.GroupName, descriptor.Name);
        }
        catch (Exception ex)
        {
            Log.PipelineFailed(logger, group.GroupName, descriptor.Name, ex);
        }
        finally
        {
            await cts.CancelAsync();
            if (args.Lease is not null)
            {
                await args.Lease.DisposeAsync();
                Log.LeaseReleased(logger, descriptor.LeaseName);
            }
        }

        return false;
    }

    private async Task<bool> IsPipelineEnabled(PipelineGroup group, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(group.FeatureFlag))
        {
            bool enabled = await featureManager.IsEnabledAsync(group.FeatureFlag, cancellationToken);
            return enabled;
        }

        return true;
    }

    public List<Func<Task>> BuildPipeline(
        List<Func<Task>> jobs,
        PipelineArgs args,
        object? stage,
        object? inbound,
        CancellationTokenSource cts)
    {
        if (stage is PipelineSourceBuilder sourceBuilder)
        {
            var funcType = sourceBuilder.Func.GetType();
            var genericArgs = funcType.GetGenericArguments();
            var outboundType = genericArgs[0];

            var method = GetPipelineSourceRunMethod(outboundType);
            var outbound = GetPipelineOutbound(outboundType);

            var newArgs = new PipelineArgs()
            {
                Descriptor = args.Descriptor,
                Lease = args.Lease,
                Name = sourceBuilder.Name,
            };

            jobs.Add(() => (Task)method.Invoke(pipelineSourceJob, [newArgs, sourceBuilder.Func, outbound, cts]!));
            return BuildPipeline(jobs, args, sourceBuilder.Next, outbound, cts);
        }

        if (IsTypePipelineSegmentBuilder(stage))
        {
            var (name, func, segment, sink) = GetSegmentBuilderProperties(stage);

            var newArgs = new PipelineArgs()
            {
                Descriptor = args.Descriptor,
                Lease = args.Lease,
                Name = name,
            };

            if (segment is not null)
            {
                var funcType = func.GetType();
                var genericArgs = funcType.GetGenericArguments();
                var outboundType = genericArgs[1];

                var method = GetPipelineSegmentRunMethod(func);
                var outbound = GetPipelineOutbound(outboundType);

                jobs.Add(() => (Task)method.Invoke(pipelineSegmentJob, [newArgs, func, inbound, outbound]!));
                return BuildPipeline(jobs, args, segment, outbound, cts);
            }
            else if (sink is not null)
            {
                var funcType = func.GetType();
                var genericArgs = funcType.GetGenericArguments();
                var inboundType = genericArgs[0];

                var method = GetPipelineSinkRunMethod(inboundType);
                jobs.Add(() => (Task)method.Invoke(pipelineSinkJob, [newArgs, func, inbound]!));
            }
            else
            {
                throw new InvalidOperationException("Pipeline builder has both Segment and Sink set to null.");
            }
        }

        return jobs;
    }

    private static MethodInfo GetPipelineSourceRunMethod(Type type) =>
        _pipelineSourceRunMethodInfo.MakeGenericMethod(type);

    private static MethodInfo GetPipelineSegmentRunMethod(object func)
    {
        var funcType = func.GetType();
        var genericArgs = funcType.GetGenericArguments();
        var inboundType = genericArgs[0];
        var outboundType = genericArgs[1];

        return _pipelineSegmentRunMethodInfo.MakeGenericMethod(inboundType, outboundType);
    }

    private static MethodInfo GetPipelineSinkRunMethod(Type inboundType) =>
        _pipelineSinkRunMethodInfo.MakeGenericMethod(inboundType);

    private static object GetPipelineOutbound(Type type, int capacity = 3)
    {
        var messageType = typeof(PipelineMessage<>).MakeGenericType(type);
        var collectionType = typeof(BlockingCollection<>).MakeGenericType(messageType);
        var result = Activator.CreateInstance(collectionType, capacity)!;
        return result;
    }

    private static bool IsTypePipelineSegmentBuilder(object? currentStage) =>
        currentStage is not null &&
        currentStage.GetType().IsGenericType &&
        currentStage.GetType().GetGenericTypeDefinition() == typeof(PipelineSegmentBuilder<>);

    private static (string Name, object? Func, object? Segment, object? Sink) GetSegmentBuilderProperties(object stage)
    {
        var type = stage.GetType();
        var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        var nameProp = type.GetProperty(nameof(PipelineSegmentBuilder<object>.Name), flags);
        var funcProp = type.GetProperty(nameof(PipelineSegmentBuilder<object>.Func), flags);
        var segmentProp = type.GetProperty(nameof(PipelineSegmentBuilder<object>.Segment), flags);
        var sinkProp = type.GetProperty(nameof(PipelineSegmentBuilder<object>.Sink), flags);

        return (
            nameProp?.GetValue(stage) as string,
            funcProp?.GetValue(stage),
            segmentProp?.GetValue(stage),
            sinkProp?.GetValue(stage)
        );
    }

    static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Starting pipeline hosted service...")]
        internal static partial void HostedServiceStarting(ILogger logger);

        [LoggerMessage(1, LogLevel.Information, "No pipelines registered.")]
        internal static partial void NoPipelinesRegistered(ILogger logger);

        [LoggerMessage(2, LogLevel.Information, "Pipeline group '{PipelineGroup}' registered with {PipelineCount} pipelines (recurring: {Recurring}).")]
        internal static partial void PipelineGroupRegistered(ILogger logger, string PipelineGroup, int PipelineCount, string Recurring);

        [LoggerMessage(3, LogLevel.Information, "Stopping pipeline hosted service...")]
        internal static partial void HostedServiceStopping(ILogger logger);

        [LoggerMessage(4, LogLevel.Information, "Lease is present.")]
        internal static partial void LeaseIsPresent(ILogger logger);

        [LoggerMessage(5, LogLevel.Error, "Failed to build pipeline.")]
        internal static partial void FailedToBuildPipeline(ILogger logger, Exception ex);

        [LoggerMessage(6, LogLevel.Error, "Pipeline '{PipelineName}' in group '{PipelineGroup}' failed.")]
        internal static partial void PipelineFailed(ILogger logger, string PipelineGroup, string PipelineName, Exception ex);

        [LoggerMessage(7, LogLevel.Warning, "Pipeline '{PipelineName}' in group '{PipelineGroup}' was canceled.")]
        internal static partial void PipelineCanceled(ILogger logger, string PipelineGroup, string PipelineName);

        [LoggerMessage(8, LogLevel.Information, "Pipeline '{PipelineName}' in group '{PipelineGroup}' completed successfully.")]
        internal static partial void PipelineCompleted(ILogger logger, string PipelineGroup, string PipelineName);

        [LoggerMessage(9, LogLevel.Information, "Pipeline '{PipelineName}' in group '{PipelineGroup}' starting...")]
        internal static partial void PipelineStarting(ILogger logger, string PipelineGroup, string PipelineName);

        [LoggerMessage(10, LogLevel.Warning, "Feature flag disabled pipeline group '{PipelineGroup}' (pipeline '{PipelineName}').")]
        internal static partial void PipelineDisabled(ILogger logger, string PipelineGroup, string PipelineName);

        [LoggerMessage(11, LogLevel.Warning, "Lease unavailable for '{LeaseName}', skipping run.")]
        internal static partial void LeaseUnavailable(ILogger logger, string LeaseName);

        [LoggerMessage(12, LogLevel.Information, "Lease '{LeaseName}' acquired.")]
        internal static partial void LeaseAcquired(ILogger logger, string LeaseName);

        [LoggerMessage(13, LogLevel.Information, "Lease '{LeaseName}' released.")]
        internal static partial void LeaseReleased(ILogger logger, string LeaseName);

        [LoggerMessage(14, LogLevel.Information, "Waiting {Delay} before next pipeline group '{PipelineGroup}' run...")]
        internal static partial void WaitingForNextRun(ILogger logger, string PipelineGroup, TimeSpan Delay);

        [LoggerMessage(15, LogLevel.Error, "Unhandled exception while processing pipeline group '{PipelineGroup}'.")]
        internal static partial void UnhandledPipelineGroupError(ILogger logger, string PipelineGroup, Exception ex);

        [LoggerMessage(16, LogLevel.Error, "Failed to stop running pipelines cleanly.")]
        internal static partial void FailedToStopPipelines(ILogger logger, Exception ex);
    }
}
