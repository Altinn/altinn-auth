using System.Runtime.Serialization;

namespace Altinn.ResourceRegistry.Core.Enums
{
    public enum ReferenceType : int
    {
        [EnumMember(Value = "Default")]
        Default = 0,

        [EnumMember(Value = "ServiceCodeVersion")]
        ServiceCodeVersion = 1,

        [EnumMember(Value = "OrgApp")]
        OrgApp = 2,

        [EnumMember(Value = "Uri")]
        Uri = 3,

    }
}
