using System.Text.Json.Serialization;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Apps.Enums;

/// <summary>
/// Enum for different right delegation status responses
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DelegationStatusExternal
{
    /// <summary>
    /// Right was not delegated
    /// </summary>
    NotDelegated = 0,

    /// <summary>
    /// Right was delegated
    /// </summary>
    Delegated = 1
}
