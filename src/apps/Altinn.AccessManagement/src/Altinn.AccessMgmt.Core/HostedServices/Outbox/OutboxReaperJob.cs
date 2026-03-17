using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.HostedServices.Outbox;

/// <summary>
/// Background job for managing failed outbox messages
/// </summary>
/// <param name="logger"><see cref="ILogger"/></param>
/// <param name="provider"><see cref="IServiceProvider"/></param>
/// <param name="featureManager">for managing if job should be on or off.</param>
internal partial class OutboxReaperJob(
    ILogger<OutboxHandlerJob> logger,
    IServiceProvider provider,
    IFeatureManager featureManager
    ) : IHostedService
{
    private int _stopping = 0;

    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    private Task ReaperTask { get; set; } = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.OutboxReaperStarting(logger);
        ReaperTask = Job(CancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task Job(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesOutboxReaper, cancellationToken))
            {
                await ReaperJob(cancellationToken);
            }

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.HostedServicesOutboxReaper, cancellationToken))
                {
                    await ReaperJob(cancellationToken);
                }
            }
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            Log.OutboxReaperShutDownGracefully(logger);
        }
    }

    public async Task ReaperJob(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var scope = provider.CreateEFScope(SystemEntityConstants.Outbox);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ProcessTimedOutJobs(db, cancellationToken);
        await ProcessFailedJobs(db, cancellationToken);
        await RemoveOldJobs(db, cancellationToken);
    }

    private static async Task RemoveOldJobs(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.OutboxMessages.FromSqlRaw(/*strpgsql*/
        """
            DELETE FROM dbo.outboxmessage
            WHERE
                status = 'Completed'
                AND completedat < NOW() - INTERVAL '1 days'
            RETURNING dbo.outboxmessage.*;
        """)
        .ToListAsync(cancellationToken);
    }

    private static async Task ProcessFailedJobs(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.OutboxMessages.FromSqlRaw(/*strpgsql*/
        """
            WITH locked_rows AS (
                SELECT id
                FROM dbo.outboxmessage
                WHERE (status = 'Failed' OR status = 'TimedOut' OR status = 'Interrupted') AND retries < 2
                FOR UPDATE SKIP LOCKED
            )
            UPDATE dbo.outboxmessage
            SET
                retries = retries + 1,
                status = 'Pending',
                startedat = NULL,
                completedat = NULL
            FROM locked_rows
            WHERE dbo.outboxmessage.id = locked_rows.id
            RETURNING dbo.outboxmessage.*;
        """)
        .ToListAsync(cancellationToken);
    }

    private static async Task ProcessTimedOutJobs(AppDbContext db, CancellationToken cancellationToken)
    {
        await db.OutboxMessages.FromSqlRaw(/*strpgsql*/
        """
            WITH locked_rows AS (
                SELECT id
                FROM dbo.outboxmessage
                WHERE
                    status = 'Processing'
                    AND startedat IS NOT NULL
                    AND (NOW() > startedat + timeout::interval + INTERVAL '5 seconds')
                    AND retries < 2
                FOR UPDATE SKIP LOCKED
            )
            UPDATE dbo.outboxmessage
            SET
                startedat = NULL,
                completedat = NULL,
                retries = retries + 1,
                status = 'Pending'
            FROM locked_rows
            WHERE dbo.outboxmessage.id = locked_rows.id
            RETURNING dbo.outboxmessage.*;
        """)
        .ToListAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopping, 1) == 1)
        {
            return;
        }

        Log.OutboxReaperReceivedQuitSignal(logger);
        await CancellationTokenSource.CancelAsync();
        if (ReaperTask is { })
        {
            await ReaperTask;
        }

        CancellationTokenSource?.Dispose();
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Outbox reaper starting.")]
        internal static partial void OutboxReaperStarting(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Outbox reaper received quit signal.")]
        internal static partial void OutboxReaperReceivedQuitSignal(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Outbox reaper shut down gracefully.")]
        internal static partial void OutboxReaperShutDownGracefully(ILogger logger);
    }
}
