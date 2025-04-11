using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegister;

namespace Altinn.Authorization.Integration.Tests.ResourceRegister;

/// <summary>
/// Tests for the ResourceUpdatedEndpoint.
/// </summary>
/// <param name="fixture">The platform fixture to provide necessary services.</param>
public class ServiceOwnersEndpointsTest : IClassFixture<PlatformFixture>
{
    public ServiceOwnersEndpointsTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnResourceRegisterOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("ResourceRegister");
        ResourceRegister = fixture.GetService<IAltinnResourceRegister>();
    }

    private IAltinnResourceRegister ResourceRegister { get; }

    /// <summary>
    /// Tests getting all service owners from resource registry.
    /// </summary>
    [Fact]
    public async Task TestGetAllServiceOwners()
    {
        var response = await ResourceRegister.GetServiceOwners(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessful);
        Assert.True(response.Content.Orgs.Count > 0);
    }
}
