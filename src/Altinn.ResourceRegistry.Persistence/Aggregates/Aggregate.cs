using Altinn.ResourceRegistry.Core.Aggregates;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// A base class for aggregates.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal abstract class Aggregate<TAggregate, TEvent>
    : IAggregate, IAggregateEventHandler<TAggregate, TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateEventHandler<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    private readonly TimeProvider _timeProvider;
    private readonly IAggregateRepository<TAggregate, TEvent> _repository;

    private readonly List<TEvent> _events = [];
    private int _committed = 0;

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public abstract bool IsInitialized { get; }

    /// <inheritdoc/>
    public abstract bool IsDeleted { get; }

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt
        => _events.Count switch
        {
            0 => throw new InvalidOperationException("Aggregate not initialized"),
            _ => _events[0].EventTime,
        };

    /// <inheritdoc/>
    public DateTimeOffset UpdatedAt 
        => _events.Count switch
        {
            0 => throw new InvalidOperationException("Aggregate not initialized"),
            _ => _events[^1].EventTime,
        };

    /// <inheritdoc/>
    public EventId CommittedVersion
        => _committed switch
        {
            0 => EventId.Unset,
            _ => _events[_committed - 1].EventId,
        };

    /// <summary>
    /// Gets a value indicating wheather or not this aggregate has any uncommited events.
    /// </summary>
    public bool HasUncommittedEvents => _events.Count != _committed;

    /// <summary>
    /// Asserts that the aggregate is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized.</exception>
    protected void AssertInitialized()
    {
        if (!IsInitialized)
        {
            ThrowHelper.ThrowInvalidOperationException("Aggregate not initialized");
        }
    }

    /// <summary>
    /// Asserts thta the aggregate is not deleted.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is deleted.</exception>
    protected void AssertNotDeleted()
    {
        if (!IsInitialized)
        {
            ThrowHelper.ThrowInvalidOperationException("Aggregate deleted");
        }
    }

    /// <summary>
    /// Asserts that the aggregate is both initialized and not deleted.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized or is deleted.</exception>
    protected void AssertLive()
    {
        AssertInitialized();
        AssertNotDeleted();
    }

    /// <summary>
    /// Gets the current time in UTC according to the configured <see cref="TimeProvider"/>.
    /// </summary>
    /// <returns><see cref="DateTimeOffset"/></returns>
    protected DateTimeOffset GetUtcNow()
        => _timeProvider.GetUtcNow();

    /// <summary>
    /// Gets a self-reference that throws an exception if the aggregate is not initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized</exception>
    protected TAggregate InitializedThis
    {
        get
        {
            AssertInitialized();

            return (TAggregate)this;
        }
    }

    /// <summary>
    /// Constructs a new <see cref="Aggregate{TAggregate,TEvent}"/> with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider</param>
    /// <param name="id">The aggregate id</param>
    /// <param name="repository">A <see cref="IAggregateRepository{TAggregate, TEvent}"/> for saving changes to the aggregate</param>
    protected Aggregate(TimeProvider timeProvider, Guid id, IAggregateRepository<TAggregate, TEvent> repository)
    {
        Guard.IsNotNull(timeProvider);
        Guard.IsNotNull(repository);
        Guard.IsNotDefault(id);

        _timeProvider = timeProvider;
        _repository = repository;
        Id = id;
    }

    /// <summary>
    /// Mark all events as committed.
    /// </summary>
    public void Commit()
    {
#if DEBUG
        if (GetUncommittedEvents().Any(static e => !e.EventId.IsSet))
        {
            throw new InvalidOperationException("Cannot commit aggregate events with unset event ids");
        }
#endif

        _committed = _events.Count;
    }

    /// <summary>
    /// Gets the uncommitted events.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of <typeparamref name="TEvent"/>s containing all uncommitted events</returns>
    internal IEnumerable<TEvent> GetUncommittedEvents()
        => _events.Skip(_committed);

    /// <summary>
    /// Apply the <paramref name="event"/> to the aggregate.
    /// </summary>
    /// <param name="event">The event</param>
    protected void AddEvent(TEvent @event)
    {
        @event.ApplyTo((TAggregate)this);
        _events.Add(@event);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<TEvent>.ApplyEvent(TEvent @event)
        => AddEvent(@event);

    /// <inheritdoc />
    public Task SaveChanged(CancellationToken cancellationToken = default)
        => _repository.ApplyChanges((TAggregate)this, cancellationToken);
}
