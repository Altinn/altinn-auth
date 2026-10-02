using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models
{
    /// <summary>
    /// Controls how client-delegated access is considered when looking up delegation changes.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AuthContext
    {
        /// <summary>
        /// Default behavior. All access is considered, including client-delegated access.
        /// </summary>
        [EnumMember(Value = "All")]
        All = 0,

        /// <summary>
        /// Only client-delegated access received through a specific via-party organization is considered.
        /// Applies to both access packages and resources.
        /// </summary>
        [EnumMember(Value = "ClientAccess")]
        ClientAccess = 1,

        /// <summary>
        /// Only access held directly by the subject (direct delegations and main-unit inheritance) is considered.
        /// Both client-delegated access and keyrole (org-to-org) inherited access are excluded.
        /// </summary>
        [EnumMember(Value = "DirectAccess")]
        DirectAccess = 2,
    }
}
