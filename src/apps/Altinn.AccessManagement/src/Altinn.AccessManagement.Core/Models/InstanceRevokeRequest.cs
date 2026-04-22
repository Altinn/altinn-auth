using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Altinn.AccessManagement.Core.Models
{
    /// <summary>
    /// Represents a revoke all actions delegated for an instance between two parties
    /// </summary>
    public class InstanceRevokeRequest
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
    }
}
