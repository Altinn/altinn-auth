using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Register;

namespace Altinn.Authorization.Integration.Tests.Register;

public class RoleStreamEndpointTest : IClassFixture<PlatformFixture>
{
    public RoleStreamEndpointTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnRegisterOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("Register");
        Register = fixture.GetService<IAltinnRegister>();
    }

    private IAltinnRegister Register { get; }

    [Theory]
    [InlineData(5)]
    public async Task TestStreamRoles(int iterations)
    {
        await foreach (var role in await Register.StreamRoles([], null, TestContext.Current.CancellationToken))
        {
            if (iterations-- <= 0)
            {
                break;
            }

            Assert.True(role.IsSuccessful);
        }
    }

    [Fact]
    public async Task TestStreamRolesWithNextPage()
    {
        var firstPage = await GetPage(null, TestContext.Current.CancellationToken);
        Assert.NotNull(firstPage);
        var secondPage = await GetPage(firstPage?.Content?.Links?.Next, TestContext.Current.CancellationToken);
        Assert.NotEqual(firstPage?.Content?.Links?.Next, secondPage?.Content?.Links?.Next);
    }

    private async Task<PlatformResponse<PageStream<RoleModel>>> GetPage(string nextPage = null, CancellationToken cancellationToken = default)
    {
        await foreach (var role in await Register.StreamRoles([], nextPage, cancellationToken))
        {
            return role;
        }

        return null;
    }
}
