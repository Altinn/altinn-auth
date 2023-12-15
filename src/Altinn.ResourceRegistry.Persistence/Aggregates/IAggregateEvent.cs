namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// An event that can be applied to an aggregate.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal interface IAggregateEvent<TAggregate, in TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    /// <summary>
    /// Get's the event id.
    /// </summary>
    EventId EventId { get; }

    /// <summary>
    /// Gets the event time.
    /// </summary>
    DateTimeOffset EventTime { get; }

    /// <summary>
    /// Apply the event to the specified <paramref name="aggregate"/>.
    /// </summary>
    /// <param name="aggregate">The aggregate</param>
    /// <remarks>This implements the visitor pattern.</remarks>
    void ApplyTo(TAggregate aggregate);
}
