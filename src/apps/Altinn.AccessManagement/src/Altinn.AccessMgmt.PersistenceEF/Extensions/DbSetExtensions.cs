using System.Diagnostics;
using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

/// <summary>
/// Provides extension methods for working with outbox messages and logs.
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Adds a log entry for the specified outbox message.
    /// </summary>
    /// <param name="dbset">
    /// The <see cref="DbSet{TEntity}"/> containing <see cref="OutboxMessageLog"/> entities.
    /// </param>
    /// <param name="outbox">
    /// The outbox message the log entry should be associated with.
    /// If <see langword="null"/>, no log entry is added.
    /// </param>
    /// <param name="content">
    /// The log message to store.
    /// </param>
    public static void Add(this DbSet<OutboxMessageLog> dbset, OutboxMessage outbox, string content)
    {
        if (outbox is { })
        {
            dbset.Add(new()
            {
                OutboxMessageId = outbox.Id,
                Attempt = outbox.Attempt,
                Log = content,
            });
        }
    }

    /// <summary>
    /// Removes a pending outbox message matching the specified reference identifier and handler.
    /// </summary>
    /// <remarks>
    /// This method locates an <see cref="OutboxMessage"/> with the given
    /// <paramref name="refId"/> and <paramref name="handler"/> that has status
    /// <see cref="OutboxStatus.Pending"/>.
    ///
    /// If such a message exists, it is removed from the <see cref="DbSet{TEntity}"/>.
    /// If no matching message is found, no action is taken.
    /// </remarks>
    /// <param name="dbset">
    /// The <see cref="DbSet{TEntity}"/> containing <see cref="OutboxMessage"/> entities.
    /// </param>
    /// <param name="refId">
    /// The reference identifier used to locate the outbox message.
    /// </param>
    /// <param name="handler">
    /// The name of the handler associated with the outbox message.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public static async Task CancelOutboxAsync(this DbSet<OutboxMessage> dbset, string refId, string handler, CancellationToken cancellationToken = default)
    {
        var message = await dbset
            .AsTracking()
            .FirstOrDefaultAsync(
                o =>
                o.RefId == refId &&
                o.Handler == handler &&
                o.Status == OutboxStatus.Pending,
                cancellationToken);

        if (message is { })
        {
            dbset.Remove(message);
        }
    }

    /// <summary>
    /// Adds a new pending outbox message for the specified reference identifier,
    /// or updates the payload of an existing pending outbox message.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for outbox messages identified by
    /// <paramref name="refId"/> and <paramref name="handler"/>.
    ///
    /// If a matching pending outbox message exists, its payload is deserialized to
    /// <typeparamref name="T"/>, passed to <paramref name="updateValueFactory"/>,
    /// and the returned value is serialized back into the message.
    ///
    /// If no matching pending outbox message exists, a new <see cref="OutboxMessage"/>
    /// is created and its payload is initialized using <paramref name="addValueFactory"/>.
    ///
    /// Only messages with status <see cref="OutboxStatus.Pending"/> are considered
    /// when locating an existing outbox message to update.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the payload stored in the outbox message.
    /// </typeparam>
    /// <param name="dbset">
    /// The <see cref="DbSet{TEntity}"/> containing <see cref="OutboxMessage"/> entities.
    /// </param>
    /// <param name="refId">
    /// The reference identifier used to locate an existing pending outbox message.
    /// This typically represents a domain entity identifier or correlation key.
    /// </param>
    /// <param name="handler">
    /// The name of the handler that will process the outbox message.
    /// </param>
    /// <param name="addValueFactory">
    /// A factory used to create the payload for a new outbox message when no matching
    /// pending message exists. The newly created <see cref="OutboxMessage"/> is passed
    /// to the factory.
    /// </param>
    /// <param name="updateValueFactory">
    /// A function used to produce an updated payload when a matching pending outbox message exists.
    /// The function receives the existing <see cref="OutboxMessage"/> and the current deserialized
    /// payload, and returns the value to be stored.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous upsert operation.
    /// </returns>
    public static async Task UpsertOutboxAsync<T>(
        this DbSet<OutboxMessage> dbset,
        string refId,
        string handler,
        Func<OutboxMessage, T> addValueFactory,
        Func<OutboxMessage, T, T> updateValueFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(refId);
        ArgumentNullException.ThrowIfNull(addValueFactory);

        var message = await dbset
            .AsTracking()
            .FirstOrDefaultAsync(
                o =>
                o.RefId == refId &&
                o.Handler == handler &&
                o.Status == OutboxStatus.Pending,
                cancellationToken);

        UpsertOutbox(dbset, refId, handler, addValueFactory, updateValueFactory, message);
    }

    /// <summary>
    /// Adds a new pending outbox message for the specified reference identifier,
    /// or updates the payload of an existing pending outbox message.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for outbox messages identified by
    /// <paramref name="refId"/> and <paramref name="handler"/>.
    ///
    /// If a matching pending outbox message exists, its payload is deserialized to
    /// <typeparamref name="T"/>, passed to <paramref name="updateValueFactory"/>,
    /// and the returned value is serialized back into the message.
    ///
    /// If no matching pending outbox message exists, a new <see cref="OutboxMessage"/>
    /// is created and its payload is initialized using <paramref name="addValueFactory"/>.
    ///
    /// Only messages with status <see cref="OutboxStatus.Pending"/> are considered
    /// when locating an existing outbox message to update.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the payload stored in the outbox message.
    /// </typeparam>
    /// <param name="dbset">
    /// The <see cref="DbSet{TEntity}"/> containing <see cref="OutboxMessage"/> entities.
    /// </param>
    /// <param name="refId">
    /// The reference identifier used to locate an existing pending outbox message.
    /// This typically represents a domain entity identifier or correlation key.
    /// </param>
    /// <param name="handler">
    /// The name of the handler that will process the outbox message.
    /// </param>
    /// <param name="addValueFactory">
    /// A factory used to create the payload for a new outbox message when no matching
    /// pending message exists. The newly created <see cref="OutboxMessage"/> is passed
    /// to the factory.
    /// </param>
    /// <param name="updateValueFactory">
    /// A function used to produce an updated payload when a matching pending outbox message exists.
    /// The function receives the existing <see cref="OutboxMessage"/> and the current deserialized
    /// payload, and returns the value to be stored.
    /// </param>
    public static void UpsertOutbox<T>(
        this DbSet<OutboxMessage> dbset,
        string refId,
        string handler,
        Func<OutboxMessage, T> addValueFactory,
        Func<OutboxMessage, T, T> updateValueFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(refId);
        ArgumentNullException.ThrowIfNull(addValueFactory);

        var message = dbset
            .AsTracking()
            .FirstOrDefault(o =>
                o.RefId == refId &&
                o.Handler == handler &&
                o.Status == OutboxStatus.Pending);

        UpsertOutbox(dbset, refId, handler, addValueFactory, updateValueFactory, message);
    }

    private static void UpsertOutbox<T>(
        DbSet<OutboxMessage> dbset,
        string refId,
        string handler,
        Func<OutboxMessage, T> addValueFactory,
        Func<OutboxMessage, T, T> updateValueFactory,
        OutboxMessage message)
    {
        if (message is { })
        {
            if (updateValueFactory is { })
            {
                var data = JsonSerializer.Deserialize<T>(message.Data);
                var updatedValue = updateValueFactory(message, data);
                message.Data = JsonSerializer.Serialize(updatedValue);
            }
        }
        else
        {
            message = new OutboxMessage()
            {
                CorrelationId = Activity.Current?.TraceId.ToString(),
                Status = OutboxStatus.Pending,
                RefId = refId,
                Handler = handler,
            };

            var data = addValueFactory(message);
            message.Data = JsonSerializer.Serialize(data);
            dbset.Add(message);
        }
    }
}
