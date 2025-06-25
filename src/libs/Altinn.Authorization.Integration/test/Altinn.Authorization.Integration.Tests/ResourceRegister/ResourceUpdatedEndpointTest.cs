using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.ResourceRegistry;

namespace Altinn.Authorization.Integration.Tests.ResourceRegistry;

/// <summary>
/// Tests for the ResourceUpdatedEndpoint.
/// </summary>
/// <param name="fixture">The platform fixture to provide necessary services.</param>
public class ResourceUpdatedEndpointTest : IClassFixture<PlatformFixture>
{
    public ResourceUpdatedEndpointTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnResourceRegistryOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("ResourceRegistry");
        ResourceRegistry = fixture.GetService<IAltinnResourceRegistry>();
    }

    private IAltinnResourceRegistry ResourceRegistry { get; }

    /// <summary>
    /// Tests streaming of resources with a limited number of iterations.
    /// </summary>
    /// <param name="iterations">The maximum number of iterations to process.</param>
    [Theory]
    [InlineData(5)]
    public async Task TestStreamParties(int iterations)
    {
        await foreach (var role in await ResourceRegistry.StreamResources(null, TestContext.Current.CancellationToken))
        {
            if (iterations-- <= 0)
            {
                break;
            }

            Assert.True(role.IsSuccessful);
        }
    }

    /// <summary>
    /// Tests streaming of roles, ensuring that subsequent pages are different.
    /// </summary>
    [Fact]
    public async Task TestStreamRolesWithNextPage()
    {
        var firstPage = await GetPage(null, TestContext.Current.CancellationToken);
        Assert.NotNull(firstPage);
        var secondPage = await GetPage(firstPage?.Content?.Links?.Next, TestContext.Current.CancellationToken);
        Assert.NotEqual(firstPage?.Content?.Links?.Next, secondPage?.Content?.Links?.Next);
    }

    /// <summary>
    /// Retrieves a page of streamed resources.
    /// </summary>
    /// <param name="nextPage">The next page link, if available.</param>
    /// <returns>A task representing the asynchronous operation, returning a platform response with resource updates.</returns>
    private async Task<PlatformResponse<PageStream<ResourceUpdatedModel>>> GetPage(string nextPage = null, CancellationToken cancellationToken = default)
    {
        await foreach (var role in await ResourceRegistry.StreamResources(nextPage, cancellationToken))
        {
            return role;
        }

        return null;
    }
}
