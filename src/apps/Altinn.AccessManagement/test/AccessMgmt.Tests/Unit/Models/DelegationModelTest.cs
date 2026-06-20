using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.Tests.Unit.Models
{
    [UnitTest]
    [Collection("Models Test")]
    public class DelegationModelTest
    {
        [Fact]
        public void SetId_Version4Uuid_ThrowsArgumentException()
        {
            Assert.Equal(
                "Id must be a version 7 UUID (Parameter 'value')",
                Assert.Throws<ArgumentException>(() => new Delegation { Id = Guid.NewGuid() }).Message);
        }
    }
}
