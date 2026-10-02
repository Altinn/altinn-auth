using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// Controls how client-delegated access is considered when looking up delegations.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthContextDto
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
    /// Only access held directly by the subject is considered. Client-delegated and keyrole (org-to-org) inherited access are excluded.
    /// </summary>
    [EnumMember(Value = "DirectAccess")]
    DirectAccess = 2,
}
