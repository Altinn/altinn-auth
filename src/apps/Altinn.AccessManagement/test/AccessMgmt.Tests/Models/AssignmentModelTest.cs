using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.Tests.Models
{
    [UnitTest]
    [Collection("Models Test")]
    public class AssignmentModelTest
    {
        [Fact]
        public void NotAllowedToSetIdToUuidV4()
        {
            Assert.Equal(
                "Id must be a version 7 UUID (Parameter 'value')",
                Assert.Throws<ArgumentException>(() => new Assignment { Id = Guid.NewGuid() }).Message);
        }
    }
}
