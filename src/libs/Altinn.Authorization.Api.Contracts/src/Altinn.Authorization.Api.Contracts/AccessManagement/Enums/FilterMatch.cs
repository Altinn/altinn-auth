using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

/// <summary>
/// How the list filters on a clients endpoint select clients. It applies to every filter the caller
/// sets, so a request filtering on both roles and packages is matched the same way on both.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FilterMatch
{
    /// <summary>
    /// A client is selected when it matches at least one value in each filter the caller set.
    /// </summary>
    [JsonStringEnumMemberName("any")]
    Any = 0,

    /// <summary>
    /// A client is selected when it matches every value in each filter the caller set. Packages held
    /// through a role and packages delegated directly count as one set, so a client can cover the
    /// packages filter through a combination of both.
    /// </summary>
    [JsonStringEnumMemberName("all")]
    All = 1
}
