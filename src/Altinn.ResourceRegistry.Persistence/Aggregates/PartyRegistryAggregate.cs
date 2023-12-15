using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Altinn.ResourceRegistry.Core.PartyRegistry;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Persistence.Aggregates;

/// <summary>
/// Represents a party registry aggregate.
/// </summary>
internal class PartyRegistryAggregate
    : Aggregate<PartyRegistryAggregate, PartyRegistryEvent>
    , IAggregateFactory<PartyRegistryAggregate, PartyRegistryEvent>
    , IAggregateEventHandler<PartyRegistryCreatedEvent>
    , IAggregateEventHandler<PartyRegistryUpdatedEvent>
    , IAggregateEventHandler<PartyRegistryDeletedEvent>
    , IAggregateEventHandler<PartyRegistryResourceConnectionCreatedEvent>
    , IAggregateEventHandler<PartyRegistryResourceConnectionActionsAddedEvent>
    , IAggregateEventHandler<PartyRegistryResourceConnectionActionsRemovedEvent>
    , IAggregateEventHandler<PartyRegistryResourceConnectionDeletedEvent>
    , IAggregateEventHandler<PartyRegistryMembersAddedEvent>
    , IAggregateEventHandler<PartyRegistryMembersRemovedEvent>
{
    private bool _isDeleted;
    private string? _registryOwner;
    private string? _identifier;
    private string? _name;
    private string? _description;
    private Dictionary<string, PartyRegistryResourceConnection> _resourceConnections = new();
    private HashSet<Guid> _members = new();

    /// <inheritdoc/>
    [SuppressMessage(
        "StyleCop.CSharp.DocumentationRules", 
        "SA1648:inheritdoc should be used with inheriting class", 
        Justification = "https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3717")]
    public static PartyRegistryAggregate New(TimeProvider timeProvider, Guid id)
        => new(timeProvider, id);

    private PartyRegistryAggregate(TimeProvider timeProvider, Guid id)
        : base(timeProvider, id)
    {
    }

    /// <inheritdoc />
    public override bool IsInitialized => _registryOwner is not null;

    /// <inheritdoc />
    public override bool IsDeleted => _isDeleted;

    /// <summary>
    /// Gets the registry owner.
    /// </summary>
    public string RegistryOwner => InitializedThis._registryOwner!;

    /// <summary>
    /// Gets the registry identifier.
    /// </summary>
    public string Identifier => InitializedThis._identifier!;

    /// <summary>
    /// Gets the registry (display) name.
    /// </summary>
    public string Name => InitializedThis._name!;

    /// <summary>
    /// Gets the registry (optional) description.
    /// </summary>
    public string? Description => InitializedThis._description;

    /// <summary>
    /// Create a new party registry.
    /// </summary>
    /// <param name="registryOwner">The registry owner</param>
    /// <param name="identifier">The registry identifier</param>
    /// <param name="name">The registry (display) name</param>
    /// <param name="description">The registry (optional) description</param>
    public void Initialize(string registryOwner, string identifier, string name, string? description)
    {
        if (IsInitialized)
        {
            throw new InvalidOperationException("Aggregate already initialized");
        }

        AddEvent(new PartyRegistryCreatedEvent(EventId.Unset, Id, registryOwner, identifier, name, description ?? string.Empty, GetUtcNow()));
    }

    /// <summary>
    /// Update the party registry.
    /// </summary>
    /// <param name="identifier">The new identifier, or <see langword="null"/> to keep the old value</param>
    /// <param name="name">The new <see cref="Name"/>, or <see langword="null"/> to keep the old value</param>
    /// <param name="description">The new <see cref="Description"/>, or <see langword="null"/> to keep the old value</param>
    public void Update(
        string? identifier = null,
        string? name = null,
        string? description = null)
    {
        AssertInitialized();

        if (identifier is null
            && name is null
            && description is null)
        {
            throw new ArgumentException("At least one of the parameters must be specified");
        }

        AddEvent(new PartyRegistryUpdatedEvent(EventId.Unset, Id, identifier, name, description, GetUtcNow()));
    }

    /// <summary>
    /// Delete the party registry.
    /// </summary>
    public void Delete()
    {
        AssertInitialized();

        AddEvent(new PartyRegistryDeletedEvent(EventId.Unset, Id, GetUtcNow()));
    }

    /// <summary>
    /// Add a resource connection to the party registry.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions allow-list</param>
    public PartyRegistryResourceConnection AddResourceConnection(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertInitialized();

        if (_resourceConnections.ContainsKey(resourceIdentifier))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' already exists");
        }

        var actionsImmutable = actions.ToImmutableArray();
        if (actionsImmutable.IsDefault)
        {
            throw new ArgumentException("Actions must be specified");
        }

        AddEvent(new PartyRegistryResourceConnectionCreatedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <summary>
    /// Add actions to a resource connection.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions to add</param>
    public PartyRegistryResourceConnection AddResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertInitialized();

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

        AddEvent(new PartyRegistryResourceConnectionActionsAddedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <summary>
    /// Remove actions from a resource connection.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    /// <param name="actions">The actions to remove</param>
    public PartyRegistryResourceConnection RemoveResourceConnectionActions(string resourceIdentifier, IEnumerable<string> actions)
    {
        AssertInitialized();

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

        AddEvent(new PartyRegistryResourceConnectionActionsRemovedEvent(EventId.Unset, Id, resourceIdentifier, actionsImmutable, GetUtcNow()));
        return _resourceConnections[resourceIdentifier];
    }

    /// <summary>
    /// Remove a resource connection from the party registry.
    /// </summary>
    /// <param name="resourceIdentifier">The resource identifier</param>
    public PartyRegistryResourceConnection RemoveResourceConnection(string resourceIdentifier)
    {
        AssertInitialized();

        if (!_resourceConnections.TryGetValue(resourceIdentifier, out var connection))
        {
            throw new ArgumentException($"Resource connection for resource '{resourceIdentifier}' does not exist");
        }

        AddEvent(new PartyRegistryResourceConnectionDeletedEvent(EventId.Unset, Id, resourceIdentifier, GetUtcNow()));
        return connection;
    }

    /// <summary>
    /// Add members to the party registry.
    /// </summary>
    /// <param name="partyIds">The members</param>
    public void AddMembers(IEnumerable<Guid> partyIds)
    {
        AssertInitialized();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (partyIdsImmutable.Any(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs already exist in the registry", nameof(partyIds));
        }

        AddEvent(new PartyRegistryMembersAddedEvent(EventId.Unset, Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <summary>
    /// Remove members from the party registry.
    /// </summary>
    /// <param name="partyIds">The members</param>
    public void RemoveMembers(IEnumerable<Guid> partyIds)
    {
        AssertInitialized();

        var partyIdsImmutable = partyIds.ToImmutableArray();
        if (partyIdsImmutable.IsDefault)
        {
            throw new ArgumentException("Party IDs must be specified", nameof(partyIds));
        }

        if (!partyIdsImmutable.All(_members.Contains))
        {
            throw new ArgumentException("One or more party IDs do not exist in the registry", nameof(partyIds));
        }

        AddEvent(new PartyRegistryMembersRemovedEvent(EventId.Unset, Id, partyIdsImmutable, GetUtcNow()));
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryCreatedEvent>.ApplyEvent(PartyRegistryCreatedEvent @event)
    {
        _registryOwner = @event.RegistryOwner;
        _identifier = @event.Identifier;
        _name = @event.Name;
        _description = @event.Description;
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryUpdatedEvent>.ApplyEvent(PartyRegistryUpdatedEvent @event)
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
    void IAggregateEventHandler<PartyRegistryDeletedEvent>.ApplyEvent(PartyRegistryDeletedEvent @event)
    {
        _isDeleted = true;
    }

    /// <inheritdoc />
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Collection initializer")]
    void IAggregateEventHandler<PartyRegistryResourceConnectionCreatedEvent>.ApplyEvent(PartyRegistryResourceConnectionCreatedEvent @event)
    {
        _resourceConnections[@event.ResourceIdentifier] = new PartyRegistryResourceConnection(@event.ResourceIdentifier, [.. @event.Actions], @event.EventTime, @event.EventTime);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryResourceConnectionActionsAddedEvent>.ApplyEvent(PartyRegistryResourceConnectionActionsAddedEvent @event)
    {
        if (_resourceConnections.TryGetValue(@event.ResourceIdentifier, out var connection))
        {
            connection = connection with { Actions = connection.Actions.Union(@event.Actions), Modified = @event.EventTime };
            _resourceConnections[connection.ResourceIdentifier] = connection;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryResourceConnectionActionsRemovedEvent>.ApplyEvent(PartyRegistryResourceConnectionActionsRemovedEvent @event)
    {
        if (_resourceConnections.TryGetValue(@event.ResourceIdentifier, out var connection))
        {
            connection = connection with { Actions = connection.Actions.Except(@event.Actions), Modified = @event.EventTime };
            _resourceConnections[connection.ResourceIdentifier] = connection;
        }
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryResourceConnectionDeletedEvent>.ApplyEvent(PartyRegistryResourceConnectionDeletedEvent @event)
    {
        _resourceConnections.Remove(@event.ResourceIdentifier);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryMembersAddedEvent>.ApplyEvent(PartyRegistryMembersAddedEvent @event)
    {
        _members.UnionWith(@event.PartyIds);
    }

    /// <inheritdoc />
    void IAggregateEventHandler<PartyRegistryMembersRemovedEvent>.ApplyEvent(PartyRegistryMembersRemovedEvent @event)
    {
        _members.ExceptWith(@event.PartyIds);
    }

    /// <summary>
    /// Gets the aggregate as a <see cref="PartyRegistryInfo"/>.
    /// </summary>
    /// <returns><see cref="PartyRegistryInfo"/></returns>
    public PartyRegistryInfo AsRegistryInfo()
        => new PartyRegistryInfo(Id, RegistryOwner, Identifier, Name, Description, CreatedAt, UpdatedAt);
}

/// <summary>
/// A event id that is either a db-defined id, or the special value unset.
/// </summary>
/// <param name="id">The db id</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal readonly struct EventId(ulong id)
{
    /// <summary>
    /// Get's the nullable value for the event id.
    /// </summary>
    public readonly ulong? Value 
        => id == 0 ? null : id;

    /// <summary>
    /// Get's a value for the event id, assuming it is not unset.
    /// </summary>
    internal readonly ulong UnsafeValue
        => id;

    /// <summary>
    /// Gets whether the value is set.
    /// </summary>
    public bool IsSet => id != 0;

    /// <summary>
    /// Gets the special unset event id.
    /// </summary>
    public static EventId Unset => default;

    [DebuggerHidden]
    private string DebuggerDisplay
        => IsSet ? id.ToString(CultureInfo.InvariantCulture) : "unset";
}

internal abstract record PartyRegistryEvent(EventId EventId, Guid RegistryId, DateTimeOffset EventTime)
    : IAggregateEvent<PartyRegistryAggregate, PartyRegistryEvent>
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
    protected abstract void ApplyTo(PartyRegistryAggregate aggregate);

    /// <inheritdoc />
    void IAggregateEvent<PartyRegistryAggregate, PartyRegistryEvent>.ApplyTo(PartyRegistryAggregate aggregate)
        => ApplyTo(aggregate);

    /// <summary>
    /// Gets the event as a set of values used for persistence.
    /// </summary>
    /// <returns><see cref="Values"/></returns>
    internal abstract Values AsValues();

    internal readonly record struct Values(
        string Kind,
        DateTimeOffset EventTime,
        Guid AggregateId,
        string? Identifier,
        string? Name,
        string? Description,
        string? RegistryOwner,
        ImmutableArray<string> Actions,
        ImmutableArray<Guid> PartyIds);
}

internal record PartyRegistryCreatedEvent(
    EventId EventId,
    Guid RegistryId,
    string RegistryOwner,
    string Identifier,
    string Name,
    string Description,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryCreatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_created",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            RegistryOwner: RegistryOwner,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryUpdatedEvent(
    EventId EventId,
    Guid RegistryId,
    string? Identifier,
    string? Name,
    string? Description,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryUpdatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_updated",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: Identifier,
            Name: Name,
            Description: Description,
            RegistryOwner: null,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryDeletedEvent(
    EventId EventId,
    Guid RegistryId,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryDeletedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "registry_deleted",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryResourceConnectionCreatedEvent(
    EventId EventId,
    Guid RegistryId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryResourceConnectionCreatedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_created",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: Actions,
            PartyIds: default);
}

internal record PartyRegistryResourceConnectionActionsAddedEvent(
    EventId EventId,
    Guid RegistryId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryResourceConnectionActionsAddedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_actions_added",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: Actions,
            PartyIds: default);
}

internal record PartyRegistryResourceConnectionActionsRemovedEvent(
    EventId EventId,
    Guid RegistryId,
    string ResourceIdentifier,
    ImmutableArray<string> Actions,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryResourceConnectionActionsRemovedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_actions_removed",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: Actions,
            PartyIds: default);
}

internal record PartyRegistryResourceConnectionDeletedEvent(
    EventId EventId,
    Guid RegistryId,
    string ResourceIdentifier,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryResourceConnectionDeletedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "resource_connection_deleted",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: ResourceIdentifier,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: default);
}

internal record PartyRegistryMembersAddedEvent(
    EventId EventId,
    Guid RegistryId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryMembersAddedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_added",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}

internal record PartyRegistryMembersRemovedEvent(
    EventId EventId,
    Guid RegistryId,
    ImmutableArray<Guid> PartyIds,
    DateTimeOffset EventTime)
    : PartyRegistryEvent(EventId, RegistryId, EventTime)
{
    /// <inheritdoc />
    protected override void ApplyTo(PartyRegistryAggregate aggregate)
        => ((IAggregateEventHandler<PartyRegistryMembersRemovedEvent>)aggregate).ApplyEvent(this);

    /// <inheritdoc />
    internal override Values AsValues()
        => new Values(
            Kind: "members_removed",
            EventTime: EventTime,
            AggregateId: RegistryId,
            Identifier: null,
            Name: null,
            Description: null,
            RegistryOwner: null,
            Actions: default,
            PartyIds: PartyIds);
}
