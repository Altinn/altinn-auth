using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.Authorization.ModelUtils;

namespace Altinn.ResourceRegistry.Core.Enums;

/// <summary>
/// Enum for the different reference sources for resources in the resource registry
/// </summary>
[StringEnumConverter]
public enum ReferenceSource 
    : int
{
    [EnumMember(Value = "Default")]
    [JsonStringEnumMemberName("Default")]
    Default = 0,

    [EnumMember(Value = "Altinn1")]
    [JsonStringEnumMemberName("Altinn1")]
    Altinn1 = 1,

    [EnumMember(Value = "Altinn2")]
    [JsonStringEnumMemberName("Altinn2")]
    Altinn2 = 2,

    [EnumMember(Value = "Altinn3")]
    [JsonStringEnumMemberName("Altinn3")]
    Altinn3 = 3,

    [EnumMember(Value = "ExternalPlatform")]
    [JsonStringEnumMemberName("ExternalPlatform")]
    ExternalPlatform = 4,
}
