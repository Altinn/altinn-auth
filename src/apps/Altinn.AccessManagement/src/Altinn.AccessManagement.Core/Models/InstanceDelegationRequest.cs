using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models
{
    /// <summary>
    /// Represents a delegation of an instance, including the instance owner and the actions that are delegated.
    /// </summary>
    public class InstanceDelegationRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier for the authorization rule in Altinn II.
        /// </summary>
        [JsonPropertyName("authorizationruleid")]
        [Required(ErrorMessage = "The original instance delegation id must be provided")]
        public int AuthorizationRuleID { get; set; }

        /// <summary>
        /// Gets or sets the date and time from which the value is considered valid.
        /// </summary>
        [JsonPropertyName("created")]
        [Required(ErrorMessage = "The created datetime must be provided")]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the recipient.
        /// </summary>
        [JsonPropertyName("touuid")]
        [Required(ErrorMessage = "ToUuid must be a valid non-empty GUID")]
        public Guid ToUuid { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the party the instance right is for.
        /// </summary>
        [JsonPropertyName("fromuuid")]
        [Required(ErrorMessage = "FromUuid must be a valid non-empty GUID")]
        public Guid FromUuid { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the user or process that performed the action.
        /// </summary>
        [JsonPropertyName("performedby")]
        [Required(ErrorMessage = "PerformedBy must be a valid non-empty GUID")]
        public Guid PerformedBy { get; set; }

        /// <summary>
        /// Gets or sets the resource identifier associated with the current instance.
        /// </summary>
        [JsonPropertyName("resourceid")]
        [Required(ErrorMessage = "ResourceId cannot be null or empty")]
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this instance.
        /// </summary>
        [JsonPropertyName("instanceid")]
        [Required(ErrorMessage = "InstanceId cannot be null or empty")]
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the list of actions.
        /// </summary>
        [JsonPropertyName("actions")]
        [Required(ErrorMessage = "At least one action must be specified")]
        [MinLength(1, ErrorMessage = "At least one action must be specified")]
        public IEnumerable<string> Actions { get; set; }
    }
}
