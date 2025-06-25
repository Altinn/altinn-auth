using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;

namespace Altinn.Authorization.Integration.Tests.ResourceRegistry;

/// <summary>
/// Tests for the ResourceUpdatedEndpoint.
/// </summary>
/// <param name="fixture">The platform fixture to provide necessary services.</param>
public class ServiceOwnersEndpointsTest : IClassFixture<PlatformFixture>
{
    public ServiceOwnersEndpointsTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnResourceRegistryOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("ResourceRegistry");
        ResourceRegistry = fixture.GetService<IAltinnResourceRegistry>();
    }

    private IAltinnResourceRegistry ResourceRegistry { get; }

    /// <summary>
    /// Tests getting all service owners from resource registry.
    /// </summary>
    [Fact]
    public async Task TestGetAllServiceOwners()
    {
        var response = await ResourceRegistry.GetServiceOwners(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessful);
        Assert.True(response.Content.Orgs.Count > 0);
    }
}
