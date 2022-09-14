using Altinn.Platform.Authorization.Configuration;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.PostgresTypes;
using System.Data;
using System.Runtime.Serialization;
using System.Text.Encodings.Web;

namespace Altinn.ResourceRegistry.Persistence
{
    public class ResourceRepository : IResourceRegistryRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly string getResource = "SELECT * FROM resourceregistry.get_resource(@_identifier)";
        private readonly string searchForResource = "SELECT * FROM resourceregistry.search_for_resource(@_searchterm)";
        private readonly string createResource = "SELECT * FROM resourceregistry.create_resource(@_identifier, @_created, @_modified, @_serviceresourcejson)";
        private readonly string deleteResource = "SELECT * FROM resourceregistry.delete_resource(@_identifier)";

        public ResourceRepository(
    IOptions<PostgreSQLSettings> postgresSettings,
    ILogger<ResourceRepository> logger)
        {
            _logger = logger;
            _connectionString = string.Format(
                postgresSettings.Value.ConnectionString,
                postgresSettings.Value.AuthorizationDbPwd);
        }

        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(searchForResource, conn);
                pgcom.Parameters.AddWithValue("_searchterm", resourceSearch.SearchTerm);

                List<ServiceResource> serviceResources = new List<ServiceResource>();

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                while (reader.Read())
                {
                    serviceResources.Add(getServiceResource(reader));
                }

                return serviceResources;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // Search // Exception");
                throw;
            }
        }

        public async Task<ServiceResource> CreateResource(ServiceResource resource)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(resource, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(createResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", resource.Identifier);
                pgcom.Parameters.AddWithValue("_created", NpgsqlTypes.NpgsqlDbType.TimestampTz ,DateTime.UtcNow);
                pgcom.Parameters.AddWithValue("_modified", NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTime.UtcNow);
                pgcom.Parameters.AddWithValue("_serviceresourcejson", NpgsqlTypes.NpgsqlDbType.Jsonb, json);


                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return getServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("duplicate key value violates unique constraint"))
                {
                    return new ServiceResource();
                }
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
                throw;
            }
        }

        public async Task<ServiceResource> DeleteResource(string id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(deleteResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", id);

                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return getServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // DeleteResource // Exception");
                throw;
            }
        }

        public async Task<ServiceResource> GetResource(string id)
        {
            try
            {
                await using NpgsqlConnection conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                NpgsqlCommand pgcom = new NpgsqlCommand(getResource, conn);
                pgcom.Parameters.AddWithValue("_identifier", id);
                
                using NpgsqlDataReader reader = pgcom.ExecuteReader();
                if (reader.Read())
                {
                    return getServiceResource(reader);
                }

                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Authorization // ResourceRegistryRepository // GetResource // Exception");
                throw;
            }
        }

        public Task<ServiceResource> UpdateResource(ServiceResource resource)
        {
            throw new NotImplementedException();
        }

        private static ServiceResource getServiceResource(NpgsqlDataReader reader)
        {
            if (reader["serviceresourcejson"] != DBNull.Value)
            {
                var jsonb = reader.GetString("serviceresourcejson");

                ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(jsonb, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                return resource;
            }
            return null;
        }
    }
}