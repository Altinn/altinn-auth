using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Altinn.ResourceRegistry.Core.PartyRegistry;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// Repository for party registries.
/// </summary>
internal partial class PartyRegistryRepository 
    : IPartyRegistryRepository
{
    private readonly NpgsqlDataSource _conn;
    private readonly ILogger _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartyRegistryRepository"/> class
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="logger">Logger</param>
    /// <param name="timeProvider">Time provider</param>
    public PartyRegistryRepository(
        NpgsqlDataSource conn,
        ILogger<ResourceRegistryRepository> logger,
        TimeProvider timeProvider)
    {
        _conn = conn;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnection(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyResourceConnection(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnection(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyResourceConnection(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnectionActions(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyResourceConnectionActions(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> AddPartyResourceConnectionActions(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyResourceConnectionActions(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> CreatePartyRegistry(string registryOwner, string identifier, string name, string description, CancellationToken cancellationToken = default) 
        => InTransaction(tx => tx.CreatePartyRegistry(registryOwner, identifier, name, description, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> DeletePartyRegistry(Guid id, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Delete(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> DeletePartyRegistry(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Delete(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> DeletePartyResourceConnection(Guid id, string resourceIdentifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.DeletePartyResourceConnection(new(id), resourceIdentifier, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> DeletePartyResourceConnection(string registryOwner, string identifier, string resourceIdentifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.DeletePartyResourceConnection(new(registryOwner, identifier), resourceIdentifier, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PartyRegistryResourceConnection>> GetResourceConnections(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.GetResourceConnections(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PartyRegistryResourceConnection>> GetResourceConnections(Guid id, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.GetResourceConnections(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo?> Lookup(Guid id, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.Lookup(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.Lookup(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> UpdatePartyRegistry(Guid id, string? newIdentifier, string? newName, string? newDescription, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Update(new(id), newIdentifier, newName, newDescription, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryInfo> UpdatePartyRegistry(string registryOwner, string identifier, string? newIdentifier, string? newName, string? newDescription, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.Update(new(registryOwner, identifier), newIdentifier, newName, newDescription, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> RemovePartyResourceConnectionActions(Guid id, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemovePartyResourceConnectionActions(new(id), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<PartyRegistryResourceConnection> RemovePartyResourceConnectionActions(string registryOwner, string identifier, string resourceIdentifier, IEnumerable<string> actions, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemovePartyResourceConnectionActions(new(registryOwner, identifier), resourceIdentifier, actions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PartyRegistryMembership>> GetPartyRegistryMemberships(Guid id, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.GetPartyRegistryMemberships(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PartyRegistryMembership>> GetPartyRegistryMemberships(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => TransactionLess(tx => tx.GetPartyRegistryMemberships(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task AddPartyRegistryMembers(Guid id, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyRegistryMembers(new(id), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task AddPartyRegistryMembers(string registryOwner, string identifier, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.AddPartyRegistryMembers(new(registryOwner, identifier), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task RemovePartyRegistryMembers(Guid id, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemovePartyRegistryMembers(new(id), partyIds, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task RemovePartyRegistryMembers(string registryOwner, string identifier, IEnumerable<Guid> partyIds, CancellationToken cancellationToken = default)
        => InTransaction(tx => tx.RemovePartyRegistryMembers(new(registryOwner, identifier), partyIds, cancellationToken), cancellationToken);

    private Task<T> InTransaction<T>(Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        => ScopedRepository.RunInTransaction(this, func, cancellationToken);

    private Task<T> TransactionLess<T>(Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        => ScopedRepository.TransactionLess(this, func, cancellationToken);

    private readonly record struct PartyRegistryIdentifier(Guid RegistryId, string? Owner, string? Identifier)
    {
        public PartyRegistryIdentifier(Guid registryId)
            : this(registryId, null, null)
        {
        }

        public PartyRegistryIdentifier(string owner, string identifier)
            : this(Guid.Empty, owner, identifier)
        { 
        }

        public bool IsEmpty => RegistryId == Guid.Empty && Owner is null && Identifier is null;

        public bool IsRegistryId => RegistryId != Guid.Empty;

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
