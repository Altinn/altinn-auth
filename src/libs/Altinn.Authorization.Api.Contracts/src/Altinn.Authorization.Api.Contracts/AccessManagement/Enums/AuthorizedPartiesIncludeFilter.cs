using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

/// <summary>
/// Enum type to use for include filter parameters
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthorizedPartiesIncludeFilter
{
    /// <summary>
    /// The include filter is False
    /// </summary>
    [JsonStringEnumMemberName("false")]
    False = 0,

    /// <summary>
    /// The include filter is True
    /// </summary>
    [JsonStringEnumMemberName("true")]
    True = 1,

    /// <summary>
    /// Automatic behavior. Meaning the system or user preference will decide the inclusion.
    /// </summary>
    [JsonStringEnumMemberName("auto")]
    Auto = 2
}
