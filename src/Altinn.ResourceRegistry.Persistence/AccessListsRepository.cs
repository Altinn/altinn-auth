using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Persistence.Aggregates;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// Repository for access lists.
/// </summary>
internal partial class AccessListsRepository 
    : IAccessListsRepository
    , IAggregateRepository<AccessListAggregate, AccessListEvent>
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
    public Task<IReadOnlyList<AccessListInfo>> GetAccessListsByOwner(string resourceOwner, string? continueFrom, int count, AccessListIncludes includes = default, CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.GetAccessListsByOwner(resourceOwner, continueFrom, count, includes, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo?> LookupInfo(Guid id, AccessListIncludes includes = default, CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.LookupInfo(new(id), includes, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListInfo?> LookupInfo(string resourceOwner, string identifier, AccessListIncludes includes = default, CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.LookupInfo(new(resourceOwner, identifier), includes, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListData<IReadOnlyList<AccessListResourceConnection>>?> GetAccessListResourceConnections(
        Guid id,
        string? continueFrom,
        int count,
        bool includeActions,
        CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.GetAccessListResourceConnections(new(id), continueFrom, count, includeActions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListData<IReadOnlyList<AccessListResourceConnection>>?> GetAccessListResourceConnections(
        string registryOwner,
        string identifier,
        string? continueFrom,
        int count,
        bool includeActions,
        CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.GetAccessListResourceConnections(new(registryOwner, identifier), continueFrom, count, includeActions, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListData<IReadOnlyList<AccessListMembership>>?> GetAccessListMemberships(
        Guid id,
        CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.GetAccessListMemberships(new(id), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public Task<AccessListData<IReadOnlyList<AccessListMembership>>?> GetAccessListMemberships(
        string registryOwner,
        string identifier,
        CancellationToken cancellationToken = default)
        => InTransaction(repo => repo.GetAccessListMemberships(new(registryOwner, identifier), cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public async Task<IAccessListAggregate> CreateAccessList(string resourceOwner, string identifier, string name, string description, CancellationToken cancellationToken = default)
        => await InTransaction(repo => repo.CreateAccessList(resourceOwner, identifier, name, description, cancellationToken), cancellationToken);

    /// <inheritdoc/>
    public async Task<AccessListLoadOrCreateResult> LoadOrCreateAccessList(
        string resourceOwner,
        string identifier,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var result = await InTransaction(repo => repo.LoadOrCreateAccessList(resourceOwner, identifier, name, description, cancellationToken), cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        throw new UnreachableException("This should not happen. Failed to load/create an access list");
    }

    /// <inheritdoc/>
    public async Task<IAccessListAggregate?> LoadAccessList(Guid id, CancellationToken cancellationToken = default)
        => await Load(new AccessListIdentifier(id), cancellationToken);

    /// <inheritdoc/>
    public async Task<IAccessListAggregate?> LoadAccessList(string registryOwner, string identifier, CancellationToken cancellationToken = default)
        => await Load(new AccessListIdentifier(registryOwner, identifier), cancellationToken);

    /// <inheritdoc/>
    Task<AccessListAggregate?> IAggregateRepository<AccessListAggregate, AccessListEvent>.Load(Guid id, CancellationToken cancellationToken)
        => Load(new AccessListIdentifier(id), cancellationToken);

    /// <inheritdoc/>
    Task IAggregateRepository<AccessListAggregate, AccessListEvent>.ApplyChanges(AccessListAggregate aggregate, CancellationToken cancellationToken)
        => InTransaction(repo => repo.ApplyChanges(aggregate, cancellationToken), cancellationToken);

    private Task<AccessListAggregate?> Load(AccessListIdentifier identifier, CancellationToken cancellationToken)
        => InTransaction(repo => repo.Load(identifier, cancellationToken), cancellationToken);

    private Task<T> InTransaction<T>(Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        => ScopedRepository.RunInTransaction(this, func, cancellationToken);

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

        [MemberNotNullWhen(true, nameof(Owner), nameof(Identifier))]
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
