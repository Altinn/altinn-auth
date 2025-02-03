using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Cli.Config;

/// <summary>
/// Represents the conf.json settings for an application under apps.
/// </summary>
public class AppsConfig
{
    /// <summary>
    /// Gets or sets the database configuration.
    /// </summary>
    [JsonPropertyName("database")]
    public DatabaseConfig Database { get; set; }

    /// <summary>
    /// Gets or sets the infrastructure configuration.
    /// </summary>
    [JsonPropertyName("infra")]
    public InfraConfig Infra { get; set; }

    /// <summary>
    /// Represents the configuration for the database, including its name, prefix, and schemas.
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the prefix to be used for database-related entities, such as user roles.
        /// </summary>
        [JsonPropertyName("prefix")]
        public string Prefix { get; set; }

        /// <summary>
        /// Gets or sets the schemas and their associated configuration.
        /// </summary>
        [JsonPropertyName("schema")]
        public IDictionary<string, JsonElement> Schemas { get; set; }
    }

    /// <summary>
    /// Represents the configuration for the infrastructure, including details related to Terraform.
    /// </summary>
    public class InfraConfig
    {
        /// <summary>
        /// Gets or sets the Terraform-specific configuration.
        /// </summary>
        [JsonPropertyName("terraform")]
        public TerraformConfig Terraform { get; set; }

        /// <summary>
        /// Represents the Terraform configuration, including the path to the state file.
        /// </summary>
        public class TerraformConfig
        {
            /// <summary>
            /// Gets or sets the path to the Terraform state file.
            /// </summary>
            [JsonPropertyName("stateFile")]
            public string StateFile { get; set; }
        }
    }
}
