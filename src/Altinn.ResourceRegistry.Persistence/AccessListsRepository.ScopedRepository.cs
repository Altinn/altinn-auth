using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Persistence.Aggregates;
using Altinn.ResourceRegistry.Persistence.Extensions;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using EventId = Altinn.ResourceRegistry.Core.Aggregates.EventId;

namespace Altinn.ResourceRegistry.Persistence;

/// <content>
/// Contains the <see cref="ScopedRepository"/> class for doing operations
/// within the scope of a transaction.
/// </content>
internal partial class AccessListsRepository
{
    private sealed class ScopedRepository
    {
        private const string UNIQUE_OWNER_IDENT_CONSTRAINT_NAME = "uq_access_list_state_owner_ident";

        public static async Task<T> RunInTransaction<T>(
            AccessListsRepository repo,
            Func<ScopedRepository, Task<T>> func,
            CancellationToken cancellationToken)
        {
            await using var conn = await repo._conn.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

            var result = await func(new ScopedRepository(repo, conn, tx));
            await tx.CommitAsync(cancellationToken);

            return result;
        }

        private readonly ILogger _logger;
        private readonly NpgsqlConnection _conn;
        private readonly NpgsqlTransaction _tx;
        private readonly TimeProvider _timeProvider;
        private readonly IAggregateRepository<AccessListAggregate, AccessListEvent> _repo;

        private ScopedRepository(AccessListsRepository repo, NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            _conn = conn;
            _tx = tx;
            _logger = repo._logger;
            _timeProvider = repo._timeProvider;
            _repo = repo;
        }

        public async Task<AccessListAggregate> CreateAccessList(string resourceOwner, string identifier, string name, string description, CancellationToken cancellationToken)
        {
            // Step 1. Create the aggregate
            var accessListAggregate = AccessListAggregate.New(_timeProvider, Guid.NewGuid(), _repo);
            accessListAggregate.Initialize(resourceOwner, identifier, name, description);

            // Step 2. Try to save the aggregate - catching the exception that's thrown if the list already exist
            try
            {
                await ApplyChanges(accessListAggregate, cancellationToken);
            }
            catch (PostgresException e) when (e.ConstraintName is UNIQUE_OWNER_IDENT_CONSTRAINT_NAME)
            {
                ThrowHelper.ThrowInvalidOperationException("Access list already exists", e);
            }

            // Step 3. Return the access list info
            return accessListAggregate;
        }

        public async Task<AccessListLoadOrCreateResult?> LoadOrCreateAccessList(
            string resourceOwner,
            string identifier,
            string name,
            string description,
            CancellationToken cancellationToken = default)
        {
            const string SAVEPOINT_NAME = nameof(LoadOrCreateAccessList);

            AccessListAggregate aggregate;
            AccessListLoadOrCreateResult.ResultMode mode;

            // Try to create the access list
            try
            {
                // We expect that the following call to CreateAccessList *might* fail with a database constraint violation,
                // so we wrap it in a savepoint to be able to rollback to the savepoint if it fails.
                await _tx.SaveAsync(SAVEPOINT_NAME, cancellationToken);

                aggregate = await CreateAccessList(resourceOwner, identifier, name, description, cancellationToken);
                await _tx.ReleaseAsync(SAVEPOINT_NAME, cancellationToken);

                mode = AccessListLoadOrCreateResult.ResultMode.Created;
            }
            catch (InvalidOperationException e) when (e.InnerException is PostgresException { ConstraintName: UNIQUE_OWNER_IDENT_CONSTRAINT_NAME })
            {
                await _tx.RollbackAsync(SAVEPOINT_NAME, cancellationToken);

                // If it fails because we already have the list created, load it instead
                var loaded = await Load(new(resourceOwner, identifier), cancellationToken);
                mode = AccessListLoadOrCreateResult.ResultMode.Loaded;

                if (loaded is null)
                {
                    // We've hit an optimistic concurrency case. We need to start a new transaction to be able to
                    // get the correct data. Return `null` to indicate to caller that it should call again.
                    return null;
                }

                aggregate = loaded;
            }

            return new AccessListLoadOrCreateResult(mode, aggregate);
        }

        public async Task<AccessListInfo?> LookupInfo(AccessListIdentifier identifier, AccessListIncludes includes, CancellationToken cancellationToken)
        {
            const string BY_ID_QUERY = /*strpsql*/@"
                SELECT aggregate_id, identifier, resource_owner, name, description, created, modified, version
                FROM resourceregistry.access_list_state
                WHERE aggregate_id = @aggregate_id;";

            const string BY_OWNER_AND_IDENT_QUERY = /*strpsql*/@"
                SELECT aggregate_id, identifier, resource_owner, name, description, created, modified, version
                FROM resourceregistry.access_list_state
                WHERE resource_owner = @owner AND identifier = @identifier;";

            await using var cmd = identifier switch
            {
                { IsAccessListId: true } => ByIdQuery(_conn, identifier.AccessListId),
                { IsOwnerAndIdentifier: true } => ByOwnerAndIdentQuery(_conn, identifier.Owner, identifier.Identifier),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

            await cmd.PrepareAsync(cancellationToken);
            var accessList = await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreateAccessListInfo)
                .FirstOrDefaultAsync(cancellationToken);

            if (accessList is null)
            {
                return null;
            }

            if (includes.HasFlag(AccessListIncludes.ResourceConnections))
            {
                var connections = await GetAccessListResourceConnections(
                    accessList.Id,
                    limit: null,
                    includeActions: includes.HasFlag(AccessListIncludes.ResourceConnectionsActions),
                    cancellationToken);

                accessList = accessList with { ResourceConnections = connections };
            }

            return accessList;

            static NpgsqlCommand ByIdQuery(NpgsqlConnection conn, Guid accessListId)
            {
                var cmd = conn.CreateCommand(BY_ID_QUERY);
                cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, accessListId);
                return cmd;
            }

            static NpgsqlCommand ByOwnerAndIdentQuery(NpgsqlConnection conn, string owner, string identifier)
            {
                var cmd = conn.CreateCommand(BY_OWNER_AND_IDENT_QUERY);
                cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, owner);
                cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);
                return cmd;
            }
        }

        public async Task<IReadOnlyList<AccessListInfo>> GetAccessListsByOwner(string resourceOwner, string? continueFrom, int count, AccessListIncludes includes, string? resourceIdentifier, CancellationToken cancellationToken)
        {
            Guard.IsNotNullOrEmpty(resourceOwner);
            Guard.IsGreaterThan(count, 0);

            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id, identifier, resource_owner, name, description, created, modified, version
                FROM resourceregistry.access_list_state
                WHERE resource_owner = @resource_owner
                AND (@continue_from IS NULL OR identifier >= @continue_from)
                ORDER BY identifier ASC
                LIMIT @count;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("resource_owner", NpgsqlDbType.Text, resourceOwner);
            cmd.Parameters.AddWithNullableValue("continue_from", NpgsqlDbType.Text, continueFrom);
            cmd.Parameters.AddWithValue("count", NpgsqlDbType.Integer, count);

            await cmd.PrepareAsync(cancellationToken);
            var accessLists = await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreateAccessListInfo)
                .ToListAsync(cancellationToken);

            List<Guid>? idSet = null;
            if (includes.HasFlag(AccessListIncludes.ResourceConnections))
            {
                idSet ??= accessLists.Select(info => info.Id).ToList();
                await LoadResourceConnections(
                    accessLists,
                    idSet,
                    resourceIdentifier,
                    includeActions: includes.HasFlag(AccessListIncludes.ResourceConnectionsActions),
                    cancellationToken);
            }

            return accessLists;
        }

        public async Task<AccessListData<IReadOnlyList<AccessListResourceConnection>>?> GetAccessListResourceConnections(
            AccessListIdentifier identifier,
            string? continueFrom,
            int count,
            bool includeActions, 
            CancellationToken cancellationToken)
        {
            var metadata = await LookupInfo(identifier, AccessListIncludes.None, cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            var connections = await GetAccessListResourceConnections(metadata.Id, (continueFrom, count), includeActions, cancellationToken);
            return AccessListData.Create(metadata, connections);
        }

        public async Task<AccessListData<IReadOnlyList<AccessListMembership>>?> GetAccessListMemberships(
            AccessListIdentifier identifier,
            Guid? continueFrom,
            int count,
            CancellationToken cancellationToken)
        {
            var metadata = await LookupInfo(identifier, AccessListIncludes.None, cancellationToken);
            if (metadata is null)
            {
                return null;
            }

            var memberships = await GetAccessListMemberships(metadata.Id, continueFrom, count, cancellationToken);
            return AccessListData.Create(metadata, memberships);
        }

        public async Task<AccessListAggregate?> Load(
            AccessListIdentifier identifier,
            CancellationToken cancellationToken)
        {
            var accessListId = await GetRegistryId(identifier, cancellationToken);
            if (accessListId is null)
            {
                return null;
            }

            var events = LoadEvents(accessListId.Value, cancellationToken);
            var aggregate = await IAggregateFactory<AccessListAggregate, AccessListEvent>.LoadFrom(_timeProvider, accessListId.Value, _repo, events);
            if (!aggregate.IsInitialized)
            {
                return null;
            }

            return aggregate;
        }

        public async Task<int> ApplyChanges(AccessListAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_events (etime, kind, aggregate_id, identifier, name, description, resource_owner, actions, party_ids)
                VALUES (@etime, @kind, @aggregate_id, @identifier, @name, @description, @resource_owner, @actions, @party_ids)
                RETURNING eid;";

            // If there are no uncommitted events, return early
            if (!aggregate.HasUncommittedEvents)
            {
                return 0;
            }

            // Step 1. Insert all events into the database.
            var newIds = new List<(AccessListEvent Evt, EventId Id)>();
            var version = aggregate.CommittedVersion;
            {
                await using var cmd = _conn.CreateCommand(QUERY);
                var etime = cmd.Parameters.Add("etime", NpgsqlDbType.TimestampTz);
                var kind = cmd.Parameters.Add("kind", NpgsqlDbType.Unknown);
                var aggregate_id = cmd.Parameters.Add("aggregate_id", NpgsqlDbType.Uuid);
                var identifier = cmd.Parameters.Add("identifier", NpgsqlDbType.Text);
                var name = cmd.Parameters.Add("name", NpgsqlDbType.Text);
                var description = cmd.Parameters.Add("description", NpgsqlDbType.Text);
                var resource_owner = cmd.Parameters.Add("resource_owner", NpgsqlDbType.Text);
                var actions = cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text);
                var party_ids = cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid);

                foreach (var evt in aggregate.GetUncommittedEvents())
                {
                    var values = evt.AsValues();
                    etime.SetValue(values.EventTime);
                    kind.SetValue(values.Kind);
                    aggregate_id.SetValue(values.AggregateId);
                    identifier.SetNullableValue(values.Identifier);
                    name.SetNullableValue(values.Name);
                    description.SetNullableValue(values.Description);
                    resource_owner.SetNullableValue(values.ResourceOwner);
                    actions.SetOptionalImmutableArrayValue(values.Actions);
                    party_ids.SetOptionalImmutableArrayValue(values.PartyIds);

                    await cmd.PrepareAsync(cancellationToken);
                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new InvalidOperationException("No event id returned from database.");
                    }

                    version = new EventId((ulong)reader.GetFieldValue<long>("eid"));
                    newIds.Add((evt, version));
                }
            }

            // Step 2. Update state tables in the database.
            foreach (var evt in aggregate.GetUncommittedEvents())
            {
                var applyTask = evt switch
                {
                    AccessListCreatedEvent create => ApplyCreateStateChecked(create, aggregate, cancellationToken),
                    AccessListUpdatedEvent update => ApplyUpdateState(update, cancellationToken),
                    AccessListDeletedEvent delete => ApplyDeleteStateChecked(delete, aggregate, cancellationToken),
                    AccessListResourceConnectionCreatedEvent connectionSet => ApplyResourceConnectionState(connectionSet, cancellationToken),
                    AccessListResourceConnectionActionsAddedEvent connectionActionsAdded => ApplyResourceConnectionState(connectionActionsAdded, cancellationToken),
                    AccessListResourceConnectionActionsRemovedEvent connectionActionsRemoved => ApplyResourceConnectionState(connectionActionsRemoved, cancellationToken),
                    AccessListResourceConnectionDeletedEvent connectionDeleted => ApplyResourceConnectionState(connectionDeleted, cancellationToken),
                    AccessListMembersAddedEvent membersAdded => ApplyMembersAddedState(membersAdded, cancellationToken),
                    AccessListMembersRemovedEvent membersRemoved => ApplyMembersRemovedState(membersRemoved, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown event type '{evt.GetType().Name}'")
                };

                await applyTask;
            }

            // Step 3. Update modified timestamp on the access list state table.
            if (!aggregate.IsDeleted)
            {
                await UpdateModifiedAtAndVersionChecked(aggregate, version, cancellationToken);
            }

            // Step 4. Mark all events as commited.
            foreach (var (evt, id) in newIds)
            {
                evt.EventId = id;
            }

            aggregate.Commit();
            return newIds.Count;
        }

        private async Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(
            Guid id, 
            (string? ContinueFrom, int Count)? limit,
            bool includeActions, 
            CancellationToken cancellationToken)
        {
            const string ALL_QUERY = /*strpsql*/@"
                SELECT resource_identifier, actions, created, modified
                FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = @aggregate_id;";

            const string LIMIT_QUERY = /*strpsql*/@"
                SELECT resource_identifier, actions, created, modified
                FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = @aggregate_id
                AND (@continue_from IS NULL OR resource_identifier >= @continue_from)
                ORDER BY resource_identifier ASC
                LIMIT @count;";

            await using var cmd = limit switch {
                null => GetAll(_conn, id),
                var (continueFrom, count) => GetLimited(_conn, id, continueFrom, count),
            };

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var connections = new List<AccessListResourceConnection>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var connection = CreateResourceConnection(reader, includeActions);
                connections.Add(connection);
            }

            return connections;

            static NpgsqlCommand GetAll(NpgsqlConnection conn, Guid accessListId)
            {
                var cmd = conn.CreateCommand(ALL_QUERY);
                cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, accessListId);
                return cmd;
            }

            static NpgsqlCommand GetLimited(NpgsqlConnection conn, Guid accessListId, string? continueFrom, int count)
            {
                var cmd = conn.CreateCommand(LIMIT_QUERY);
                cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, accessListId);
                cmd.Parameters.AddWithNullableValue("continue_from", NpgsqlDbType.Text, continueFrom);
                cmd.Parameters.AddWithValue("count", NpgsqlDbType.Integer, count);
                return cmd;
            }
        }

        private async Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(Guid id, Guid? continueFrom, int count, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT party_id, since
                FROM resourceregistry.access_list_members_state
                WHERE aggregate_id = @aggregate_id
                AND (@continue_from IS NULL OR party_id >= @continue_from)
                ORDER BY party_id ASC
                LIMIT @count;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, id);
            cmd.Parameters.AddWithNullableValue("continue_from", NpgsqlDbType.Uuid, continueFrom);
            cmd.Parameters.AddWithValue("count", NpgsqlDbType.Integer, count);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var memberships = new List<AccessListMembership>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var partyId = reader.GetGuid("party_id");
                var since = reader.GetFieldValue<DateTimeOffset>("since");

                var membership = new AccessListMembership(partyId, since);
                memberships.Add(membership);
            }

            return memberships;
        }

        private async IAsyncEnumerable<AccessListEvent> LoadEvents(
            Guid accessListId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT eid, etime, kind, aggregate_id, identifier, name, description, resource_owner, actions, party_ids
                FROM resourceregistry.access_list_events
                WHERE aggregate_id = @aggregate_id
                ORDER BY eid ASC;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, accessListId);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return await CreateAccessListEvent(reader, cancellationToken);
            }
        }

        private ValueTask<Guid?> GetRegistryId(AccessListIdentifier identifier, CancellationToken cancellationToken)
            => identifier switch
            {
                { IsAccessListId: true } => ValueTask.FromResult<Guid?>(identifier.AccessListId),
                { IsOwnerAndIdentifier: true } => new ValueTask<Guid?>(GetRegistryId(identifier.Owner, identifier.Identifier, cancellationToken)),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

        private async Task<Guid?> GetRegistryId(string owner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id
                FROM resourceregistry.access_list_state
                WHERE resource_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, owner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return reader.GetGuid(0);
        }

        private async Task LoadResourceConnections(List<AccessListInfo> accessLists, List<Guid> idSet, string? resourceIdentifier, bool includeActions, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id, resource_identifier, actions, created, modified
                FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = ANY(@aggregate_ids)
                AND (@resource_identifier IS NULL OR resource_identifier = @resource_identifier)
                ORDER BY aggregate_id, resource_identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.Add<IList<Guid>>("aggregate_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).TypedValue = idSet;
            cmd.Parameters.AddWithNullableValue("resource_identifier", NpgsqlDbType.Text, resourceIdentifier);

            AccessListInfo? current = null;
            List<AccessListResourceConnection>? connections = null;

            // Note: We've sorted the result set by aggregate_id, so we can assume that the rows are ordered by aggregate_id.
            await cmd.PrepareAsync(cancellationToken);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var aggregateId = reader.GetGuid("aggregate_id");

                if (current is null || current.Id != aggregateId)
                {
                    var index = accessLists.FindIndex(info => info.Id == aggregateId);
                    if (index < 0)
                    {
                        Debug.Fail("Access list not found");
                        continue;
                    }

                    connections = new List<AccessListResourceConnection>();
                    current = accessLists[index] = accessLists[index] with { ResourceConnections = connections };
                }

                var resourceConnection = CreateResourceConnection(reader, includeActions);
                connections!.Add(resourceConnection);
            }

            for (var i = 0; i < accessLists.Count; i++)
            {
                if (accessLists[i].ResourceConnections is null)
                {
                    accessLists[i] = accessLists[i] with { ResourceConnections = Array.Empty<AccessListResourceConnection>() };
                }
            }
        }

        private async Task ApplyCreateStateChecked(AccessListCreatedEvent evt, AccessListAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_state
                (aggregate_id, identifier, resource_owner, name, description, created, modified, version)
                VALUES(@aggregate_id, @identifier, @resource_owner, @name, @description, @created, @modified, @version);";

            Debug.Assert(!aggregate.CommittedVersion.IsSet, "Aggregate version should not be set.");

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithValue("resource_owner", NpgsqlDbType.Text, evt.ResourceOwner);
            cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithValue("description", NpgsqlDbType.Text, evt.Description);
            cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, evt.EventTime);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, evt.EventTime);
            cmd.Parameters.AddWithValue("version", NpgsqlDbType.Bigint, 0L);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyUpdateState(AccessListUpdatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.access_list_state
                SET identifier = COALESCE(@identifier, identifier),
                    name = COALESCE(@name, name),
                    description = COALESCE(@description, description)
                WHERE aggregate_id = @aggregate_id;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithNullableValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithNullableValue("name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithNullableValue("description", NpgsqlDbType.Text, evt.Description);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyDeleteStateChecked(AccessListDeletedEvent evt, AccessListAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.access_list_state
                WHERE aggregate_id = @aggregate_id
                AND version = @version;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("version", NpgsqlDbType.Bigint, aggregate.CommittedVersion.DbValue);

            await cmd.PrepareAsync(cancellationToken);
            var updatedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (updatedRows == 0)
            {
                throw OptimisticConcurrencyException.Create(aggregate);
            }
        }

        private async Task ApplyResourceConnectionState(AccessListResourceConnectionCreatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_resource_connections_state
                (aggregate_id, resource_identifier, actions, created, modified)
                VALUES(@aggregate_id, @resource_identifier, @actions, @time, @time);";

            // We use `default` (null in the database) to indicate that the connection should be deleted.
            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
            cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = evt.Actions;
            cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyResourceConnectionState(AccessListResourceConnectionActionsAddedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.access_list_resource_connections_state
                SET actions = actions || @actions,
                    modified = @time
                WHERE aggregate_id = @aggregate_id
                AND resource_identifier = @resource_identifier;";

            // We use `default` (null in the database) to indicate that the connection should be deleted.
            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
            cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = evt.Actions;
            cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyResourceConnectionState(AccessListResourceConnectionActionsRemovedEvent evt, CancellationToken cancellationToken)
        {
            const string GET_ACTIONS_QUERY = /*strpsql*/@"
                SELECT actions
                FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = @aggregate_id
                AND resource_identifier = @resource_identifier
                FOR UPDATE;";

            const string UPDATE_QUERY = /*strpsql*/@"
                UPDATE resourceregistry.access_list_resource_connections_state
                SET actions = @actions,
                    modified = @time
                WHERE aggregate_id = @aggregate_id
                AND resource_identifier = @resource_identifier;";

            HashSet<string> actions;
            
            // get the actions in the db (locking the row)
            {
                await using var cmd = _conn.CreateCommand(GET_ACTIONS_QUERY);

                cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
                cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);

                await cmd.PrepareAsync(cancellationToken);
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException($"No resource connection with identifier '{evt.ResourceIdentifier}' found.");
                }

                actions = new HashSet<string>(await reader.GetFieldValueAsync<IList<string>>("actions", cancellationToken));
            }

            // remove the items in the event
            actions.ExceptWith(evt.Actions);

            // save the new values
            {
                await using var cmd = _conn.CreateCommand(UPDATE_QUERY);

                cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
                cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
                cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = actions.ToList();
                cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

                await cmd.PrepareAsync(cancellationToken);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private async Task ApplyResourceConnectionState(AccessListResourceConnectionDeletedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = @aggregate_id
                AND resource_identifier = @resource_identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyMembersAddedState(AccessListMembersAddedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_members_state
                (aggregate_id, party_id, since)
                SELECT @aggregate_id, party_id, @since
                FROM unnest(@party_ids) AS party_id;";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).TypedValue = evt.PartyIds;
            cmd.Parameters.AddWithValue("since", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyMembersRemovedState(AccessListMembersRemovedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.access_list_members_state
                WHERE aggregate_id = @aggregate_id
                AND party_id = ANY(@party_ids);";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).TypedValue = evt.PartyIds;

            await cmd.PrepareAsync(cancellationToken);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpdateModifiedAtAndVersionChecked(AccessListAggregate aggregate, EventId newVersion, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.access_list_state
                SET modified = @modified,
                    version = @version
                WHERE aggregate_id = @aggregate_id
                AND version = @old_version;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, aggregate.Id);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, aggregate.UpdatedAt);
            cmd.Parameters.AddWithValue("version", NpgsqlDbType.Bigint, newVersion.DbValue);
            cmd.Parameters.AddWithValue("old_version", NpgsqlDbType.Bigint, aggregate.CommittedVersion.IsSet ? aggregate.CommittedVersion.DbValue : 0L);

            await cmd.PrepareAsync(cancellationToken);
            var updatedRows = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (updatedRows == 0)
            {
                throw OptimisticConcurrencyException.Create(aggregate);
            }
        }

        private static AccessListInfo CreateAccessListInfo(
            NpgsqlDataReader reader)
        {
            var aggregate_id = reader.GetGuid("aggregate_id");
            var identifier = reader.GetString("identifier");
            var owner = reader.GetString("resource_owner");
            var name = reader.GetString("name");
            var description = reader.GetString("description");
            var created = reader.GetFieldValue<DateTimeOffset>("created");
            var modified = reader.GetFieldValue<DateTimeOffset>("modified");
            var version = reader.GetFieldValue<long>("version");

            return new AccessListInfo(
                aggregate_id,
                owner,
                identifier,
                name,
                description,
                created,
                modified,
                ResourceConnections: null,
                checked((ulong)version));
        }

        private static AccessListResourceConnection CreateResourceConnection(
            NpgsqlDataReader reader,
            bool includeActions)
        {
            var resourceIdentifier = reader.GetString("resource_identifier");
            var actionsList = includeActions ? reader.GetFieldValue<IList<string>>("actions") : null;
            var created = reader.GetFieldValue<DateTimeOffset>("created");
            var modified = reader.GetFieldValue<DateTimeOffset>("modified");

            var actions = actionsList switch
            {
                null => null,
                [] => [],
                _ => ImmutableHashSet.CreateRange(actionsList),
            };

            return new AccessListResourceConnection(
                resourceIdentifier,
                actions,
                created, 
                modified);
        }

        private static async ValueTask<AccessListEvent> CreateAccessListEvent(
            NpgsqlDataReader reader,
            CancellationToken cancellationToken)
        {
            var id = new EventId((ulong)reader.GetFieldValue<long>("eid"));
            var kind = reader.GetString("kind");
            return kind switch
            {
                "created" => CreateAccessListCreatedEvent(reader, id),
                "updated" => CreateAccessListUpdatedEvent(reader, id),
                "deleted" => CreateAccessListDeletedEvent(reader, id),
                "resource_connection_created" => await CreateResourceConnectionCreatedEvent(reader, id, cancellationToken),
                "resource_connection_actions_added" => await CreateResourceConnectionActionsAddedEvent(reader, id, cancellationToken),
                "resource_connection_actions_removed" => await CreateResourceConnectionActionsRemovedEvent(reader, id, cancellationToken),
                "resource_connection_deleted" => CreateResourceConnectionDeletedEvent(reader, id),
                "members_added" => await CreateMembersAddedEvent(reader, id, cancellationToken),
                "members_removed" => await CreateMembersRemovedEvent(reader, id, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown event kind '{kind}'")
            };

            static AccessListCreatedEvent CreateAccessListCreatedEvent(NpgsqlDataReader reader, EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var identifier = reader.GetString("identifier");
                var owner = reader.GetString("resource_owner");
                var name = reader.GetString("name");
                var description = reader.GetString("description");

                return new AccessListCreatedEvent(id, aggregate_id, owner, identifier, name, description, etime);
            }

            static AccessListUpdatedEvent CreateAccessListUpdatedEvent(NpgsqlDataReader reader, EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var identifier = reader.GetStringOrNull("identifier");
                var name = reader.GetStringOrNull("name");
                var description = reader.GetStringOrNull("description");

                return new AccessListUpdatedEvent(id, aggregate_id, identifier, name, description, etime);
            }

            static AccessListDeletedEvent CreateAccessListDeletedEvent(NpgsqlDataReader reader, EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");

                return new AccessListDeletedEvent(id, aggregate_id, etime);
            }

            static async ValueTask<AccessListResourceConnectionCreatedEvent> CreateResourceConnectionCreatedEvent(NpgsqlDataReader reader, EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionCreatedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static async ValueTask<AccessListResourceConnectionActionsAddedEvent> CreateResourceConnectionActionsAddedEvent(NpgsqlDataReader reader, EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionActionsAddedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static async ValueTask<AccessListResourceConnectionActionsRemovedEvent> CreateResourceConnectionActionsRemovedEvent(NpgsqlDataReader reader, EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionActionsRemovedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static AccessListResourceConnectionDeletedEvent CreateResourceConnectionDeletedEvent(NpgsqlDataReader reader, EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");

                return new AccessListResourceConnectionDeletedEvent(id, aggregate_id, resourceIdentifier, etime);
            }

            static async ValueTask<AccessListMembersAddedEvent> CreateMembersAddedEvent(NpgsqlDataReader reader, EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new AccessListMembersAddedEvent(id, aggregate_id, partyIds, etime);
            }

            static async ValueTask<AccessListMembersRemovedEvent> CreateMembersRemovedEvent(NpgsqlDataReader reader, EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new AccessListMembersRemovedEvent(id, aggregate_id, partyIds, etime);
            }
        }
    }
}
