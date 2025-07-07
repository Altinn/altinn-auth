using System.Data;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace Altinn.AccessManagement.Persistence.Consent
{
    /// <summary>
    /// Repository for handling consent data.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ConsentRepository"/> class
    /// </remarks>
    public class ConsentRepository(NpgsqlDataSource db) : IConsentRepository
    {
        private readonly NpgsqlDataSource _db = db;

        private const string PARAM_CONSENT_REQUEST_ID = "consentRequestId";
        private const string PARAM_PERFORMED_BY_PARTY = "performedByParty";
        private const string PARAM_CONSENT_EVENT_ID = "consentEventId";
        private const string PARAM_EVENT_TYPE = "eventtype";
        private const string PARAM_CREATED = "created";
        private const string PARAM_CONSENT_RIGHT_ID = "consentRightId";
        private const string PARAM_CONSENT_CONTEXT_ID = "contextId";
        private const string PARAM_CONTEXT = "context";
        private const string PARAM_LANGAUGE = "language";

        private const string EventQuery = /*strpsql*/@"
                INSERT INTO consent.consentevent (consentEventId, consentRequestId, eventtype, created, performedByParty)
                VALUES (
                @consentEventId, 
                @consentRequestId, 
                @eventtype, 
                @created, 
                @performedByParty)
                RETURNING consentEventId;
                ";

        /// <inheritdoc/>
        public async Task AcceptConsentRequest(Guid consentRequestId, Guid performedByParty,  ConsentContext context, CancellationToken cancellationToken = default)
        {
            DateTimeOffset consentedTime = DateTime.UtcNow;

            const string updateConsentRequestQuery = /*strpsql*/@"
                    UPDATE consent.consentrequest set status = 'accepted', consented = @consentedTime  WHERE consentRequestId= @consentRequestId and status = 'created'";

            await using NpgsqlConnection conn = await _db.OpenConnectionAsync(default);

            // Run all inserts in one transaction in case of failure
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(cancellationToken);
            await using NpgsqlCommand command = conn.CreateCommand();
            command.CommandText = updateConsentRequestQuery;
            command.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            command.Parameters.AddWithValue("consentedTime", NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                // No rows were updated, meaning the consent request ID was not found or the status was not created 
                throw new InvalidOperationException($"Consent request with ID {consentRequestId} not found or already updated.");
            }

            await using NpgsqlCommand eventCommand = conn.CreateCommand();
            eventCommand.CommandText = EventQuery;
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_EVENT_ID, NpgsqlDbType.Uuid, Guid.CreateVersion7());
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            eventCommand.Parameters.Add(new NpgsqlParameter<ConsentRequestEventType>(PARAM_EVENT_TYPE, ConsentRequestEventType.Accepted));
            eventCommand.Parameters.AddWithValue(PARAM_CREATED, NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            eventCommand.Parameters.AddWithValue(PARAM_PERFORMED_BY_PARTY, NpgsqlDbType.Uuid, performedByParty);
            await eventCommand.ExecuteNonQueryAsync(cancellationToken);

            string contextQuery = /*strpsql*/@"
                INSERT INTO consent.context (contextId, consentRequestId, language)
                VALUES (
                @contextId,
                @consentRequestId, 
                @language)
                RETURNING consentRequestId;
                ";
            Guid contextId = Guid.CreateVersion7();
            await using NpgsqlCommand contextCommand = conn.CreateCommand();
            contextCommand.CommandText = contextQuery;
            contextCommand.Parameters.AddWithValue(PARAM_CONSENT_CONTEXT_ID, NpgsqlDbType.Uuid, contextId);
            contextCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            contextCommand.Parameters.AddWithValue("language", NpgsqlDbType.Text, context.Language);
            await contextCommand.ExecuteNonQueryAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ConsentRequestDetails> CreateRequest(ConsentRequest consentRequest, ConsentPartyUrn performedByParty, CancellationToken cancellationToken = default)
        {
            DateTimeOffset createdTime = DateTime.UtcNow;

            const string consentRquestQuery = /*strpsql*/@"
                INSERT INTO consent.consentrequest (consentRequestId, fromPartyUuid, requiredDelegatorUuid, toPartyUuid, handledByPartyUuid, validTo, requestMessage, templateId, templateVersion, redirectUrl)
                VALUES (
                @consentRequestId, 
                @fromPartyUuid,
                @requiredDelegatorUuid,
                @toPartyUuid, 
                @handledByPartyUuid,
                @validTo, 
                @requestMessage,
                @templateId, 
                @templateVersion, 
                @redirectUrl)
                RETURNING consentRequestId;
                ";

            await using NpgsqlConnection conn = await _db.OpenConnectionAsync(default);

            // Run all inserts in one transaction in case of failure
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(cancellationToken);
            await using NpgsqlCommand command = conn.CreateCommand();
            command.CommandText = consentRquestQuery;
            command.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid,  consentRequest.Id);
            command.Parameters.AddWithValue("templateId", NpgsqlDbType.Text, consentRequest.TemplateId);
       
            if (consentRequest.TemplateVersion != null)
            {
                command.Parameters.AddWithValue("templateVersion", NpgsqlDbType.Integer, consentRequest.TemplateVersion);
            }
            else
            {
                command.Parameters.AddWithValue("templateVersion", NpgsqlDbType.Integer, DBNull.Value);
            }

            if (consentRequest.From.IsPartyUuid(out Guid fromPartyGuid))
            {
                command.Parameters.AddWithValue("fromPartyUuid", NpgsqlDbType.Uuid, fromPartyGuid);
            }
            else
            {
                throw new InvalidDataException("Invalid fromPartyUuid");
            }

            if (consentRequest.HandledBy != null && consentRequest.HandledBy.IsPartyUuid(out Guid handledByPartyGuid))
            {
                command.Parameters.AddWithValue("handledByPartyUuid", NpgsqlDbType.Uuid, handledByPartyGuid);
            }
            else
            {
                command.Parameters.AddWithValue("handledByPartyUuid", NpgsqlDbType.Uuid, DBNull.Value);
            }

            if (consentRequest.RequiredDelegator != null && consentRequest.RequiredDelegator.IsPartyUuid(out Guid requiredDelegatorGuid))
            {
                command.Parameters.AddWithValue("requiredDelegatorUuid", NpgsqlDbType.Uuid, requiredDelegatorGuid);
            }
            else
            {
                command.Parameters.AddWithValue("requiredDelegatorUuid", NpgsqlDbType.Uuid, DBNull.Value);
            }

            if (consentRequest.To.IsPartyUuid(out Guid toPartyGuid))
            {
                command.Parameters.AddWithValue("toPartyUuid", NpgsqlDbType.Uuid, toPartyGuid);
            }
            else
            {
                throw new InvalidDataException("Invalid toPartyUuid");
            }

            if (consentRequest.RequestMessage != null)
            {
                command.Parameters.AddWithValue("requestMessage", NpgsqlDbType.Hstore, consentRequest.RequestMessage);
            }
            else
            {
                command.Parameters.AddWithValue("requestMessage", NpgsqlDbType.Hstore, DBNull.Value);
            }

            command.Parameters.AddWithValue("redirectUrl", NpgsqlDbType.Text, consentRequest.RedirectUrl);

            command.Parameters.AddWithValue("validTo", NpgsqlDbType.TimestampTz, consentRequest.ValidTo.ToOffset(TimeSpan.Zero));

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (NpgsqlException ex)
            {
                if (ex.SqlState == "23505")
                {
                    return null; // Duplicate key violation
                }
                
                throw;
            }

            foreach (ConsentRight consentRight in consentRequest.ConsentRights)
            {
                Guid consentRightGuid = Guid.CreateVersion7();

                const string rightsQuery = /*strpsql*/@"
                INSERT INTO consent.consentright(consentRightId , consentRequestId , action)
                VALUES (
                @consentRightId, 
                @consentRequestId, 
                @action
                )
                RETURNING consentRightId;
                ";

                await using NpgsqlCommand rightsCommand = conn.CreateCommand();
                rightsCommand.CommandText = rightsQuery;
                rightsCommand.Parameters.AddWithValue(PARAM_CONSENT_RIGHT_ID, consentRightGuid);
                rightsCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, consentRequest.Id);
                rightsCommand.Parameters.AddWithValue("action", consentRight.Action);
                await rightsCommand.ExecuteNonQueryAsync(cancellationToken);

                // Bulding up the query for the resource attributes. Typical this is only one, but in theory it can be multiple attributes identifying a resource.
                List<string> values = [];

                await using NpgsqlCommand resourceCommand = conn.CreateCommand();
                for (int i = 0; i < consentRight.Resource.Count; i++)
                {
                    values.Add($"(@consentRightId{i}, @type{i}, @value{i}, @version{i})");
                    resourceCommand.Parameters.AddWithValue($"@consentRightId{i}", consentRightGuid);
                    resourceCommand.Parameters.AddWithValue($"@type{i}", consentRight.Resource[i].Type);
                    resourceCommand.Parameters.AddWithValue($"@value{i}", consentRight.Resource[i].Value);
                    if (consentRight.Resource[i].Version != null)
                    {
                        resourceCommand.Parameters.AddWithValue($"@version{i}", consentRight.Resource[i].Version);
                    }
                    else
                    {
                        resourceCommand.Parameters.AddWithValue($"@version{i}", DBNull.Value);
                    }
                }

                resourceCommand.CommandText = $"INSERT INTO consent.resourceattribute (consentRightId, type, value, version) VALUES {string.Join(", ", values)}";
                await resourceCommand.ExecuteNonQueryAsync(cancellationToken);

                if (consentRight.Metadata != null && consentRight.Metadata.Count > 0)
                {
                    await using NpgsqlCommand metadatacommand = conn.CreateCommand();
                    List<string> metaValues = [];
                    int metadataIndex = 0;
                    foreach (KeyValuePair<string, string> kvp in consentRight.Metadata)
                    {
                        metaValues.Add($"(@consentRightId{metadataIndex}, @id{metadataIndex}, @value{metadataIndex})");
                        metadatacommand.Parameters.AddWithValue($"@consentrightid{metadataIndex}", consentRightGuid);
                        metadatacommand.Parameters.AddWithValue($"@id{metadataIndex}", kvp.Key);
                        metadatacommand.Parameters.AddWithValue($"@value{metadataIndex}", kvp.Value);
                        metadataIndex++;
                    }

                    metadatacommand.CommandText = $"INSERT INTO consent.metadata (consentrightid, id, value) VALUES {string.Join(", ", metaValues)}";
                    await metadatacommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await using NpgsqlCommand eventCommand = conn.CreateCommand();
            eventCommand.CommandText = EventQuery;
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_EVENT_ID, NpgsqlDbType.Uuid, Guid.CreateVersion7());
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequest.Id);
            eventCommand.Parameters.Add(new NpgsqlParameter<ConsentRequestEventType>(PARAM_EVENT_TYPE, ConsentRequestEventType.Created));
            eventCommand.Parameters.AddWithValue(PARAM_CREATED, NpgsqlDbType.TimestampTz, createdTime.ToOffset(TimeSpan.Zero));
            if (performedByParty.IsPartyUuid(out Guid performedByPartyGuid))
            {
                eventCommand.Parameters.AddWithValue(PARAM_PERFORMED_BY_PARTY, NpgsqlDbType.Uuid, performedByPartyGuid);
            }
            else
            {
                throw new InvalidDataException("Invalid fromPartyUuid");
            }

            await eventCommand.ExecuteNonQueryAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken); 

            return await GetRequest(consentRequest.Id, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DeleteRequest(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<List<Core.Models.Consent.Consent>> GetAllConsents(Guid partyUid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<ConsentRequestDetails> GetRequest(Guid consentRequestId, CancellationToken cancellationToken = default)
        {
            List<ConsentRight> consentRight = await GetConsentRights(consentRequestId, cancellationToken);
            List<ConsentRequestEvent> consentRequestEvents = await GetEvents(consentRequestId, cancellationToken);

            string consentQuery = /*strpsql*/@$"
                SELECT * FROM consent.consentrequest 
                WHERE consentRequestId = @id
                ";

            await using var pgcom = _db.CreateCommand(consentQuery);
            pgcom.Parameters.AddWithValue("id", consentRequestId);

            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                Guid from = await reader.GetFieldValueAsync<Guid>("fromPartyUuid", cancellationToken: cancellationToken);
                Guid to = await reader.GetFieldValueAsync<Guid>("toPartyUuid", cancellationToken: cancellationToken);
                Guid? requiredDelegator = await reader.GetFieldValueAsync<Guid?>("requiredDelegatorUuid", cancellationToken: cancellationToken);
                Guid? handledByParty = await reader.GetFieldValueAsync<Guid?>("handledByPartyUuid", cancellationToken: cancellationToken);

                ConsentPartyUrn fromPartyUrn = ConsentPartyUrn.PartyUuid.Create(from);
                ConsentPartyUrn toPartyUrn = ConsentPartyUrn.PartyUuid.Create(to);

                ConsentPartyUrn requiredDelegatorUrn = null;
                ConsentPartyUrn handledByPartyUrn = null;
                
                if (requiredDelegator != null)
                {
                    requiredDelegatorUrn = ConsentPartyUrn.PartyUuid.Create(requiredDelegator.Value);
                }

                if (handledByParty != null)
                {
                    handledByPartyUrn = ConsentPartyUrn.PartyUuid.Create(handledByParty.Value);
                }

                if (fromPartyUrn == null || toPartyUrn == null)
                {
                    throw new InvalidDataException("Invalid party URN");
                }

                return new ConsentRequestDetails
                {
                    Id = consentRequestId,
                    From = fromPartyUrn,
                    To = toPartyUrn,
                    HandledBy = handledByPartyUrn,
                    RequiredDelegator = requiredDelegatorUrn,
                    ValidTo = await reader.GetFieldValueAsync<DateTimeOffset>("validTo", cancellationToken: cancellationToken),
                    ConsentRights = consentRight,
                    RequestMessage = await reader.IsDBNullAsync(reader.GetOrdinal("requestMessage"), cancellationToken)
                        ? null
                        : await reader.GetFieldValueAsync<Dictionary<string, string>>("requestMessage", cancellationToken: cancellationToken),
                    ConsentRequestStatus = await reader.GetFieldValueAsync<ConsentRequestStatusType>("status", cancellationToken: cancellationToken),
                    Consented = await reader.GetFieldValueAsync<DateTimeOffset?>("consented", cancellationToken: cancellationToken),
                    RedirectUrl = await reader.GetFieldValueAsync<string>("redirectUrl", cancellationToken: cancellationToken),
                    ConsentRequestEvents = consentRequestEvents,
                    TemplateId = await reader.GetFieldValueAsync<string>("templateId", cancellationToken: cancellationToken),
                    TemplateVersion = await reader.GetFieldValueAsync<int?>("templateVersion", cancellationToken: cancellationToken),
                };
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task RejectConsentRequest(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            DateTimeOffset consentedTime = DateTime.UtcNow;

            const string updateConsentRequestQuery = /*strpsql*/@"
                    UPDATE consent.consentrequest set status = 'rejected' WHERE consentRequestId= @consentRequestId and status = 'created'";

            await using NpgsqlConnection conn = await _db.OpenConnectionAsync(default);

            // Run all inserts in one transaction in case of failure
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(cancellationToken);
            await using NpgsqlCommand command = conn.CreateCommand();
            command.CommandText = updateConsentRequestQuery;
            command.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            command.Parameters.AddWithValue("consentedTime", NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                // No rows were updated, meaning the consent request ID was not found or the status was not created 
                throw new InvalidOperationException($"Consent request with ID {consentRequestId} not found or already updated.");
            }

            await using NpgsqlCommand eventCommand = conn.CreateCommand();
            eventCommand.CommandText = EventQuery;
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_EVENT_ID, NpgsqlDbType.Uuid, Guid.CreateVersion7());
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            eventCommand.Parameters.Add(new NpgsqlParameter<ConsentRequestEventType>(PARAM_EVENT_TYPE, ConsentRequestEventType.Rejected));
            eventCommand.Parameters.AddWithValue(PARAM_CREATED, NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            eventCommand.Parameters.AddWithValue(PARAM_PERFORMED_BY_PARTY, NpgsqlDbType.Uuid, performedByParty);
            await eventCommand.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task Revoke(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken = default)
        {
            DateTimeOffset consentedTime = DateTime.UtcNow;

            const string updateConsentRequestQuery = /*strpsql*/@"
                    UPDATE consent.consentrequest set status = 'revoked', consented = @consentedTime  WHERE consentRequestId= @consentRequestId and status = 'accepted'";

            await using NpgsqlConnection conn = await _db.OpenConnectionAsync(default);

            // Run all inserts in one transaction in case of failure
            await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(cancellationToken);
            await using NpgsqlCommand command = conn.CreateCommand();
            command.CommandText = updateConsentRequestQuery;
            command.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            command.Parameters.AddWithValue("consentedTime", NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                // No rows were updated, meaning the consent request ID was not found or the status was not created 
                throw new InvalidOperationException($"Consent request with ID {consentRequestId} not found or already updated.");
            }

            await using NpgsqlCommand eventCommand = conn.CreateCommand();
            eventCommand.CommandText = EventQuery;
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_EVENT_ID, NpgsqlDbType.Uuid, Guid.CreateVersion7());
            eventCommand.Parameters.AddWithValue(PARAM_CONSENT_REQUEST_ID, NpgsqlDbType.Uuid, consentRequestId);
            eventCommand.Parameters.Add(new NpgsqlParameter<ConsentRequestEventType>(PARAM_EVENT_TYPE, ConsentRequestEventType.Revoked));
            eventCommand.Parameters.AddWithValue(PARAM_CREATED, NpgsqlDbType.TimestampTz, consentedTime.ToOffset(TimeSpan.Zero));
            eventCommand.Parameters.AddWithValue(PARAM_PERFORMED_BY_PARTY, NpgsqlDbType.Uuid, performedByParty);
            await eventCommand.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        /// <summary>
        /// Return the consent rights for a given consentRequest
        /// </summary>
        private async Task<List<ConsentRight>> GetConsentRights(Guid consentRequestId, CancellationToken cancellationToken = default)
        {
            Dictionary<Guid, List<ConsentResourceAttribute>> keyValuePairs = await GetConsentResourceAttributes(consentRequestId, cancellationToken);

            string consentRightsQuery = /*strpsql*/@$"
                SELECT 
                consentRightId,
                consentRequestId,
                action 
                FROM consent.consentright 
                WHERE consentRequestId = @consentRequestId
                ";

            await using var pgcom = _db.CreateCommand(consentRightsQuery);
            pgcom.Parameters.AddWithValue("@consentRequestId", NpgsqlTypes.NpgsqlDbType.Uuid, consentRequestId);
            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);
            List<ConsentRight> consentRights = [];
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid consentRightId = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_RIGHT_ID, cancellationToken: cancellationToken);

                List<ConsentResourceAttribute> resourceAttributes = [];
                if (keyValuePairs.TryGetValue(consentRightId, out List<ConsentResourceAttribute> foundAttributes))
                {
                    resourceAttributes = foundAttributes;
                }

                Dictionary<string, string> metadata;
                Dictionary<Guid, Dictionary<string, string>> consentMetadata = await GetConsentRightMetadata(consentRequestId, cancellationToken);
                if (consentMetadata.TryGetValue(consentRightId, out Dictionary<string, string> foundMetadata))
                {
                    metadata = foundMetadata;
                }
                else
                {
                    metadata = null;
                }

                ConsentRight consentRight = new()
                {
                    Action = await reader.GetFieldValueAsync<List<string>>("action", cancellationToken: cancellationToken),
                    Resource = resourceAttributes
                };

                consentRight.AddMetadataValues(metadata);

                consentRights.Add(consentRight);
            }

            return consentRights;
        }

        /// <summary>
        ///  Gets the consent resource attributes for a consent request. Returned as a dictinary to be able to group the attributes by consent right.
        /// </summary>
        private async Task<Dictionary<Guid, List<ConsentResourceAttribute>>> GetConsentResourceAttributes(Guid consentRequestId, CancellationToken cancellationToken)
        {
            string consentResourcesQuery = /*strpsql*/@$"
                SELECT 
                cr.consentRightId,
                type,
                value,
                version
                FROM consent.resourceattribute ra 
                join consent.consentright cr on cr.consentRightId = ra.consentRightId 
                WHERE cr.consentRequestId = @id
                ";

            await using var pgcom = _db.CreateCommand(consentResourcesQuery);
            pgcom.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, consentRequestId);

            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);
            Dictionary<Guid, List<ConsentResourceAttribute>> keyValuePairs = [];

            while (await reader.ReadAsync(cancellationToken))
            {
                Guid consentRightId = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_RIGHT_ID, cancellationToken: cancellationToken);
                ConsentResourceAttribute consentResourceAttribute = new()
                {
                    Type = await reader.GetFieldValueAsync<string>("type", cancellationToken: cancellationToken),
                    Value = await reader.GetFieldValueAsync<string>("value", cancellationToken: cancellationToken),
                    Version = await reader.IsDBNullAsync(reader.GetOrdinal("version"), cancellationToken) ? null
                        : await reader.GetFieldValueAsync<string>("version", cancellationToken: cancellationToken)
                };

                if (keyValuePairs.TryGetValue(consentRightId, out List<ConsentResourceAttribute> value))
                {
                    value.Add(consentResourceAttribute);
                }
                else
                {
                    keyValuePairs.Add(consentRightId, [consentResourceAttribute]);
                }
            }

            return keyValuePairs;
        }

        private async Task<Dictionary<Guid, Dictionary<string, string>>> GetConsentRightMetadata(Guid consentRequestId, CancellationToken cancellationToken)
        {
            string consentMetadataQuery = /*strpsql*/@$"
                SELECT
                cr.consentRightId,
                id,
                value 
                FROM consent.metadata ra 
                join consent.consentright cr on cr.consentRightId = ra.consentRightId
                WHERE cr.consentRequestId = @id
                ";

            await using var pgcom = _db.CreateCommand(consentMetadataQuery);
            pgcom.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, consentRequestId);
            Dictionary<Guid, Dictionary<string, string>> consentMetadata = [];

            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid consentRightId = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_RIGHT_ID, cancellationToken: cancellationToken);
                string metadataId = await reader.GetFieldValueAsync<string>("id", cancellationToken: cancellationToken);
                string metadataValue = await reader.GetFieldValueAsync<string>("value", cancellationToken: cancellationToken);

                if (consentMetadata.TryGetValue(consentRightId, out Dictionary<string, string> value))
                {
                    value.Add(metadataId, metadataValue);
                }
                else
                {
                    consentMetadata.Add(consentRightId, new Dictionary<string, string> { { metadataId, metadataValue } });
                }
            }

            return consentMetadata;
        }

        private async Task<List<ConsentRequestEvent>> GetEvents(Guid consentRequestId, CancellationToken cancellationToken = default)
        {
            List<ConsentRequestEvent> consentRequestEvents = [];
            string consentMetadataQuery = /*strpsql*/@$"
                SELECT
                consentEventId,
                consentRequestId,
                eventtype, 
                created,
                performedByParty
                FROM consent.consentevent 
                WHERE consentRequestId = @consentRequestId
                ORDER BY created
                ";

            await using var pgcom = _db.CreateCommand(consentMetadataQuery);
            pgcom.Parameters.AddWithValue("@consentRequestId", NpgsqlTypes.NpgsqlDbType.Uuid, consentRequestId);

            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid performedBy = await reader.GetFieldValueAsync<Guid>("performedByParty", cancellationToken: cancellationToken);
                ConsentRequestEvent consentRequestEvent = new()
                {
                    ConsentEventID = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_EVENT_ID, cancellationToken: cancellationToken),
                    ConsentRequestID = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_REQUEST_ID, cancellationToken: cancellationToken),
                    EventType = await reader.GetFieldValueAsync<ConsentRequestEventType>(PARAM_EVENT_TYPE, cancellationToken: cancellationToken),
                    Created = await reader.GetFieldValueAsync<DateTimeOffset>(PARAM_CREATED, cancellationToken: cancellationToken),
                    PerformedBy = ConsentPartyUrn.PartyUuid.Create(performedBy)
                };
                consentRequestEvents.Add(consentRequestEvent);
            }

            return consentRequestEvents;
        }

        /// <summary>
        /// Gets the consent context if consented
        /// </summary>
        public async Task<ConsentContext> GetConsentContext(Guid consentRequestId, CancellationToken cancellationToken)
        {
            string consentContextQuery = /*strpsql*/@$"
                SELECT 
                contextId,
                consentRequestId,
                language 
                FROM consent.context 
                WHERE consentRequestId = @consentRequestId
                ";
            await using var pgcom = _db.CreateCommand(consentContextQuery);
            pgcom.Parameters.AddWithValue("@consentRequestId", NpgsqlTypes.NpgsqlDbType.Uuid, consentRequestId);
            using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

            ConsentContext consentContext = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                consentContext = new()
                {
                    Language = await reader.GetFieldValueAsync<string>(PARAM_LANGAUGE, cancellationToken: cancellationToken),
                    ContextId = await reader.GetFieldValueAsync<Guid>(PARAM_CONSENT_CONTEXT_ID, cancellationToken: cancellationToken)
                };
            }

            if (consentContext == null)
            {
                return null;
            }

            return consentContext;      
        }
    }
}
