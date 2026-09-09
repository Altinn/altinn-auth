using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models.IdPortenAuthorization
{
    /// <summary>
    /// Model representing an ID-porten authorization
    /// </summary>
    public class IdPortenAuthorization
    {
        /// <summary>
        /// Id of the authorization
        /// </summary>
        [JsonPropertyName("authorization_id")]
        public string AuthorizationId { get; set; }

        /// <summary>
        /// Client id of the authorization
        /// </summary>
        [JsonPropertyName("client_id")]
        public required string ClientId { get; set; }

        /// <summary>
        /// Client name of the authorization
        /// </summary>
        [JsonPropertyName("client_name")]
        public string ClientName { get; set; }

        /// <summary>
        /// Long client description of the authorization
        /// </summary>
        [JsonPropertyName("client_description")]
        public string ClientDescription { get; set; }

        /// <summary>
        /// Timestamp of when the authorization was granted
        /// </summary>
        [JsonPropertyName("authorized_at")]
        public long AuthorizedAt { get; set; }

        /// <summary>
        /// Timestamp of when the authorization expires
        /// </summary>
        [JsonPropertyName("expires")]
        public long Expires { get; set; }

        /// <summary>
        /// Scopes granted for the authorization
        /// </summary>
        [JsonPropertyName("scopes")]
        public IEnumerable<Scope> Scopes { get; set; }

        /// <summary>
        /// User agent string of the device that granted the authorization
        /// </summary>
        [JsonPropertyName("user_agent")]
        public string UserAgent { get; set; }

        /// <summary>
        /// Consumer organization
        /// </summary>
        [JsonPropertyName("consumer")]
        public Consumer Consumer { get; set; }
    }

    /// <summary>
    /// Model for consumer organization
    /// </summary>
    public class Consumer
    {
        /// <summary>
        /// Authority, iso6523-actorid-upis
        /// </summary>
        [JsonPropertyName("authority")]
        public string Authority { get; set; }

        /// <summary>
        /// Organization number and prefix 0192:
        /// </summary>
        [JsonPropertyName("ID")]
        public string Id { get; set; }

        /// <summary>
        /// Organization number of the consumer organization
        /// </summary>
        [JsonPropertyName("orgNo")]
        public string OrgNo { get; set; }
    }

    /// <summary>
    /// Model for scope of the authorization
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Technical scope name, e.g. "altinn:serviceowner/instances.read"
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Description (title) of the scope
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Long description of the scope
        /// </summary>
        [JsonPropertyName("long_description")]
        public string LongDescription { get; set; }

        /// <summary>
        /// Whether the scope requires user consent or not
        /// </summary>
        [JsonPropertyName("requires_user_consent")]
        public bool RequiresUserConsent { get; set; }

        /// <summary>
        /// Organization number of the owner of the scope
        /// </summary>
        [JsonPropertyName("owner_orgno")]
        public string OwnerOrgNo { get; set; }
    }
}
