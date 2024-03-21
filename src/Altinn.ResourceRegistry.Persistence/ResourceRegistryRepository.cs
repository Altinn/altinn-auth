using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using Altinn.Authorization.ABAC.Utils;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

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
        CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            SELECT identifier, created, modified, serviceresourcejson
            FROM resourceregistry.resources
            WHERE (@id IS NULL OR serviceresourcejson ->> 'identifier' ILIKE concat('%', @id, '%'))
            AND (@title IS NULL OR serviceresourcejson ->> 'title' ILIKE concat('%', @title, '%'))
            AND (@description IS NULL OR serviceresourcejson ->> 'description' ILIKE concat('%', @description, '%'))
            AND (@resourcetype IS NULL OR serviceresourcejson ->> 'resourceType' ILIKE @resourcetype::text)
            AND (@keyword IS NULL OR serviceresourcejson ->> 'keywords' ILIKE concat('%', @keyword, '%'))
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithNullableValue("id", NpgsqlDbType.Text, resourceSearch.Id);
            pgcom.Parameters.AddWithNullableValue("title", NpgsqlDbType.Text, resourceSearch.Title);
            pgcom.Parameters.AddWithNullableValue("description", NpgsqlDbType.Text, resourceSearch.Description);
            pgcom.Parameters.AddWithNullableValue("resourcetype", NpgsqlDbType.Text, resourceSearch.ResourceType?.ToString());
            pgcom.Parameters.AddWithNullableValue("keyword", NpgsqlDbType.Text, resourceSearch.Keyword);

            return await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
                .ToListAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Authorization // ResourceRegistryRepository // Search // Exception");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            INSERT INTO resourceregistry.resources(
	            identifier,
	            created,
	            modified,
	            serviceresourcejson
            )
            VALUES (
	            @identifier,
	            Now(),
	            Now(),
	            @serviceresourcejson
            )
            RETURNING identifier, created, modified, serviceresourcejson
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
                .SelectAwait(GetServiceResource)
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
                .SelectAwait(GetServiceResource)
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
    public async Task<ServiceResource?> GetResource(string id, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            SELECT identifier, created, modified, serviceresourcejson
            FROM resourceregistry.resources
            WHERE identifier = @identifier   
            ";

        try
        {
            await using var pgcom = _conn.CreateCommand(QUERY);
            pgcom.Parameters.AddWithValue("identifier", NpgsqlDbType.Text, id);

            var serviceResource = await pgcom.ExecuteEnumerableAsync(cancellationToken)
                .SelectAwait(GetServiceResource)
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
    public async Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default)
    {
        const string QUERY = /*strpsql*/@"
            UPDATE resourceregistry.resources
            SET
	            modified = Now(),
	            serviceresourcejson = @serviceresourcejson
            WHERE identifier = @identifier
            RETURNING identifier, created, modified, serviceresourcejson
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
                .SelectAwait(GetServiceResource)
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
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE subject_urn = ANY(@subjects)";

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
        const string findResourcesSQL = /*strpsql*/@"select * from resourceregistry.resourcesubjects WHERE resource_urn = ANY(@resources)";

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
    public async Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken = default)
    {
        const string deleteResourcesSQL = /*strpsql*/@"DELETE from resourceregistry.resourcesubjects WHERE resource_urn = ANY(@resources)";
        const string insertResourceSubjectsSQL = /*strpsql*/@"INSERT INTO resourceregistry.resourcesubjects(
                                                              resource_type, resource_value, resource_urn, subject_type, subject_value, subject_urn, resource_owner) 
                                                              VALUES(@resourcetype, @resourcevalue, @resourceurn, @subjecttype, @subjectvalue, @subjecturn, @owner)";

        string[] resourcesToDelete = [resourceSubjects.Resource.Urn];
     
        await using NpgsqlConnection conn = await _conn.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await using NpgsqlCommand pgcom = _conn.CreateCommand(deleteResourcesSQL);
        pgcom.Parameters.AddWithValue("resources", resourcesToDelete);
        await pgcom.ExecuteNonQueryAsync(cancellationToken);
        
        await using var resourceCmd = _conn.CreateCommand(insertResourceSubjectsSQL);
        NpgsqlParameter resourcetypeParam = resourceCmd.Parameters.Add("resourcetype", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter resourcevalueParam = resourceCmd.Parameters.Add("resourcevalue", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter resourceurnParam = resourceCmd.Parameters.Add("resourceurn", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter subjecttypeParam = resourceCmd.Parameters.Add("subjecttype", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter subjectvalueParam = resourceCmd.Parameters.Add("subjectvalue", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter subjecturnParam = resourceCmd.Parameters.Add("subjecturn", NpgsqlTypes.NpgsqlDbType.Text);
        NpgsqlParameter ownerParam = resourceCmd.Parameters.Add("owner", NpgsqlTypes.NpgsqlDbType.Text);

        foreach (AttributeMatchV2 subjectAttribute in resourceSubjects.Subjects)
        {
            resourcetypeParam.SetValue(resourceSubjects.Resource.Type);
            resourcevalueParam.SetValue(resourceSubjects.Resource.Value);
            resourceurnParam.SetValue(resourceSubjects.Resource.Urn);
            subjecttypeParam.SetValue(subjectAttribute.Type);
            subjectvalueParam.SetValue(subjectAttribute.Value);
            subjecturnParam.SetValue(subjectAttribute.Urn);
            ownerParam.SetValue(resourceSubjects.ResourceOwner);
            await resourceCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    private static async ValueTask<ServiceResource> GetServiceResource(NpgsqlDataReader reader)
    {
        var json = await reader.GetFieldValueAsync<JsonDocument>("serviceresourcejson");

        return json.Deserialize<ServiceResource>(JsonSerializerOptions) ??
            throw new SqlNullValueException("Got null when trying to parse ServiceResource");
    }
}
