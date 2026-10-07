using System.Runtime.Serialization;

namespace Altinn.Authorization.Enums;

/// <summary>
/// Restricts which kinds of access are considered when authorizing a request on the external Authorize API.
/// </summary>
public enum AccessRestriction
{
    /// <summary>
    /// Default behavior. No restriction, all access is considered, including client-delegated access.
    /// </summary>
    [EnumMember(Value = "None")]
    None = 0,

    /// <summary>
    /// Only client-delegated access received through a specific via-party organization is considered.
    /// Requires the <c>urn:altinn:via-party:organization:identifier-no</c> attribute to be present in the request.
    /// Applies to both access packages and resources.
    /// </summary>
    [EnumMember(Value = "ClientDelegation")]
    ClientDelegation = 1,

    /// <summary>
    /// Only access held directly by the subject (direct delegations and main-unit inheritance) is considered.
    /// Both client-delegated access and keyrole (org-to-org) inherited access are excluded.
    /// </summary>
    [EnumMember(Value = "DirectAndHierarchy")]
    DirectAndHierarchy = 2,
}
