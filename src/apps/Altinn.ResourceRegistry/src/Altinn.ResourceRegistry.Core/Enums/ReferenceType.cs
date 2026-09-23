using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.Authorization.ModelUtils;

namespace Altinn.ResourceRegistry.Core.Enums;

/// <summary>
/// Enum for reference types of resources in the resource registry
/// </summary>
[StringEnumConverter]
public enum ReferenceType 
    : int
{
    [EnumMember(Value = "Default")]
    [JsonStringEnumMemberName("Default")]
    Default = 0,

    [EnumMember(Value = "Uri")]
    [JsonStringEnumMemberName("Uri")]
    Uri = 1,
    
    [EnumMember(Value = "DelegationSchemeId")]
    [JsonStringEnumMemberName("DelegationSchemeId")]
    DelegationSchemeId = 2,

    [EnumMember(Value = "MaskinportenScope")]
    [JsonStringEnumMemberName("MaskinportenScope")]
    MaskinportenScope = 3,
    
    [EnumMember(Value = "ServiceCode")]
    [JsonStringEnumMemberName("ServiceCode")]
    ServiceCode = 4,

    [EnumMember(Value = "ServiceEditionCode")]
    [JsonStringEnumMemberName("ServiceEditionCode")]
    ServiceEditionCode = 5,

    [EnumMember(Value = "ApplicationId")]
    [JsonStringEnumMemberName("ApplicationId")]
    ApplicationId = 6,

    [EnumMember(Value = "ServiceEditionVersion")]
    [JsonStringEnumMemberName("ServiceEditionVersion")]
    ServiceEditionVersion = 7,
}
