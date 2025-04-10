using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessManagement.Tests.Helpers.Extensions
{
    public class GuidExtensionTest
    {
        [Fact]
        public void GuidIsVersion7Uuid_v7_True()
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
        public void GuidIsVersion7Uuid_v4_False()
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
