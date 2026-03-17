using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices.Outbox;

/// <summary>
/// Background job for managing pending outbox messages.
/// </summary>
/// <param name="logger"><see cref="ILogger"/></param>
/// <param name="provider"><see cref="IServiceProvider"/></param>
/// <param name="featureManager">for managing if job should be on or off.</param>
/// <param name="options">Database options</param>
internal partial class OutboxHandlerJob(
    ILogger<OutboxHandlerJob> logger,
    IServiceProvider provider,
    IFeatureManager featureManager,
    IOptions<AccessManagementDatabaseOptions> options) : IHostedService
{
    private int _stopping = 0;

    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    private Task HandlerTask { get; set; } = Task.CompletedTask;

    /// <summary>
    /// Starts the background outbox handler job by initiating the dispatch loop.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A completed task</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.OutboxHandlerStarting(logger);
        HandlerTask = Job(CancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Main dispatch loop that continuously polls for new outbox messages and processes them.
    /// Runs every minute and handles cancellation and errors gracefully.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    public async Task Job(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesOutboxHandler, cancellationToken))
            {
                await HandlerJob(cancellationToken);
            }

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesOutboxHandler, cancellationToken))
                {
                    await HandlerJob(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.OutboxHandlerShutDownGracefully(logger);
        }
    }

    /// <summary>
    /// Processes a batch of outbox messages by atomically updating their status to 'Processing',
    /// dispatching handlers concurrently, and updating final status and completion time.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    public async Task HandlerJob(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = provider.CreateEFScope(SystemEntityConstants.Outbox);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messages = await db.OutboxMessages.FromSqlRaw(/*strpgsql*/
        """
            WITH locked_rows AS (
                SELECT id
                FROM dbo.outboxmessage
                WHERE status = 'Pending' AND (schedule IS NULL OR schedule <= NOW())
                FOR UPDATE SKIP LOCKED
            )
            UPDATE dbo.outboxmessage
            SET
                status = 'Processing',
                startedat = NOW()
            FROM locked_rows
            WHERE dbo.outboxmessage.id = locked_rows.id
            RETURNING dbo.outboxmessage.*;
        """)
        .AsTracking()
        .ToListAsync(cancellationToken);

        var tasks = new Dictionary<Guid, Task<OutboxStatus>>();
        foreach (var message in messages)
        {
            tasks.Add(message.Id, ProcessMessage(message));
        }

        await Task.WhenAll(tasks.Values);

        foreach (var message in messages)
        {
            message.Status = await tasks[message.Id];
            message.CompletedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(CancellationToken.None);

        async Task<OutboxStatus> ProcessMessage(OutboxMessage message)
        {
            using var scope = provider.CreateEFScope(new AuditValues(SystemEntityConstants.Outbox));
            using var timerCt = new CancellationTokenSource(message.Timeout);
            using var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(timerCt.Token, cancellationToken);

            try
            {
                return await RunHandler(message, scope, linkedCt.Token);
            }
            catch (OperationCanceledException) when (timerCt.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return OutboxStatus.TimedOut;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return OutboxStatus.Interrupted;
            }
        }
    }

    public async Task<OutboxStatus> RunHandler(OutboxMessage message, IServiceScope serviceScope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var handlers = options.Value.Handlers;
        if (handlers.TryGetValue(message.Handler, out var handlerType))
        {
            var handler = serviceScope.ServiceProvider.GetRequiredService(handlerType) as IOutboxHandler;
            if (handler is { })
            {
                try
                {
                    await handler.Handle(message, cancellationToken);
                    return OutboxStatus.Completed;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    message.HandlerMessage = $"Handler threw exception: {ex.Message}\n{ex.StackTrace}";
                    return OutboxStatus.Failed;
                }
            }
        }

        return OutboxStatus.NoHandler;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        Log.OutboxHandlerReceivedQuitSignal(logger);
        await CancellationTokenSource.CancelAsync();
        if (HandlerTask is { })
        {
            await HandlerTask;
        }

        CancellationTokenSource?.Dispose();
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Outbox handler starting.")]
        internal static partial void OutboxHandlerStarting(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Outbox handler received quit signal.")]
        internal static partial void OutboxHandlerReceivedQuitSignal(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Outbox handler shut down gracefully.")]
        internal static partial void OutboxHandlerShutDownGracefully(ILogger logger);
    }
}
