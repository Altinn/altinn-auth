using System.Collections.Immutable;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Aggregates;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// Represents an access list aggregate.
/// </summary>
internal class AccessListAggregate
    : Aggregate<AccessListAggregate, AccessListEvent>
    , IAccessListAggregate
    , IAggregateFactory<AccessListAggregate, AccessListEvent>
    , IAggregateEventHandler<AccessListCreatedEvent>
    , IAggregateEventHandler<AccessListUpdatedEvent>
    , IAggregateEventHandler<AccessListDeletedEvent>
    , IAggregateEventHandler<AccessListResourceConnectionCreatedEvent>
    , IAggregateEventHandler<AccessListResourceConnectionActionsAddedEvent>
    , IAggregateEventHandler<AccessListResourceConnectionActionsRemovedEvent>
    , IAggregateEventHandler<AccessListResourceConnectionDeletedEvent>
    , IAggregateEventHandler<AccessListMembersAddedEvent>
    , IAggregateEventHandler<AccessListMembersRemovedEvent>
{
    private bool _isDeleted;
    private string? _resourceOwner;
    private string? _identifier;
    private string? _name;
    private string? _description;

    private readonly Dictionary<string, AccessListResourceConnection> _resourceConnections = [];
    private readonly HashSet<Guid> _members = [];

    /// <inheritdoc/>
    static AccessListAggregate IAggregateFactory<AccessListAggregate, AccessListEvent>.New(TimeProvider timeProvider, Guid id, IAggregateRepository<AccessListAggregate, AccessListEvent> repository)
        => New(timeProvider, id, repository);

    /// <summary>
    /// Creates a new <see cref="AccessListAggregate"/>.
    /// </summary>
    /// <param name="timeProvider">The <see cref="TimeProvider"/></param>
    /// <param name="id">The id</param>
    /// <param name="repository">The <see cref="IAggregateRepository{TAggregate, TEvent}"/></param>
    /// <returns>A new <see cref="AccessListAggregate"/></returns>
    internal static AccessListAggregate New(TimeProvider timeProvider, Guid id, IAggregateRepository<AccessListAggregate, AccessListEvent> repository)
        => new(timeProvider, id, repository);

    private AccessListAggregate(TimeProvider timeProvider, Guid id, IAggregateRepository<AccessListAggregate, AccessListEvent> repository)
        : base(timeProvider, id, repository)
    {
    }

    /// <inheritdoc />
    public override bool IsInitialized => _resourceOwner is not null;

    /// <inheritdoc />
    public override bool IsDeleted => _isDeleted;

    /// <inheritdoc />
    public string ResourceOwner => InitializedThis._resourceOwner!;

    /// <inheritdoc />
    public string Identifier => InitializedThis._identifier!;

    /// <inheritdoc />
    public string Name => InitializedThis._name!;

    /// <inheritdoc />
    public string Description => InitializedThis._description!;

    /// <inheritdoc />
    public void Initialize(string resourceOwner, string identifier, string name, string? description)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Aggregate already initialized");
        }

        AddEvent(new AccessListCreatedEvent(EventId.Unset, Id, resourceOwner, identifier, name, description ?? string.Empty, GetUtcNow()));
    }

    /// <inheritdoc />
    public void Update(
        string? identifier = null,
        string? name = null,
        string? description = null)
    {
        AssertLive();

        if (identifier is null
            && name is null
            && description is null)
        {
            throw new ArgumentException("At least one of the parameters must be specified");
        }

        AddEvent(new AccessListUpdatedEvent(EventId.Unset, Id, identifier, name, description, GetUtcNow()));
    }

    /// <inheritdoc />
    public void Delete()
    {
        AssertLive();

        AddEvent(new AccessListDeletedEvent(EventId.Unset, Id, GetUtcNow()));
    }

    /// <inheritdoc />
    public AccessListResourceConnection AddResourceConnection(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertLive();

        if (_resourceConnections.ContainsKey(resourceIdentifier))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' already exists");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        AddEvent(new AccessListResourceConnectionCreatedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <inheritdoc />
    public AccessListResourceConnection AddResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertLive();

        if (!_resourceConnections.TryGetValue(resourceIdentifier, out var connection))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        if (actionsImmutable.Any(connection.Actions.Contains))
        {
            throw new ArgumentException("One or more actions already exist in the resource connection", nameof(actions));
        }

        AddEvent(new AccessListResourceConnectionActionsAddedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <inheritdoc />
    public AccessListResourceConnection RemoveResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertLive();

        if (!_resourceConnections.TryGetValue(resourceIdentifier, out var connection))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        if (!actionsImmutable.All(connection.Actions.Contains))
        {
            throw new ArgumentException("One or more actions already exist in the resource connection", nameof(actions));
        }

        AddEvent(new AccessListResourceConnectionActionsRemovedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <inheritdoc />
    public AccessListResourceConnection RemoveResourceConnection(string resourceIdentifier)
    {
        AssertLive();

        if (!_resourceConnections.TryGetValue(resourceIdentifier, out var connection))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        AddEvent(new AccessListResourceConnectionDeletedEvent(EventId.Unset, Id, resourceIdentifier, GetUtcNow()));
        return connection;
    }

    /// <inheritdoc />
    public void AddMembers(IEnumerable<Guid> partyIds)
    {
        AssertLive();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (partyIdsImmutable.Any(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs already exist in the registry", nameof(partyIds));
        }

        AddEvent(new AccessListMembersAddedEvent(EventId.Unset, Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <inheritdoc />
    public void RemoveMembers(IEnumerable<Guid> partyIds)
    {
        AssertLive();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (!partyIdsImmutable.All(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs do not exist in the registry", nameof(partyIds));
        }

        AddEvent(new AccessListMembersRemovedEvent(EventId.Unset, Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListCreatedEvent>.ApplyEvent(AccessListCreatedEvent @event)
    {
        _resourceOwner = @event.ResourceOwner;
        _identifier = @event.Identifier;
        _name = @event.Name;
        _description = @event.Description;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListUpdatedEvent>.ApplyEvent(AccessListUpdatedEvent @event)
    {
        if (@event.Identifier is { } identifier)
        {
            _identifier = identifier;
        }

        if (@event.Name is { } name)
        {
            _name = name;
        }

        if (@event.Description is { } description)
        {
            _description = description;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListDeletedEvent>.ApplyEvent(AccessListDeletedEvent @event)
    {
        _isDeleted = true;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListResourceConnectionCreatedEvent>.ApplyEvent(AccessListResourceConnectionCreatedEvent @event)
    {
        _resourceConnections[@event.ResourceIdentifier] = new AccessListResourceConnection(@event.ResourceIdentifier, [..@event.Actions], @event.EventTime, @event.EventTime);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListResourceConnectionActionsAddedEvent>.ApplyEvent(AccessListResourceConnectionActionsAddedEvent @event)
    {
        if (_resourceConnections.TryGetValue(@event.ResourceIdentifier, out var connection))
        {
            connection = connection with { Actions = connection.Actions.Union(@event.Actions), Modified = @event.EventTime };
            _resourceConnections[connection.ResourceIdentifier] = connection;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListResourceConnectionActionsRemovedEvent>.ApplyEvent(AccessListResourceConnectionActionsRemovedEvent @event)
    {
        if (_resourceConnections.TryGetValue(@event.ResourceIdentifier, out var connection))
        {
            connection = connection with { Actions = connection.Actions.Except(@event.Actions), Modified = @event.EventTime };
            _resourceConnections[connection.ResourceIdentifier] = connection;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListResourceConnectionDeletedEvent>.ApplyEvent(AccessListResourceConnectionDeletedEvent @event)
    {
        _resourceConnections.Remove(@event.ResourceIdentifier);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListMembersAddedEvent>.ApplyEvent(AccessListMembersAddedEvent @event)
    {
        _members.UnionWith(@event.PartyIds);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<AccessListMembersRemovedEvent>.ApplyEvent(AccessListMembersRemovedEvent @event)
    {
        _members.ExceptWith(@event.PartyIds);
    }

    /// <summary>
    /// Gets the aggregate as a <see cref="AccessListInfo"/>.
    /// </summary>
    /// <returns><see cref="AccessListInfo"/></returns>
    /// <exception cref="InvalidOperationException">Thrown if aggregate is not commited.</exception>
    public AccessListInfo AsAccessListInfo()
    {
        if (HasUncommittedEvents)
        {
            ThrowHelper.ThrowInvalidOperationException("Cannot get access list info for uncommitted aggregate");
        }

        return new AccessListInfo(Id, ResourceOwner, Identifier, Name, Description, CreatedAt, UpdatedAt, CommittedVersion.UnsafeValue);
    }
}

/// <summary>
/// Base class for <see cref="AccessListAggregate"/> events.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AccessListId">The access list id.</param>
/// <param name="EventTime">The event time.</param>
internal abstract record AccessListEvent(EventId EventId, Guid AccessListId, DateTimeOffset EventTime)
    : IAggregateEvent<AccessListAggregate, AccessListEvent>
{
    private EventId _eventId = EventId;

    /// <summary>
    /// Gets the event id.
    /// </summary>
    public EventId EventId
    {
        get => _eventId;
        internal set
        {
#if DEBUG
            if (_eventId.IsSet)
            {
                throw new InvalidOperationException("Event id already set");
            }
#endif

            _eventId = value;
        }
    }

    /// <summary>
    /// Apply the event to the specified <paramref name="aggregate"/>.
    /// </summary>
    /// <param name="aggregate">The aggregate</param>
    /// <remarks>This implements the visitor pattern.</remarks>
    protected abstract void ApplyTo(AccessListAggregate aggregate);

    /// <inheritdoc />
    void IAggregateEvent<AccessListAggregate, AccessListEvent>.ApplyTo(AccessListAggregate aggregate)
        => ApplyTo(aggregate);

    /// <summary>
    /// Gets the event as a set of values used for persistence.
    /// </summary>
    /// <returns><see cref="Values"/></returns>
    internal abstract Values AsValues();

    /// <summary>
    /// Database representation of events.
    /// </summary>
    /// <param name="Kind">The event kind.</param>
    /// <param name="EventTime">The event time.</param>
    /// <param name="AggregateId">The aggregate id.</param>
    /// <param name="Identifier">Optional identifier.</param>
    /// <param name="Name">Optional name.</param>
    /// <param name="Description">Optional description.</param>
    /// <param name="ResourceOwner">Optional resource owner.</param>
    /// <param name="Actions">Optional set of actions.</param>
    /// <param name="PartyIds">Optional set of party ids.</param>
    internal readonly record struct Values(
        string Kind,
        DateTimeOffset EventTime,
        Guid AggregateId,
        string? Identifier,
        string? Name,
        string? Description,
        string? ResourceOwner,
        ImmutableArray<string> Actions,
        ImmutableArray<Guid> PartyIds);
}

/// <summary>
/// Event for when an access list is created.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="ResourceOwner">The resource owner.</param>
/// <param name="Identifier">The owner-unique identifier.</param>
/// <param name="Name">The access list display-name.</param>
/// <param name="Description">The access list description.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListCreatedEvent(
    EventId EventId,
    Guid AggregateId,
    string ResourceOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListCreatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "created",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            ResourceOwner: ResourceOwner,
            Actions: default,
            PartyIds: default);
}

/// <summary>
/// Event for when an access list is updated.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="Identifier">The owner-unique identifier (if updated).</param>
/// <param name="Name">The access list display-name (if updated).</param>
/// <param name="Description">The access list description (if updated).</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListUpdatedEvent(
    EventId EventId,
    Guid AggregateId,
    string? Identifier,
    string? Name,
    string? Description,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListUpdatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "updated",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            ResourceOwner: null,
            Actions: default,
            PartyIds: default);
}

/// <summary>
/// Event for when an access list is deleted.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListDeletedEvent(
    EventId EventId,
    Guid AggregateId,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListDeletedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "deleted",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: null,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: default,
            PartyIds: default);
}

/// <summary>
/// Event for when a resource connection is created.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListResourceConnectionCreatedEvent(
    EventId EventId,
    Guid AggregateId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListResourceConnectionCreatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_created",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: Actions,
            PartyIds: default);
}

/// <summary>
/// Event for when actions are added to a resource connection.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The newly added actions.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListResourceConnectionActionsAddedEvent(
    EventId EventId,
    Guid AggregateId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListResourceConnectionActionsAddedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_actions_added",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: Actions,
            PartyIds: default);
}

/// <summary>
/// Event for when actions are removed from a resource connection.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The removed actions.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListResourceConnectionActionsRemovedEvent(
    EventId EventId,
    Guid AggregateId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListResourceConnectionActionsRemovedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_actions_removed",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: Actions,
            PartyIds: default);
}

/// <summary>
/// Event for when a resource connection is deleted.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListResourceConnectionDeletedEvent(
    EventId EventId,
    Guid AggregateId,
    string ResourceIdentifier,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListResourceConnectionDeletedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_deleted",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: default,
            PartyIds: default);
}

/// <summary>
/// Event for when members are added to the access list.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="PartyIds">The parties added.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListMembersAddedEvent(
    EventId EventId,
    Guid AggregateId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListMembersAddedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_added",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: null,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}

/// <summary>
/// Event for when members are removed from the access list.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="AggregateId">The aggregate id.</param>
/// <param name="PartyIds">The parties removed.</param>
/// <param name="EventTime">The event time.</param>
internal record AccessListMembersRemovedEvent(
    EventId EventId,
    Guid AggregateId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : AccessListEvent(EventId, AggregateId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(AccessListAggregate aggregate)
        => ((IAggregateEventHandler<AccessListMembersRemovedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_removed",
            EventTime: EventTime,
            AggregateId: AggregateId,
            Identifier: null,
            Name: null,
            Description: null,
            ResourceOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}
