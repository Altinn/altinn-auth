using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Defines the view mode of the consent portal for external users.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ConsentPortalViewMode
    {
        [EnumMember(Value = "hide")]
        Hide = 0,

        [EnumMember(Value = "show")]
        Show = 1,
    }
}
