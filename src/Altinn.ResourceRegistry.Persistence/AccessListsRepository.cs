using Altinn.ResourceRegistry.Core.AccessLists;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// Repository for access lists.
/// </summary>
internal partial class AccessListsRepository 
    : IAccessListsRepository
{
    private readonly NpgsqlDataSource _conn;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessListsRepository"/> class
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="logger">Logger</param>
    /// <param name="timeProvider">Time provider</param>
    public AccessListsRepository(
        NpgsqlDataSource conn,
        ILogger<ResourceRegistryRepository> logger,
        TimeProvider timeProvider)
    {
        _conn = conn;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> AddAccessListResourceConnection(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListResourceConnection(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> AddAccessListResourceConnection(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListResourceConnection(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> AddAccessListResourceConnectionActions(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListResourceConnectionActions(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> AddAccessListResourceConnectionActions(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListResourceConnectionActions(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo> CreateAccessList(string registryOwner, string identifier, string name, string description, CancellationToken cancellationToken = default) 
        => InTransaction(tx => tx.CreateAccessList(registryOwner, identifier, name, description, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo> DeleteAccessList(Guid id, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Delete(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo> DeleteAccessList(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Delete(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> DeleteAccessListResourceConnection(Guid id, string resourceIdentifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.DeleteAccessListResourceConnection(new(id), resourceIdentifier, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> DeleteAccessListResourceConnection(string registryOwner, string identifier, string resourceIdentifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.DeleteAccessListResourceConnection(new(registryOwner, identifier), resourceIdentifier, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.GetAccessListResourceConnections(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(Guid id, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.GetAccessListResourceConnections(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo?> Lookup(Guid id, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.Lookup(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.Lookup(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo> UpdateAccessList(Guid id, string? newIdentifier, string? newName, string? newDescription, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Update(new(id), newIdentifier, newName, newDescription, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo> UpdateAccessList(string registryOwner, string identifier, string? newIdentifier, string? newName, string? newDescription, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Update(new(registryOwner, identifier), newIdentifier, newName, newDescription, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> RemoveAccessListResourceConnectionActions(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemoveAccessListResourceConnectionActions(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListResourceConnection> RemoveAccessListResourceConnectionActions(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemoveAccessListResourceConnectionActions(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(Guid id, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.GetAccessListMemberships(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.GetAccessListMemberships(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task AddAccessListMembers(Guid id, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListMembers(new(id), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task AddAccessListMembers(string registryOwner, string identifier, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddAccessListMembers(new(registryOwner, identifier), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAccessListMembers(Guid id, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemoveAccessListMembers(new(id), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAccessListMembers(string registryOwner, string identifier, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemoveAccessListMembers(new(registryOwner, identifier), partyIds, cancellationToken), cancellationToken);

    private Task<T> InTransaction<T>(Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        => ScopedRepository.RunInTransaction(this, func, cancellationToken);

    private Task<T> TransactionLess<T>(Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        => ScopedRepository.TransactionLess(this, func, cancellationToken);

    private readonly record struct AccessListIdentifier(Guid AccessListId, string? Owner, string? Identifier)
    {
        public AccessListIdentifier(Guid registryId)
            : this(registryId, null, null)
        {
        }

        public AccessListIdentifier(string owner, string identifier)
            : this(Guid.Empty, owner, identifier)
        { 
        }

        public bool IsEmpty => AccessListId == Guid.Empty && Owner is null && Identifier is null;

        public bool IsAccessListId => AccessListId != Guid.Empty;

        public bool IsOwnerAndIdentifier => Owner is not null && Identifier is not null;
    }

    /// <summary>
    /// A unit type. This acts as <see langword="void"/> for generic types.
    /// </summary>
    private readonly struct Unit
    {
        public static Unit Value { get; } = default;
    }
}
