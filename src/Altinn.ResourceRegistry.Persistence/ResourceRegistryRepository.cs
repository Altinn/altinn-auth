using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using Altinn.Authorization.ProblemDetails;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Altinn.ResourceRegistry.Persistence;

/// <summary>
/// The repository implementation for postgre database operations on resource registry
/// </summary>
internal class ResourceRegistryRepository : IResourceRegistryRepository
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly NpgsqlDataSource _conn;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceRegistryRepository"/> class
    /// </summary>
    /// <param name="conn">The database connection</param>
    /// <param name="logger">Logger</param>
    public ResourceRegistryRepository(
        NpgsqlDataSource conn,
        ILogger<ResourceRegistryRepository> logger)
    {
        _conn = conn;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<ServiceResource>> Search(
        ResourceSearch resourceSearch,
        bool includeAllVersions = false,
        CancellationToken cancellationToken = default)
    {
        string versionFilter = includeAllVersions ? string.Empty : "WHERE rn = 1";

        string query = /*strpsql*/@$"
        WITH ranked AS (
            SELECT
                ri.identifier,
                ri.created,
                res.modified,
                res.serviceresourcejson,
                res.version_id,
                ROW_NUMBER() OVER (
                    PARTITION BY ri.identifier
                    ORDER BY res.version_id DESC
                ) AS rn
            FROM resourceregistry.resources res
            JOIN resourceregistry.resource_identifier ri
                ON res.identifier = ri.identifier
            WHERE (@id IS NULL OR ri.identifier ILIKE concat('%', @id, '%'))
              AND (@title IS NULL OR serviceresourcejson ->> 'title' ILIKE concat('%', @title, '%'))
              AND (@description IS NULL OR serviceresourcejson ->> 'description' ILIKE concat('%', @description, '%'))
              AND (@resourcetype IS NULL OR serviceresourcejson ->> 'resourceType' ILIKE @resourcetype::text)
              AND (@keyword IS NULL OR serviceresourcejson ->> 'keywords' ILIKE concat('%', @keyword, '%'))
        )
        SELECT identifier, created, modified, serviceresourcejson, version_id
        FROM ranked
        {versionFilter}
        ORDER BY identifier;
        ";

        try
        {
            await using var pgcom = _conn.CreateCommand(query);
            pgcom.Parameters.AddWithNullableValue("id", NpgsqlDbType.Text, resourceSearch.Id);
            pgcom.Parameters.AddWithNullableValue("title", NpgsqlDbType.Text, resourceSearch.Title);
            pgcom.Parameters.AddWithNullableValue("description", NpgsqlDbType.Text, resourceSearch.Description);
            pgcom.Parameters.AddWithNullableValue("resourcetype", NpgsqlDbType.Text, resourceSearch.ResourceType?.ToString());
            pgcom.Parameters.AddWithNullableValue("keyword", NpgsqlDbType.Text, resourceSearch.Keyword);

            return await pgcom.ExecuteEnumerableAsync(cancellationToken)
                 .SelectAwaitWithCancellation(GetServiceResource)
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // Search // Exception");
            throw;
        }
    }

    /// <summary>
    /// Creates a new service resource in the database
    /// </summary>
    public async Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(resource.Identifier);

        DateTime created = DateTime.UtcNow;
        DateTime modified = created;

        var json = JsonSerializer.SerializeToDocument(resource, JsonSerializerOptions);

        await using var conn = await _conn.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var cmd1 = new NpgsqlCommand(
                @"
                INSERT INTO resourceregistry.resource_identifier(identifier, created)
                VALUES (@identifier, @created);", 
                conn, 
                tx))
            {
                cmd1.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, resource.Identifier);
                cmd1.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, created);
                await cmd1.ExecuteNonQueryAsync(cancellationToken);
            }

            ServiceResource serviceResource;
            
            await using (var cmd2 = new NpgsqlCommand(
                @"
                INSERT INTO resourceregistry.resources(
                    identifier,
                    created,
                    modified,
                    serviceresourcejson
                )
                VALUES (
                    @identifier,
                    @created,
                    @modified,
                    @serviceresourcejson
                )
                RETURNING identifier, created, modified, serviceresourcejson, version_id;", 
                conn, 
                tx))
            {
                cmd2.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, resource.Identifier);
                cmd2.Parameters.AddWithValue("created", NpgsqlDbType.TimestampTz, created);
                cmd2.Parameters.AddWithValue("modified", NpgsqlDbType.TimestampTz, modified);
                cmd2.Parameters.AddWithValue("serviceresourcejson", NpgsqlDbType.Jsonb, json);

                await using (var reader = await cmd2.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new SqlNullValueException("No result from database when creating resource version");
                    }

                    serviceResource = await GetServiceResource(reader, cancellationToken);
                }
            }

            await tx.CommitAsync(cancellationToken);
            return serviceResource;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // CreateResource // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource?> DeleteResource(string id, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            DELETE FROM resourceregistry.resources
            WHERE identifier = @identifier
            RETURNING identifier, created, modified, serviceresourcejson    
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwaitWithCancellation(GetServiceResource)
                .SingleOrDefaultAsync(cancellationToken);

            return serviceResource;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // DeleteResource // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource?> GetResource(string id, int? versionId, CancellationToken cancellationToken = default)
    {
        string query = string.Empty;

        if (versionId != null)
        {
            query = /*strpsql*/@"
            SELECT ri.identifier, ri.created, res.modified, res.serviceresourcejson, res.version_id
            FROM resourceregistry.resources res 
            join resourceregistry.resource_identifier ri on res.identifier = ri.identifier
            WHERE res.identifier = @identifier
            AND res.version_id = @versionId";
        }
        else
        {
            // Get the latest version using the current_resources view
            query = /*strpsql*/@"
            SELECT identifier, created, modified, serviceresourcejson, version_id
            FROM resourceregistry.current_resources res
            WHERE res.identifier = @identifier";
        }

        try
        {
            await using var pgcom = _conn.CreateCommand(query);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

            if (versionId.HasValue)
            {
                pgcom.Parameters.AddWithValue("versionId", NpgsqlDbType.Integer, versionId.Value);
            }

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwaitWithCancellation(GetServiceResource)
                .SingleOrDefaultAsync(cancellationToken);

            return serviceResource;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<CompetentAuthorityReference>> GetResourceOwner(string id, CancellationToken cancellationToken = default)
    {
        const string QUERY =
            /*strpsql*/"""
            SELECT
                serviceresourcejson->'hasCompetentAuthority'->>'organization' AS organization,
                serviceresourcejson->'hasCompetentAuthority'->>'orgcode' AS orgcode
            FROM resourceregistry.current_resources
            WHERE identifier = @identifier
            """;

        await using var cmd = _conn.CreateCommand(QUERY);
        cmd.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Problems.ResourceReference_NotFound;
        }

        var org = reader.GetString(0);
        var orgCode = reader.GetString(1);

        var authority = new CompetentAuthorityReference
        {
            Organization = org,
            Orgcode = orgCode,
        };

        return authority;
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            INSERT INTO resourceregistry.resources(
                identifier,
                created,
                modified,
                serviceresourcejson
            )
            VALUES
            (
                @identifier,
                Now(),
                Now(),
                @serviceresourcejson
            )
            RETURNING identifier, created, modified, serviceresourcejson, version_id
            ";

        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(resource.Identifier);
        var json = JsonSerializer.SerializeToDocument(resource, JsonSerializerOptions);
        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, resource.Identifier);
            pgcom.Parameters.AddWithValue("serviceresourcejson", NpgsqlDbType.Jsonb, json);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                 .SelectAwaitWithCancellation(GetServiceResource)
                .FirstOrDefaultAsync(cancellationToken);

            return serviceResource ?? throw new SqlNullValueException("No result from database");
        }
        catch (Exception e)
        {
            if (!e.Message.Contains("duplicate key value violates unique constraint"))
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<SubjectResources>> FindResourcesForSubjects(IEnumerable<string> subjects, CancellationToken cancellationToken = default)
    {
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE subject_urn = ANY(@subjects) AND deleted = false";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("subjects", subjects.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        Dictionary<string, SubjectResources> allSubjectResources = new Dictionary<string, SubjectResources>();

        while (await reader.ReadAsync(cancellationToken))
        {
            AttributeMatchV2 subjectMatch = GetSubjectAttribute(reader);
            AttributeMatchV2 resourceAttributeMatch = GetResourceAttribute(reader);

            if (allSubjectResources.TryGetValue(subjectMatch.Urn, out SubjectResources? existingSubjectResource))
            {
                existingSubjectResource.Resources.Add(resourceAttributeMatch);
            }
            else
            {
                SubjectResources subjectResources = new SubjectResources
                {
                    Subject = subjectMatch,
                    Resources = new List<AttributeMatchV2>() { resourceAttributeMatch }
                };
                allSubjectResources.Add(subjectResources.Subject.Urn, subjectResources);
            }
        }

        List<SubjectResources> subjectResourcesList = new List<SubjectResources>();

        foreach (KeyValuePair<string, SubjectResources> kvp in allSubjectResources)
        {
            subjectResourcesList.Add(kvp.Value);
        }

        return subjectResourcesList;
    }

    private static AttributeMatchV2 GetSubjectAttribute(NpgsqlDataReader reader)
    {
        return new AttributeMatchV2
        {
            Type = reader.GetFieldValue<string>("subject_type"),
            Value = reader.GetFieldValue<string>("subject_value"),
            Urn = reader.GetFieldValue<string>("subject_urn")
        };
    }

    private static AttributeMatchV2 GetResourceAttribute(NpgsqlDataReader reader)
    {
        return new AttributeMatchV2
        {
            Type = reader.GetFieldValue<string>("resource_type"),
            Value = reader.GetFieldValue<string>("resource_value"),
            Urn = reader.GetFieldValue<string>("resource_urn")
        };
    }

    /// <inheritdoc/>
    public async Task<List<ResourceSubjects>> FindSubjectsForResources(IEnumerable<string> resources, CancellationToken cancellationToken = default)
    {
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE resource_urn = ANY(@resources) AND deleted = false";

        await using NpgsqlCommand pgcom = _conn.CreateCommand(findResourcesSQL);
        pgcom.Parameters.AddWithValue("resources", resources.ToArray());
        await using NpgsqlDataReader reader = await pgcom.ExecuteReaderAsync(cancellationToken);

        Dictionary<string, ResourceSubjects> allResourceSubjects = new Dictionary<string, ResourceSubjects>();

        while (await reader.ReadAsync(cancellationToken))
        {
            AttributeMatchV2 subjectMatch = GetSubjectAttribute(reader);
            AttributeMatchV2 resourceAttributeMatch = GetResourceAttribute(reader);

            string owner = reader.GetFieldValue<string>("resource_owner");

            if (allResourceSubjects.TryGetValue(resourceAttributeMatch.Urn, out ResourceSubjects? resourceSubjectsExt))
            {
                resourceSubjectsExt.Subjects.Add(subjectMatch);
            }
            else 
            {
                ResourceSubjects resourceSubjects = new ResourceSubjects
                {
                   Resource = resourceAttributeMatch,
                   Subjects = new List<AttributeMatchV2>() { subjectMatch },
                   ResourceOwner = owner
                };
                allResourceSubjects.Add(resourceSubjects.Resource.Urn, resourceSubjects);
            }
        }

        List<ResourceSubjects> subjectResources = new List<ResourceSubjects>();

        foreach (KeyValuePair<string,ResourceSubjects> kvp in allResourceSubjects)
        {
            subjectResources.Add(kvp.Value);
        }

        return subjectResources;
    }

    /// <inheritdoc/>
    public async Task<List<UpdatedResourceSubject>> FindUpdatedResourceSubjects(DateTimeOffset lastUpdated, int limit, (Uri ResourceUrn, Uri SubjectUrn)? skipPast = null, CancellationToken cancellationToken = default)
    {
        const string selectUpdatedPairs = /*strpsql*/
            """
            SELECT resource_urn, subject_urn, updated_at, deleted
            FROM resourceregistry.resourcesubjects 
            WHERE (updated_at, resource_urn, subject_urn) > (@updated_at, @resource_urn, @subject_urn) 
            ORDER BY updated_at, resource_urn, subject_urn LIMIT @limit
            """;

        await using NpgsqlConnection conn = await _conn.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand selectCmd = new NpgsqlCommand(selectUpdatedPairs, conn);
        selectCmd.Parameters.AddWithValue("updated_at", NpgsqlDbType.TimestampTz, lastUpdated);
        selectCmd.Parameters.AddWithValue("resource_urn", NpgsqlDbType.Text, skipPast?.ResourceUrn.ToString() ?? string.Empty);
        selectCmd.Parameters.AddWithValue("subject_urn", NpgsqlDbType.Text, skipPast?.SubjectUrn.ToString() ?? string.Empty);
        selectCmd.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);
        await selectCmd.PrepareAsync(cancellationToken);
        await using NpgsqlDataReader reader = await selectCmd.ExecuteReaderAsync(cancellationToken);

        List<UpdatedResourceSubject> updatedResourceSubjects = new List<UpdatedResourceSubject>();

        while (await reader.ReadAsync(cancellationToken))
        {
            updatedResourceSubjects.Add(new UpdatedResourceSubject
            {
                ResourceUrn = new Uri(await reader.GetFieldValueAsync<string>("resource_urn", cancellationToken: cancellationToken)),
                SubjectUrn = new Uri(await reader.GetFieldValueAsync<string>("subject_urn", cancellationToken: cancellationToken)),
                UpdatedAt = await reader.GetFieldValueAsync<DateTimeOffset>("updated_at", cancellationToken: cancellationToken),
                Deleted = await reader.GetFieldValueAsync<bool>("deleted", cancellationToken: cancellationToken)
            });
        }

        return updatedResourceSubjects;
    }

    /// <inheritdoc/>
    public async Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken = default)
    {
        const string selectExistingPairsSQL = "SELECT subject_urn FROM resourceregistry.resourcesubjects WHERE resource_urn = @resourceurn AND deleted = false";
        const string softDeletePairSQL = "UPDATE resourceregistry.resourcesubjects SET deleted = true, updated_at = NOW() WHERE resource_urn = @resourceurn AND subject_urn = ANY(@subjecturns)";
        const string insertOrUpdatePairsSQL = /*strpsql*/
        """
        INSERT INTO resourceregistry.resourcesubjects (
            resource_type, resource_value, resource_urn, subject_type, subject_value, subject_urn, resource_owner, deleted
        )
        SELECT 
            @resourcetype AS resource_type, 
            @resourcevalue AS resource_value,
            @resourceurn AS resource_urn,
            unnest(@subjecttype) AS subject_type,
            unnest(@subjectvalue) AS subject_value,
            unnest(@subjecturn) AS subject_urn,
            @owner AS resource_owner,
            false AS deleted
        ON CONFLICT (resource_urn, subject_urn) DO UPDATE SET deleted = false, updated_at = NOW()
        """;

        await using NpgsqlConnection conn = await _conn.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        // Step 1: Retrieve existing subjects for this Resource from the database
        var existingSubjects = new List<string>();

        await using (var selectCmd = new NpgsqlCommand(selectExistingPairsSQL, conn, tx))
        {
            selectCmd.Parameters.AddWithValue("resourceurn", resourceSubjects.Resource.Urn);

            await selectCmd.PrepareAsync(cancellationToken);
            await using (var reader = await selectCmd.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    existingSubjects.Add(reader.GetString(0));
                }
            }
        }

        // Step 2: Mark subjects as deleted if they are no longer in resourceSubjects
        var subjectUrnsToDelete = existingSubjects
            .Where(x => resourceSubjects.Subjects.TrueForAll(s => s.Urn != x))
            .ToArray();

        if (subjectUrnsToDelete.Length > 0)
        {
            await using (var updateCmd = new NpgsqlCommand(softDeletePairSQL, conn, tx))
            {
                updateCmd.Parameters.AddWithValue("resourceurn", resourceSubjects.Resource.Urn);
                updateCmd.Parameters.AddWithValue("subjecturns", NpgsqlDbType.Array | NpgsqlDbType.Text, subjectUrnsToDelete);

                await updateCmd.PrepareAsync(cancellationToken);
                await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        // Step 3: Insert new resource-subject pairs, or set deleted=false on any previously deleted pairs now re-enabled.
        var newSubjects = resourceSubjects.Subjects.Where(s => !existingSubjects.Contains(s.Urn)).ToArray();

        var subjectTypes = newSubjects.Select(s => s.Type).ToArray();
        var subjectValues = newSubjects.Select(s => s.Value).ToArray();
        var subjectUrns = newSubjects.Select(s => s.Urn).ToArray();

        await using (var resourceCmd = new NpgsqlCommand(insertOrUpdatePairsSQL, conn, tx))
        {
            resourceCmd.Parameters.AddWithValue("resourcetype", NpgsqlDbType.Text, resourceSubjects.Resource.Type);
            resourceCmd.Parameters.AddWithValue("resourcevalue", NpgsqlDbType.Text, resourceSubjects.Resource.Value);
            resourceCmd.Parameters.AddWithValue("resourceurn", NpgsqlDbType.Text, resourceSubjects.Resource.Urn);
            resourceCmd.Parameters.AddWithValue("owner", NpgsqlDbType.Text, resourceSubjects.ResourceOwner);
            resourceCmd.Parameters.AddWithValue("subjecttype", NpgsqlDbType.Array | NpgsqlDbType.Text, subjectTypes);
            resourceCmd.Parameters.AddWithValue("subjectvalue", NpgsqlDbType.Array | NpgsqlDbType.Text, subjectValues);
            resourceCmd.Parameters.AddWithValue("subjecturn", NpgsqlDbType.Array | NpgsqlDbType.Text, subjectUrns);

            await resourceCmd.PrepareAsync(cancellationToken);
            await resourceCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    private static async ValueTask<ServiceResource> GetServiceResource(NpgsqlDataReader reader, CancellationToken cancellationToken = default)
    {
        var json = await reader.GetFieldValueAsync<JsonDocument>("serviceresourcejson", cancellationToken);
        int versionId = await reader.GetFieldValueAsync<int>("version_id", cancellationToken);

        ServiceResource resource = json.Deserialize<ServiceResource>(JsonSerializerOptions) ??
            throw new SqlNullValueException("Got null when trying to parse ServiceResource");

        resource.VersionId = versionId;
        return resource;
    }
}
