using Altinn.ResourceRegistry.Core.Aggregates;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// Exception thrown when an optimistic concurrency exception occurs.
/// </summary>
public class OptimisticConcurrencyException 
    : Exception
{
    /// <summary>
    /// Creates a new <see cref="OptimisticConcurrencyException"/> for the given <paramref name="aggregate"/>.
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    /// <param name="aggregate">The <see cref="IAggregate"/> that failed to update</param>
    /// <returns><see cref="OptimisticConcurrencyException"/></returns>
    public static OptimisticConcurrencyException Create<TAggregate>(TAggregate aggregate)
        where TAggregate : IAggregate
    {
        var typeName = aggregate.GetType().Name;
        if (typeName.EndsWith("Aggregate"))
        {
            typeName = typeName[..^9];
        }

        return new OptimisticConcurrencyException(typeName, aggregate.Id, aggregate.CommittedVersion);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimisticConcurrencyException"/> class.
    /// </summary>
    /// <param name="aggregateType">The aggregate type</param>
    /// <param name="aggregateId">The aggregate id</param>
    /// <param name="expectedVersion">The expected version</param>
    public OptimisticConcurrencyException(string aggregateType, Guid aggregateId, EventId expectedVersion)
        : base($"""Optimistic concurrency exception. Aggregate {aggregateType} with id "{aggregateId}" expected version "{expectedVersion}" in database.""")
    {
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
    }

    /// <summary>
    /// Gets the type of the aggregate that failed to update.
    /// </summary>
    public string AggregateType { get; }

    /// <summary>
    /// Gets the id of the aggregate that failed to update.
    /// </summary>
    public Guid AggregateId { get; }

    /// <summary>
    /// Gets the expected version of the aggregate.
    /// </summary>
    public EventId ExpectedVersion { get; }
}
