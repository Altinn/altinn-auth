using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Altinn.ResourceRegistry.Core.PartyRegistry;
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
internal partial class PartyRegistryRepository
{
    private class ScopedRepository
    {
        public static async Task<T> TransactionLess<T>(PartyRegistryRepository repo, Func<ScopedRepository, Task<T>> func, CancellationToken cancellationToken)
        {
            await using var conn = await repo._conn.OpenConnectionAsync(cancellationToken);
            return await func(new ScopedRepository(repo._logger, conn, repo._timeProvider));
        }

        public static async Task<T> RunInTransaction<T>(
            PartyRegistryRepository repo,
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

        public async Task<PartyRegistryInfo> CreatePartyRegistry(string registryOwner, string identifier, string name, string description, CancellationToken cancellationToken)
        {
            // Step 1. Check that the registry doesn't already exist
            var existingRegistry = await Lookup(registryOwner, identifier, cancellationToken);
            if (existingRegistry is not null)
            {
                throw new InvalidOperationException($"A registry with identifier '{identifier}' already exists for owner '{registryOwner}'.");
            }

            // Step 2. Create a new aggregate
            var partyRegistryAggregate = PartyRegistryAggregate.New(_timeProvider, Guid.NewGuid());
            partyRegistryAggregate.Initialize(registryOwner, identifier, name, description);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return partyRegistryAggregate.AsRegistryInfo();
        }

        public Task<PartyRegistryInfo?> Lookup(PartyRegistryIdentifier identifier, CancellationToken cancellationToken)
            => identifier switch
            {
                { IsRegistryId: true } => Lookup(identifier.RegistryId, cancellationToken),
                { IsOwnerAndIdentifier: true } => Lookup(identifier.Owner!, identifier.Identifier!, cancellationToken),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "Collection initializer")]
        public async Task<IReadOnlyList<PartyRegistryResourceConnection>> GetResourceConnections(PartyRegistryIdentifier identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT resource_identifier, actions, created, modified
                FROM resourceregistry.party_registry_resource_connections_state
                WHERE rid = @rid;";

            var id = await GetRegistryId(identifier, cancellationToken);

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, id);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            
            var connections = new List<PartyRegistryResourceConnection>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var resourceIdentifier = reader.GetString("resource_identifier");
                var actions = await reader.GetFieldValueAsync<IList<string>>("actions", cancellationToken);
                var created = reader.GetFieldValue<DateTimeOffset>("created");
                var modified = reader.GetFieldValue<DateTimeOffset>("modified");

                var connection = new PartyRegistryResourceConnection(resourceIdentifier, [..actions], created, modified);
                connections.Add(connection);
            }

            return connections;
        }

        public async Task<IReadOnlyList<PartyRegistryMembership>> GetPartyRegistryMemberships(PartyRegistryIdentifier identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT party_id, since
                FROM resourceregistry.party_registry_members_state
                WHERE rid = @rid;";

            var id = await GetRegistryId(identifier, cancellationToken);

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, id);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var memberships = new List<PartyRegistryMembership>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var partyId = reader.GetGuid("party_id");
                var since = reader.GetFieldValue<DateTimeOffset>("since");

                var membership = new PartyRegistryMembership(partyId, since);
                memberships.Add(membership);
            }

            return memberships;
        }

        public async Task<PartyRegistryInfo> Update(
            PartyRegistryIdentifier identifier,
            string? newIdentifier,
            string? newName,
            string? newDescription,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Update the aggregate
            partyRegistryAggregate.Update(newIdentifier, newName, newDescription);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return partyRegistryAggregate.AsRegistryInfo();
        }

        public async Task<PartyRegistryInfo> Delete(
            PartyRegistryIdentifier identifier,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Mark the aggregate as deleted
            var registryInfo = partyRegistryAggregate.AsRegistryInfo();
            partyRegistryAggregate.Delete();

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry infocod
            return registryInfo;
        }

        public async Task<PartyRegistryResourceConnection> AddPartyResourceConnection(
            PartyRegistryIdentifier identifier,
            string resourceIdentifier,
            IEnumerable<string> actions,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = partyRegistryAggregate.AddResourceConnection(resourceIdentifier, actions);
            
            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return connectionInfo;
        }

        public async Task<PartyRegistryResourceConnection> AddPartyResourceConnectionActions(
            PartyRegistryIdentifier identifier,
            string resourceIdentifier,
            IEnumerable<string> actions,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = partyRegistryAggregate.AddResourceConnectionActions(resourceIdentifier, actions);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return connectionInfo;
        }

        public async Task<PartyRegistryResourceConnection> RemovePartyResourceConnectionActions(
           PartyRegistryIdentifier identifier,
           string resourceIdentifier,
           IEnumerable<string> actions,
           CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = partyRegistryAggregate.RemoveResourceConnectionActions(resourceIdentifier, actions);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return connectionInfo;
        }

        public async Task<PartyRegistryResourceConnection> DeletePartyResourceConnection(
            PartyRegistryIdentifier identifier,
            string resourceIdentifier,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the connection to the aggregate
            var connectionInfo = partyRegistryAggregate.RemoveResourceConnection(resourceIdentifier);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            // Step 4. Return the registry info
            return connectionInfo;
        }

        public async Task<Unit> AddPartyRegistryMembers(
            PartyRegistryIdentifier identifier,
            IEnumerable<Guid> partyIds,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Add the members to the aggregate
            partyRegistryAggregate.AddMembers(partyIds);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            return Unit.Value;
        }

        public async Task<Unit> RemovePartyRegistryMembers(
            PartyRegistryIdentifier identifier,
            IEnumerable<Guid> partyIds,
            CancellationToken cancellationToken)
        {
            // Step 1. Load the aggregate
            var partyRegistryAggregate = await Load(identifier, cancellationToken);

            // Step 2. Remove the members to the aggregate
            partyRegistryAggregate.RemoveMembers(partyIds);

            // Step 3. Apply the event(s) to the database
            await ApplyChanges(partyRegistryAggregate, cancellationToken);

            return Unit.Value;
        }

        private async Task<PartyRegistryAggregate> Load(
            PartyRegistryIdentifier identifier,
            CancellationToken cancellationToken)
        {
            var registryId = await GetRegistryId(identifier, cancellationToken);
            var events = LoadEvents(registryId, cancellationToken);
            return await IAggregateFactory<PartyRegistryAggregate, PartyRegistryEvent>.LoadFrom(_timeProvider, registryId, events);
        }

        private async IAsyncEnumerable<PartyRegistryEvent> LoadEvents(
            Guid registryId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT eid, etime, kind, rid, identifier, registry_name, registry_description, registry_owner, actions, party_ids
                FROM resourceregistry.party_registry_events
                WHERE rid = @rid
                ORDER BY eid ASC;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, registryId);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return await CreatePartyRegistryEvent(reader, cancellationToken);
            }
        }

        private ValueTask<Guid> GetRegistryId(PartyRegistryIdentifier identifier, CancellationToken cancellationToken)
            => identifier switch
            {
                { IsRegistryId: true } => ValueTask.FromResult(identifier.RegistryId),
                { IsOwnerAndIdentifier: true } => new ValueTask<Guid>(GetRegistryId(identifier.Owner!, identifier.Identifier!, cancellationToken)),
                _ => throw new InvalidOperationException("Invalid identifier")
            };

        private async Task<Guid> GetRegistryId(string owner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT rid
                FROM resourceregistry.party_registry_state
                WHERE registry_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, owner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new ArgumentException($"No party registry with owner '{owner}' and identifier '{identifier}' found.");
            }

            return reader.GetGuid(0);
        }

        private async Task<PartyRegistryInfo?> Lookup(Guid registryId, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT rid, identifier, registry_owner, registry_name, registry_description, created, modified
                FROM resourceregistry.party_registry_state
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, registryId);

            return await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreatePartyRegistryInfo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<PartyRegistryInfo?> Lookup(string registryOwner, string identifier, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                SELECT rid, identifier, registry_owner, registry_name, registry_description, created, modified
                FROM resourceregistry.party_registry_state
                WHERE registry_owner = @owner AND identifier = @identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, registryOwner);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, identifier);

            return await cmd.ExecuteEnumerableAsync(cancellationToken)
                .Select(CreatePartyRegistryInfo)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task ApplyChanges(PartyRegistryAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_events (etime, kind, rid, identifier, registry_name, registry_description, registry_owner, actions, party_ids)
                VALUES (@etime, @kind, @rid, @identifier, @registry_name, @registry_description, @registry_owner, @actions, @party_ids)
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
                var rid = cmd.Parameters.Add("rid", NpgsqlDbType.Uuid);
                var identifier = cmd.Parameters.Add("identifier", NpgsqlDbType.Text);
                var registry_name = cmd.Parameters.Add("registry_name", NpgsqlDbType.Text);
                var registry_description = cmd.Parameters.Add("registry_description", NpgsqlDbType.Text);
                var registry_owner = cmd.Parameters.Add("registry_owner", NpgsqlDbType.Text);
                var actions = cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text);
                var party_ids = cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid);

                foreach (var evt in aggregate.GetUncommittedEvents())
                {
                    var values = evt.AsValues();
                    etime.SetValue(values.EventTime);
                    kind.SetValue(values.Kind);
                    rid.SetValue(values.AggregateId);
                    identifier.SetNullableValue(values.Identifier);
                    registry_name.SetNullableValue(values.Name);
                    registry_description.SetNullableValue(values.Description);
                    registry_owner.SetNullableValue(values.RegistryOwner);
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
                    PartyRegistryCreatedEvent create => ApplyCreateState(create, cancellationToken),
                    PartyRegistryUpdatedEvent update => ApplyUpdateState(update, cancellationToken),
                    PartyRegistryDeletedEvent delete => ApplyDeleteState(delete, cancellationToken),
                    PartyRegistryResourceConnectionCreatedEvent connectionSet => ApplyResourceConnectionState(connectionSet, cancellationToken),
                    PartyRegistryResourceConnectionActionsAddedEvent connectionActionsAdded => ApplyResourceConnectionState(connectionActionsAdded, cancellationToken),
                    PartyRegistryResourceConnectionActionsRemovedEvent connectionActionsRemoved => ApplyResourceConnectionState(connectionActionsRemoved, cancellationToken),
                    PartyRegistryResourceConnectionDeletedEvent connectionDeleted => ApplyResourceConnectionState(connectionDeleted, cancellationToken),
                    PartyRegistryMembersAddedEvent membersAdded => ApplyMembersAddedState(membersAdded, cancellationToken),
                    PartyRegistryMembersRemovedEvent membersRemoved => ApplyMembersRemovedState(membersRemoved, cancellationToken),
                    _ => throw new InvalidOperationException($"Unknown event type '{evt.GetType().Name}'")
                };

                await applyTask;
            }

            // Step 3. Update modified timestamp on the registry state table.
            if (!aggregate.IsDeleted)
            {
                await UpdateModifiedAt(aggregate, cancellationToken);
            }

            // Step 4. Mark all events as commited.
            aggregate.Commit();
        }

        private async Task ApplyCreateState(PartyRegistryCreatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_state
                (rid, identifier, registry_owner, registry_name, registry_description, created, modified)
                VALUES(@rid, @identifier, @registry_owner, @registry_name, @registry_description, @created, @modified);";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithValue("registry_owner", NpgsqlDbType.Text, evt.RegistryOwner);
            cmd.Parameters.AddWithValue("registry_name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithValue("registry_description", NpgsqlDbType.Text, evt.Description);
            cmd.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, evt.EventTime);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyUpdateState(PartyRegistryUpdatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_state
                SET identifier = COALESCE(@identifier, identifier),
                    registry_name = COALESCE(@registry_name, registry_name),
                    registry_description = COALESCE(@registry_description, registry_description)
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithNullableValue("identifier", NpgsqlDbType.Text, evt.Identifier);
            cmd.Parameters.AddWithNullableValue("registry_name", NpgsqlDbType.Text, evt.Name);
            cmd.Parameters.AddWithNullableValue("registry_description", NpgsqlDbType.Text, evt.Description);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyDeleteState(PartyRegistryDeletedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.party_registry_state
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyResourceConnectionState(PartyRegistryResourceConnectionCreatedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_resource_connections_state
                (rid, resource_identifier, actions, created, modified)
                VALUES(@rid, @resource_identifier, @actions, @time, @time);";

            // We use `default` (null in the database) to indicate that the connection should be deleted.
            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
            cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = evt.Actions;
            cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyResourceConnectionState(PartyRegistryResourceConnectionActionsAddedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_resource_connections_state
                SET actions = actions || @actions,
                    modified = @time
                WHERE rid = @rid
                AND resource_identifier = @resource_identifier;";

            // We use `default` (null in the database) to indicate that the connection should be deleted.
            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
            cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = evt.Actions;
            cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyResourceConnectionState(PartyRegistryResourceConnectionActionsRemovedEvent evt, CancellationToken cancellationToken)
        {
            const string GET_ACTIONS_QUERY = /*strpsql*/@"
                SELECT actions
                FROM resourceregistry.party_registry_resource_connections_state
                WHERE rid = @rid
                AND resource_identifier = @resource_identifier
                FOR UPDATE;";

            const string UPDATE_QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_resource_connections_state
                SET actions = @actions,
                    modified = @time
                WHERE rid = @rid
                AND resource_identifier = @resource_identifier;";

            HashSet<string> actions;
            
            // get the actions in the db (locking the row)
            {
                await using var cmd = _conn.CreateCommand(GET_ACTIONS_QUERY);

                cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
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

                cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
                cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);
                cmd.Parameters.Add<IList<string>>("actions", NpgsqlDbType.Array | NpgsqlDbType.Text).TypedValue = actions.ToList();
                cmd.Parameters.AddWithValue("time", NpgsqlDbType.TimestampTz, evt.EventTime);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private async Task ApplyResourceConnectionState(PartyRegistryResourceConnectionDeletedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.party_registry_resource_connections_state
                WHERE rid = @rid
                AND resource_identifier = @resource_identifier;";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.AddWithValue("resource_identifier", NpgsqlDbType.Text, evt.ResourceIdentifier);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyMembersAddedState(PartyRegistryMembersAddedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                INSERT INTO resourceregistry.party_registry_members_state
                (rid, party_id, since)
                SELECT @rid, party_id, @since
                FROM unnest(@party_ids) AS party_id;";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).TypedValue = evt.PartyIds;
            cmd.Parameters.AddWithValue("since", NpgsqlDbType.TimestampTz, evt.EventTime);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task ApplyMembersRemovedState(PartyRegistryMembersRemovedEvent evt, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                DELETE FROM resourceregistry.party_registry_members_state
                WHERE rid = @rid
                AND party_id = ANY(@party_ids);";

            await using var cmd = _conn.CreateCommand(QUERY);

            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, evt.RegistryId);
            cmd.Parameters.Add<IList<Guid>>("party_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).TypedValue = evt.PartyIds;

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpdateModifiedAt(PartyRegistryAggregate aggregate, CancellationToken cancellationToken)
        {
            const string QUERY = /*strpsql*/@"
                UPDATE resourceregistry.party_registry_state
                SET modified = @modified
                WHERE rid = @rid;";

            await using var cmd = _conn.CreateCommand(QUERY);
            cmd.Parameters.AddWithValue("rid", NpgsqlDbType.Uuid, aggregate.Id);
            cmd.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, aggregate.UpdatedAt);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        private static PartyRegistryInfo CreatePartyRegistryInfo(
            NpgsqlDataReader reader)
        {
            var rid = reader.GetGuid("rid");
            var identifier = reader.GetString("identifier");
            var owner = reader.GetString("registry_owner");
            var name = reader.GetString("registry_name");
            var description = reader.GetString("registry_description");
            var created = reader.GetFieldValue<DateTimeOffset>("created");
            var modified = reader.GetFieldValue<DateTimeOffset>("modified");

            return new PartyRegistryInfo(rid, owner, identifier, name, description, created, modified);
        }

        private static async ValueTask<PartyRegistryEvent> CreatePartyRegistryEvent(
            NpgsqlDataReader reader,
            CancellationToken cancellationToken)
        {
            var id = new Aggregates.EventId((ulong)reader.GetFieldValue<long>("eid"));
            var kind = reader.GetString("kind");
            return kind switch
            {
                "registry_created" => CreateRegistryCreatedEvent(reader, id),
                "registry_updated" => CreateRegistryUpdatedEvent(reader, id),
                "registry_deleted" => CreateRegistryDeletedEvent(reader, id),
                "resource_connection_created" => await CreateResourceConnectionCreatedEvent(reader, id, cancellationToken),
                "resource_connection_actions_added" => await CreateResourceConnectionActionsAddedEvent(reader, id, cancellationToken),
                "resource_connection_actions_removed" => await CreateResourceConnectionActionsRemovedEvent(reader, id, cancellationToken),
                "resource_connection_deleted" => CreateResourceConnectionDeletedEvent(reader, id),
                "members_added" => await CreateMembersAddedEvent(reader, id, cancellationToken),
                "members_removed" => await CreateMembersRemovedEvent(reader, id, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown event kind '{kind}'")
            };

            static PartyRegistryCreatedEvent CreateRegistryCreatedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var identifier = reader.GetString("identifier");
                var owner = reader.GetString("registry_owner");
                var name = reader.GetString("registry_name");
                var description = reader.GetString("registry_description");

                return new PartyRegistryCreatedEvent(id, rid, owner, identifier, name, description, etime);
            }

            static PartyRegistryUpdatedEvent CreateRegistryUpdatedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var identifier = reader.GetStringOrNull("identifier");
                var name = reader.GetStringOrNull("registry_name");
                var description = reader.GetStringOrNull("registry_description");

                return new PartyRegistryUpdatedEvent(id, rid, identifier, name, description, etime);
            }

            static PartyRegistryDeletedEvent CreateRegistryDeletedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");

                return new PartyRegistryDeletedEvent(id, rid, etime);
            }

            static async ValueTask<PartyRegistryResourceConnectionCreatedEvent> CreateResourceConnectionCreatedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new PartyRegistryResourceConnectionCreatedEvent(id, rid, resourceIdentifier, actions, etime);
            }

            static async ValueTask<PartyRegistryResourceConnectionActionsAddedEvent> CreateResourceConnectionActionsAddedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new PartyRegistryResourceConnectionActionsAddedEvent(id, rid, resourceIdentifier, actions, etime);
            }

            static async ValueTask<PartyRegistryResourceConnectionActionsRemovedEvent> CreateResourceConnectionActionsRemovedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var resourceIdentifier = reader.GetString("identifier");
                var actions = await reader.GetFieldValueArrayAsync<string>("actions", cancellationToken);

                return new PartyRegistryResourceConnectionActionsRemovedEvent(id, rid, resourceIdentifier, actions, etime);
            }

            static PartyRegistryResourceConnectionDeletedEvent CreateResourceConnectionDeletedEvent(NpgsqlDataReader reader, Aggregates.EventId id)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var resourceIdentifier = reader.GetString("identifier");

                return new PartyRegistryResourceConnectionDeletedEvent(id, rid, resourceIdentifier, etime);
            }

            static async ValueTask<PartyRegistryMembersAddedEvent> CreateMembersAddedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new PartyRegistryMembersAddedEvent(id, rid, partyIds, etime);
            }

            static async ValueTask<PartyRegistryMembersRemovedEvent> CreateMembersRemovedEvent(NpgsqlDataReader reader, Aggregates.EventId id, CancellationToken cancellationToken)
            {
                var etime = reader.GetFieldValue<DateTimeOffset>("etime");
                var rid = reader.GetGuid("rid");
                var partyIds = await reader.GetFieldValueArrayAsync<Guid>("party_ids", cancellationToken);

                return new PartyRegistryMembersRemovedEvent(id, rid, partyIds, etime);
            }
        }
    }
}
