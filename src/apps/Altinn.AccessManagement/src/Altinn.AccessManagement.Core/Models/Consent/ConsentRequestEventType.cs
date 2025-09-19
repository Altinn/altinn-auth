using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Enum for the status of a consent request
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConsentRequestEventType
    {
        [EnumMember(Value = "created")]
        Created = 0,

        [EnumMember(Value = "rejected")]
        Rejected = 1,

        [EnumMember(Value = "accepted")]
        Accepted = 2,

        [EnumMember(Value = "revoked")]
        Revoked = 3,

        [EnumMember(Value = "deleted")]
        Deleted = 4,

        [EnumMember(Value = "expired")]
        Expired = 5,

        [EnumMember(Value = "used")]
        Used = 6
    }
}
