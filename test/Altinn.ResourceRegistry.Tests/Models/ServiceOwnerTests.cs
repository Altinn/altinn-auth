#nullable enable

using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.ServiceOwners;

namespace Altinn.ResourceRegistry.Tests.Models;

public class ServiceOwnerTests
{
    [Fact]
    public void Equals_ReturnsTrue_WhenComparingEqualServiceOwners()
    {
        var org1 = new Core.Models.Org
        {
            Name = new Dictionary<string, string>
            {
                { "nb", "Org" },
                { "en", "Org" },
            },
            Logo = "logo",
            Orgnr = "123456789",
            Homepage = "https://org.com",
            Environments = [ "test", "prod" ],
        };

        var org2 = new Core.Models.Org
        {
            Name = new Dictionary<string, string>
            {
                { "en", "Org" },
                { "nb", "Org" },
            },
            Logo = "logo",
            Orgnr = "123456789",
            Homepage = "https://org.com",
            Environments = [ "prod", "test" ],
        };

        var so1 = ServiceOwner.Create("test", org1);
        var so2 = ServiceOwner.Create("test", org2);

        so1.Equals(so2).Should().BeTrue();
        so1.GetHashCode().Should().Be(so2.GetHashCode());
    }
}
