using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Altinn.Authorization.ModelUtils;

namespace Altinn.ResourceRegistry.Core.Enums;

/// <summary>
/// Defines the type of party that a resource is targeting
/// </summary>
[StringEnumConverter]
public enum ResourcePartyType
{
    [EnumMember(Value = "PrivatePerson")]
    [JsonStringEnumMemberName("PrivatePerson")]
    PrivatePerson = 0,

    [EnumMember(Value = "LegalEntityEnterprise")]
    [JsonStringEnumMemberName("LegalEntityEnterprise")]
    LegalEntityEnterprise = 1,

    [EnumMember(Value = "Company")]
    [JsonStringEnumMemberName("Company")]
    Company = 2,

    [EnumMember(Value = "BankruptcyEstate")]
    [JsonStringEnumMemberName("BankruptcyEstate")]
    BankruptcyEstate = 3,

    [EnumMember(Value = "SelfRegisteredUser")]
    [JsonStringEnumMemberName("SelfRegisteredUser")]
    SelfRegisteredUser = 4,
}
