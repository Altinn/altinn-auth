namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// A repository for a <typeparamref name="TAggregate"/>.
/// </summary>
/// <typeparam name="TAggregate"></typeparam>
/// <typeparam name="TEvent"></typeparam>
internal interface IAggregateRepository<TAggregate, TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateEventHandler<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    /// <summary>
    /// Load an aggregate from the database.
    /// </summary>
    /// <param name="id">The aggregate id</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>An <typeparamref name="TAggregate"/>, if it exists, otherwise <see langword="null"/>.</returns>
    Task<TAggregate?> Load(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to an aggregate to the database.
    /// </summary>
    /// <param name="aggregate">The aggregate</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    Task ApplyChanges(TAggregate aggregate, CancellationToken cancellationToken = default);
}
