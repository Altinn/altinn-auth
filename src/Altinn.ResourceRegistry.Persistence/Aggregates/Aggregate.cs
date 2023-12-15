using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// A base class for aggregates.
/// </summary>
/// <typeparam name="TAggregate">The concrete aggregate type</typeparam>
/// <typeparam name="TEvent">The concrete event type</typeparam>
internal abstract class Aggregate<TAggregate, TEvent>
    : IAggregateEventHandler<TAggregate, TEvent>
    where TAggregate : Aggregate<TAggregate, TEvent>, IAggregateEventHandler<TAggregate, TEvent>, IAggregateFactory<TAggregate, TEvent>
    where TEvent : IAggregateEvent<TAggregate, TEvent>
{
    private readonly TimeProvider _timeProvider;

    [SuppressMessage(
        "StyleCop.CSharp.SpacingRules",
        "SA1010:Opening square brackets should be spaced correctly",
        Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3687")]
    private readonly List<TEvent> _events = [];
    private int _committed = 0;

    /// <summary>
    /// Gets a value indicating wheather the aggregate is initialized.
    /// </summary>
    public abstract bool IsInitialized { get; }

    /// <summary>
    /// Gets a value indicating wheather the aggregate is deleted.
    /// </summary>
    public abstract bool IsDeleted { get; }

    /// <summary>
    /// Gets when this aggregate was created.
    /// </summary>
    public DateTimeOffset CreatedAt
        => _events.Count switch
        {
            0 => throw new InvalidOperationException("Aggregate not initialized"),
            _ => _events[0].EventTime,
        };

    /// <summary>
    /// Gets when this aggregate was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt 
        => _events.Count switch
        {
            0 => throw new InvalidOperationException("Aggregate not initialized"),
            _ => _events[^1].EventTime,
        };

    /// <summary>
    /// Gets the current aggregate version (for use with optimistic concurency).
    /// </summary>
    public ulong CommittedVersion
        => _committed switch
        {
            0 => 0,
            _ => _events[_committed - 1].EventId.UnsafeValue,
        };

    /// <summary>
    /// Gets a value indicating wheather or not this aggregate has any uncommited events.
    /// </summary>
    public bool HasUncommittedEvents => _events.Count != _committed;

    /// <summary>
    /// Asserts that the aggregate is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the aggregate is not initialized</exception>
    protected void AssertInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Aggregate not initialized");
        }

        if (IsDeleted)
        {
            throw new InvalidOperationException("Aggregate is deleted");
        }
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
    /// Gets the aggregate id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Constructs a new <see cref="Aggregate{TAggregate,TEvent}"/> with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider</param>
    /// <param name="id">The aggregate id</param>
    protected Aggregate(TimeProvider timeProvider, Guid id)
    {
        _timeProvider = timeProvider;
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
}
