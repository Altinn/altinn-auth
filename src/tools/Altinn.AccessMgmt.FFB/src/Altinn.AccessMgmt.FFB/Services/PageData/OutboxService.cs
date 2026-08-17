using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Queries and mutates <c>dbo.outboxmessage</c> for the outbox page. Search and reset
/// share the same query builder so the reset always targets exactly the searched set.
/// </summary>
public sealed class OutboxService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns outbox messages matching the filters, newest first, capped at <paramref name="maxRows"/>.
    /// </summary>
    /// <param name="environment">Environment key.</param>
    /// <param name="from">CreatedAt lower bound (local date, inclusive). Defaults to yesterday.</param>
    /// <param name="to">CreatedAt upper bound (local date, inclusive). Defaults to tomorrow.</param>
    /// <param name="handler">Handler name, or null for all handlers.</param>
    /// <param name="statuses">Statuses to include.</param>
    /// <param name="dataFilters">JSON key/value pairs matched as substrings in the message data; blank values are ignored.</param>
    /// <param name="maxRows">Maximum number of rows to return.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<List<OutboxMessage>> SearchAsync(
        string environment,
        DateTime? from,
        DateTime? to,
        string? handler,
        IReadOnlyCollection<OutboxStatus> statuses,
        IReadOnlyDictionary<string, string> dataFilters,
        int maxRows,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        return await BuildQuery(db, from, to, handler, statuses, dataFilters)
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxRows)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Resets every message matching the same filters as <see cref="SearchAsync"/> to
    /// <see cref="OutboxStatus.Pending"/> with cleared retries and run timestamps.
    /// Returns the number of affected rows.
    /// </summary>
    /// <param name="environment">Environment key.</param>
    /// <param name="from">CreatedAt lower bound (local date, inclusive). Defaults to yesterday.</param>
    /// <param name="to">CreatedAt upper bound (local date, inclusive). Defaults to tomorrow.</param>
    /// <param name="handler">Handler name, or null for all handlers.</param>
    /// <param name="statuses">Statuses to include.</param>
    /// <param name="dataFilters">JSON key/value pairs matched as substrings in the message data; blank values are ignored.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<int> ResetToPendingAsync(
        string environment,
        DateTime? from,
        DateTime? to,
        string? handler,
        IReadOnlyCollection<OutboxStatus> statuses,
        IReadOnlyDictionary<string, string> dataFilters,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);

        return await BuildQuery(db, from, to, handler, statuses, dataFilters)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(m => m.Status, OutboxStatus.Pending)
                    .SetProperty(m => m.Retries, 0)
                    .SetProperty(m => m.StartedAt, (DateTime?)null)
                    .SetProperty(m => m.CompletedAt, (DateTime?)null),
                ct);
    }

    private static IQueryable<OutboxMessage> BuildQuery(
        AppDbContext db,
        DateTime? from,
        DateTime? to,
        string? handler,
        IReadOnlyCollection<OutboxStatus> statuses,
        IReadOnlyDictionary<string, string> dataFilters)
    {
        var fromUtc = (from ?? DateTime.Today.AddDays(-1)).ToUniversalTime();
        var toUtc = (to ?? DateTime.Today.AddDays(1)).AddDays(1).ToUniversalTime();

        var query = db.OutboxMessages
            .Where(m => m.CreatedAt >= fromUtc && m.CreatedAt < toUtc)
            .Where(m => statuses.Contains(m.Status))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(handler))
        {
            query = query.Where(m => m.Handler == handler);
        }

        foreach (var (jsonKey, value) in dataFilters)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var pattern = $"%\"{jsonKey}\":\"{value}\"%";
                query = query.Where(m => EF.Functions.Like(m.Data, pattern));
            }
        }

        return query;
    }
}
