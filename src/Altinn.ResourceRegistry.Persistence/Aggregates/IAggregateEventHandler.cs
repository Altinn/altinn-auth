namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// Used to implement the visitor pattern for aggregate events.
/// </summary>
internal interface IAggregateEventHandler<TEvent>
{
    /// <summary>
    /// Apply the <paramref name="event"/> to the aggregate.
    /// </summary>
    /// <param name="event">The event</param>
    void ApplyEvent(TEvent @event);
}

/// <summary>
/// Used to implement the visitor pattern for aggregate events.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal interface IAggregateEventHandler<TAggregate, TEvent> 
    : IAggregateEventHandler<TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
}
