using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Enums;

namespace Altinn.AccessManagement.Tests.Unit.Helpers.Extensions
{
    [UnitTest]
    public class EnumExtensionTest
    {
        private enum Source
        {
            First = 0,
            Second = 1,
            OnlyInSource = 2,
        }

        private enum Target
        {
            Second = 5,
            FIRST = 7,
        }

        [Fact]
        public void EnumValue_ValidMemberValueString_ReturnsTrueAndEnum()
        {
            string enumMemberValueString = "urn:altinn:person:uuid";
            bool result = EnumExtensions.EnumValue<UuidType>(enumMemberValueString, out UuidType enumValue);
            Assert.True(result);
            Assert.Equal(UuidType.Person, enumValue);
        }

        [Fact]
        public void MapByNameOrValue_SameName_ReturnsMemberWithSameName()
        {
            Assert.Equal(Target.Second, Source.Second.MapByNameOrValue<Target>());
        }

        [Fact]
        public void MapByNameOrValue_NameDiffersInCase_ReturnsMemberIgnoringCase()
        {
            Assert.Equal(Target.FIRST, Source.First.MapByNameOrValue<Target>());
        }

        [Fact]
        public void MapByNameOrValue_NameMissingInTarget_ReturnsSameNumericValue()
        {
            Assert.Equal((Target)2, Source.OnlyInSource.MapByNameOrValue<Target>());
        }

        [Fact]
        public void MapByNameOrValue_UndefinedValue_ReturnsSameNumericValue()
        {
            Assert.Equal((Target)99, ((Source)99).MapByNameOrValue<Target>());
        }
    }
}
