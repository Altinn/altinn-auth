using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Enums;

namespace Altinn.AccessManagement.Tests.Unit.Helpers.Extensions
{
    [UnitTest]
    public class EnumExtensionTest
    {
        [Fact]
        public void EnumValue_ValidMemberValueString_ReturnsTrueAndEnum()
        {
            string enumMemberValueString = "urn:altinn:person:uuid";
            bool result = EnumExtensions.EnumValue<UuidType>(enumMemberValueString, out UuidType enumValue);
            Assert.True(result);
            Assert.Equal(UuidType.Person, enumValue);
        }
    }
}
