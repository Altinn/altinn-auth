using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Persistence.Aggregates;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.ResourceRegistry.Persistence;

/// <content>
/// Contains the <see cref="ScopedRepository"/> class for doing operations
/// within the scope of a transaction.
/// </content>
internal partial class AccessListsRepository
{
    private sealed class ScopedRepository
    {
        public static async Task<T> TransactionLess<T>(AccessListsRepository repo, Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        {
            await using var conn = await repo._conn.OpenConnectionAsync(cancellationToken);
            return await func(new ScopedRepository(repo._logger, conn, repo._timeProvider));
        }

        public static async Task<T> RunInTransaction<T>(
            AccessListsRepository repo,
            Func<ScopedRepository, Task<T>> func,
            CancellationToken cancellationToken)
        {
            await using var conn = await repo._conn.OpenConnectionAsync(cancellationToken);
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var result = await func(new ScopedRepository(repo._logger, conn, repo._timeProvider));
            await tx.CommitAsync(cancellationToken);

            return result;
        }

        private readonly ILogger _logger;
        private readonly NpgsqlConnection _conn;
        private readonly TimeProvider _timeProvider;

        private ScopedRepository(ILogger logger, NpgsqlConnection conn, TimeProvider timeProvider)
        {
            _logger = logger;
            _conn = conn;
            _timeProvider = timeProvider;
        }

        public async Task<AccessListInfo> CreateAccessList(string resourceOwner, string identifier, string name, string description, CancellationToken cancellationToken)
        {
            // Step 1. Check that the access list doesn't already exist
            var existingRegistry = await Lookup(resourceOwner, identifier, cancellationToken);
            if (existingRegistry is not null)
            {
                throw new InvalidOperationException($"An access list with identifier '{identifier}' already exists for owner '{resourceOwner}'.");
            }

            // Step 2. Create a new aggregate
            var accessListAggregate = AccessListAggregate.New(_timeProvider, Guid.NewGuid());
            accessListAggregate.Initialize(resourceOwner, identifier, name, description);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the access list info
            return accessListAggregate.AsAccessListInfo();
        }

        public Task<AccessListInfo?> Lookup(AccessListIdentifier identifier, CancellationToken cancellationToken)
            => identifier switch
            {
                { IsAccessListId: true } => Lookup(identifier.AccessListId, cancellationToken),
                { IsOwnerAndIdentifier: true } => Lookup(identifier.Owner!, identifier.Identifier!, cancellationToken),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Collection initializer")]
        public async Task<IReadOnlyList<AccessListResourceConnection>> GetAccessListResourceConnections(AccessListIdentifier identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT resource_identifier, actions, created, modified
                FROM resourceregistry.access_list_resource_connections_state
                WHERE aggregate_id = @aggregate_id;";

            var id = await GetRegistryId(identifier, cancellationToken);

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, id);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            
            var connections = new List<AccessListResourceConnection>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var resourceIdentifier = reader.GetString("resource_identifier");
                var actions = await reader.GetFieldValueAsync<IList<string>>("actions", cancellationToken);
                var created = reader.GetFieldValue<DateTimeOffset>("created");
                var modified = reader.GetFieldValue<DateTimeOffset>("modified");

                var connection = new AccessListResourceConnection(resourceIdentifier, [..actions], created, modified);
                connections.Add(connection);
            }

            return connections;
        }

        public async Task<IReadOnlyList<AccessListMembership>> GetAccessListMemberships(AccessListIdentifier identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT party_id, since
                FROM resourceregistry.access_list_members_state
                WHERE aggregate_id = @aggregate_id;";

            var id = await GetRegistryId(identifier, cancellationToken);

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, id);

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

        public async Task<AccessListInfo> Update(
            AccessListIdentifier identifier,
            string? newIdentifier,
            string? newName,
            string? newDescription,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Update the aggregate
            accessListAggregate.Update(newIdentifier, newName, newDescription);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the access list info
            return accessListAggregate.AsAccessListInfo();
        }

        public async Task<AccessListInfo> Delete(
            AccessListIdentifier identifier,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Mark the aggregate as deleted
            var registryInfo = accessListAggregate.AsAccessListInfo();
            accessListAggregate.Delete();

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the access list info
            return registryInfo;
        }

        public async Task<AccessListResourceConnection> AddAccessListResourceConnection(
            AccessListIdentifier identifier,
            string resourceIdentifier,
            IEnumerable<string> actions,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = accessListAggregate.AddResourceConnection(resourceIdentifier, actions);
            
            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the access list info
            return connectionInfo;
        }

        public async Task<AccessListResourceConnection> AddAccessListResourceConnectionActions(
            AccessListIdentifier identifier,
            string resourceIdentifier,
            IEnumerable<string> actions,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = accessListAggregate.AddResourceConnectionActions(resourceIdentifier, actions);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the connection info
            return connectionInfo;
        }

        public async Task<AccessListResourceConnection> RemoveAccessListResourceConnectionActions(
           AccessListIdentifier identifier,
           string resourceIdentifier,
           IEnumerable<string> actions,
           CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = accessListAggregate.RemoveResourceConnectionActions(resourceIdentifier, actions);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the connection info
            return connectionInfo;
        }

        public async Task<AccessListResourceConnection> DeleteAccessListResourceConnection(
            AccessListIdentifier identifier,
            string resourceIdentifier,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = accessListAggregate.RemoveResourceConnection(resourceIdentifier);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            // Step 4. Return the connection info
            return connectionInfo;
        }

        public async Task<Unit> AddAccessListMembers(
            AccessListIdentifier identifier,
            IEnumerable<Guid> partyIds,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the members to the aggregate
            accessListAggregate.AddMembers(partyIds);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            return Unit.Value;
        }

        public async Task<Unit> RemoveAccessListMembers(
            AccessListIdentifier identifier,
            IEnumerable<Guid> partyIds,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var accessListAggregate = await Load(identifier, cancellationToken);

            // Step 2. Remove the members to the aggregate
            accessListAggregate.RemoveMembers(partyIds);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(accessListAggregate, cancellationToken);

            return Unit.Value;
        }

        private async Task<AccessListAggregate> Load(
            AccessListIdentifier identifier,
            CancellationToken cancellationToken)
        {
            var accessListId = await GetRegistryId(identifier, cancellationToken);
            var events = LoadEvents(accessListId, cancellationToken);
            return await IAggregateFactory<AccessListAggregate, AccessListEvent>.LoadFrom(_timeProvider, accessListId, events);
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

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return await CreateAccessListEvent(reader, cancellationToken);
            }
        }

        private ValueTask<Guid> GetRegistryId(AccessListIdentifier identifier, CancellationToken cancellationToken)
            => identifier switch
            {
                { IsAccessListId: true } => ValueTask.FromResult(identifier.AccessListId),
                { IsOwnerAndIdentifier: true } => new ValueTask<Guid>(GetRegistryId(identifier.Owner!, identifier.Identifier!, cancellationToken)),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

        private async Task<Guid> GetRegistryId(string owner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id
                FROM resourceregistry.access_list_state
                WHERE resource_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, owner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new ArgumentException($"No access list with owner '{owner}' and identifier '{identifier}' found.");
            }

            return reader.GetGuid(0);
        }

        private async Task<AccessListInfo?> Lookup(Guid accessListId, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id, identifier, resource_owner, name, description, created, modified
                FROM resourceregistry.access_list_state
                WHERE aggregate_id = @aggregate_id;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, accessListId);

            return await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreateAccessListInfo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<AccessListInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT aggregate_id, identifier, resource_owner, name, description, created, modified
                FROM resourceregistry.access_list_state
                WHERE resource_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, registryOwner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            return await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreateAccessListInfo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task ApplyChanges(AccessListAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_events (etime, kind, aggregate_id, identifier, name, description, resource_owner, actions, party_ids)
                VALUES (@etime, @kind, @aggregate_id, @identifier, @name, @description, @resource_owner, @actions, @party_ids)
                RETURNING eid;";

            // If there are no uncommitted events, return early
            if (!aggregate.HasUncommittedEvents)
            {
                return;
            }

            // Step 1. Insert all events into the database.
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

                    await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new InvalidOperationException("No event id returned from database.");
                    }

                    var id = new Aggregates.EventId((ulong)reader.GetFieldValue<long>("eid"));
                    evt.EventId = id;
                }
            }

            // Step 2. Update state tables in the database.
            foreach (var evt in aggregate.GetUncommittedEvents())
            {
                var applyTask = evt switch
                {
                    AccessListCreatedEvent create => ApplyCreateState(create, cancellationToken),
                    AccessListUpdatedEvent update => ApplyUpdateState(update, cancellationToken),
                    AccessListDeletedEvent delete => ApplyDeleteState(delete, cancellationToken),
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
                await UpdateModifiedAt(aggregate, cancellationToken);
            }

            // Step 4. Mark all events as commited.
            aggregate.Commit();
        }

        private async Task ApplyCreateState(AccessListCreatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.access_list_state
                (aggregate_id, identifier, resource_owner, name, description, created, modified)
                VALUES(@aggregate_id, @identifier, @resource_owner, @name, @description, @created, @modified);";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithValue("resource_owner", NpgsqlDbType.Text, evt.ResourceOwner);
            cmd.Parameters.AddWithValue("name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithValue("description", NpgsqlDbType.Text, evt.Description);
            cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, evt.EventTime);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, evt.EventTime);

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

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyDeleteState(AccessListDeletedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.access_list_state
                WHERE aggregate_id = @aggregate_id;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, evt.AggregateId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpdateModifiedAt(AccessListAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.access_list_state
                SET modified = @modified
                WHERE aggregate_id = @aggregate_id;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("aggregate_id", NpgsqlDbType.Uuid, aggregate.Id);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, aggregate.UpdatedAt);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
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

            return new AccessListInfo(aggregate_id, owner, identifier, name, description, created, modified);
        }

        private static async ValueTask<AccessListEvent> CreateAccessListEvent(
            NpgsqlDataReader reader,
            CancellationToken cancellationToken)
        {
            var id = new Aggregates.EventId((ulong)reader.GetFieldValue<long>("eid"));
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

            static AccessListCreatedEvent CreateAccessListCreatedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var identifier = reader.GetString("identifier");
                var owner = reader.GetString("resource_owner");
                var name = reader.GetString("name");
                var description = reader.GetString("description");

                return new AccessListCreatedEvent(id, aggregate_id, owner, identifier, name, description, etime);
            }

            static AccessListUpdatedEvent CreateAccessListUpdatedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var identifier = reader.GetStringOrNull("identifier");
                var name = reader.GetStringOrNull("name");
                var description = reader.GetStringOrNull("description");

                return new AccessListUpdatedEvent(id, aggregate_id, identifier, name, description, etime);
            }

            static AccessListDeletedEvent CreateAccessListDeletedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");

                return new AccessListDeletedEvent(id, aggregate_id, etime);
            }

            static async ValueTask<AccessListResourceConnectionCreatedEvent> CreateResourceConnectionCreatedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionCreatedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static async ValueTask<AccessListResourceConnectionActionsAddedEvent> CreateResourceConnectionActionsAddedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionActionsAddedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static async ValueTask<AccessListResourceConnectionActionsRemovedEvent> CreateResourceConnectionActionsRemovedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new AccessListResourceConnectionActionsRemovedEvent(id, aggregate_id, resourceIdentifier, actions, etime);
            }

            static AccessListResourceConnectionDeletedEvent CreateResourceConnectionDeletedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var resourceIdentifier = reader.GetString("identifier");

                return new AccessListResourceConnectionDeletedEvent(id, aggregate_id, resourceIdentifier, etime);
            }

            static async ValueTask<AccessListMembersAddedEvent> CreateMembersAddedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new AccessListMembersAddedEvent(id, aggregate_id, partyIds, etime);
            }

            static async ValueTask<AccessListMembersRemovedEvent> CreateMembersRemovedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var aggregate_id = reader.GetGuid("aggregate_id");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new AccessListMembersRemovedEvent(id, aggregate_id, partyIds, etime);
            }
        }
    }
}
