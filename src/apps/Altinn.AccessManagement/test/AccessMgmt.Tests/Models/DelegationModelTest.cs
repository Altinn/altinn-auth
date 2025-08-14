using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessManagement.Tests.Models
{
    [Collection("Models Test")]
    public class DelegationModelTest
    {
        [Fact]
        public void NotAllowedToSetIdToUuidV4()
        {
            Assert.Equal(
                "Id must be a version 7 UUID (Parameter 'value')",
                Assert.Throws<ArgumentException>(() => new Delegation { Id = Guid.NewGuid() }).Message);
        }
    }
}
