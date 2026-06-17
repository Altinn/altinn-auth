using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessManagement.Tests.Unit.Helpers.Extensions
{
    [UnitTest]
    public class GuidExtensionTest
    {
        [Fact]
        public void IsVersion7Uuid_Version7Guid_ReturnsTrue()
        {
            // Test that 100 different version 7 UUIDs are recognized as such
            for (int i = 0; i < 100; i++)
            {
                Guid v7Uuid = Guid.CreateVersion7();
                Assert.True(v7Uuid.IsVersion7Uuid());
                Assert.False(v7Uuid.IsVersion4Uuid());
            }
        }

        [Fact]
        public void IsVersion7Uuid_Version4Guid_ReturnsFalse()
        {
            // Test that 100 different version 4 UUIDs are recognized as such
            for (int i = 0; i < 100; i++)
            {
                Guid v4Uuid = Guid.NewGuid();
                Assert.True(v4Uuid.IsVersion4Uuid());
                Assert.False(v4Uuid.IsVersion7Uuid());
            }
        }
    }
}
