using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;

namespace Altinn.Authorization.Integration.Tests.ResourceRegistry;

/// <summary>
/// Contains test cases for the ResourceGetEndpoint.
/// </summary>
public class ResourceGetEndpointTest : IClassFixture<PlatformFixture>
{
    public ResourceGetEndpointTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnResourceRegistryOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("ResourceRegistry");
        ResourceRegistry = fixture.GetService<IAltinnResourceRegistry>();
    }

    private IAltinnResourceRegistry ResourceRegistry { get; }

    /// <summary>
    /// Gets a specific resource from resource registry
    /// </summary>
    /// <param name="resourceId">resource ID</param>
    /// <returns></returns>
    [Theory]
    [InlineData("altinn_access_management")]
    public async Task GetResource(string resourceId)
    {
        var result = await ResourceRegistry.GetResource(resourceId, TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccessful);
    }
}
