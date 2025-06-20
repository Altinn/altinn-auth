using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Register;

namespace Altinn.Authorization.Integration.Tests.Register;

public class PartiesStreamEndpointTest : IClassFixture<PlatformFixture>
{
    public PartiesStreamEndpointTest(PlatformFixture fixture)
    {
        fixture.SkipIfMissingConfiguration<AltinnRegisterOptions>();
        fixture.SkipIfMissingConfiguration<AltinnIntegrationOptions>();
        fixture.SkipIfDisabled("Register");
        Register = fixture.GetService<IAltinnRegister>();
    }

    private IAltinnRegister Register { get; }

    [Theory]
    [InlineData(5)]
    public async Task TestStreamParties(int iterations)
    {
        await foreach (var role in await Register.StreamParties([], null, TestContext.Current.CancellationToken))
        {
            if (iterations-- <= 0)
            {
                break;
            }

            Assert.True(role.IsSuccessful);
        }
    }

    [Fact]
    public async Task TestStreamPartiesWithNextPage()
    {
        var firstPage = await GetPage(null, TestContext.Current.CancellationToken);
        Assert.NotNull(firstPage);
        var secondPage = await GetPage(firstPage?.Content?.Links?.Next, TestContext.Current.CancellationToken);
        Assert.NotEqual(firstPage?.Content?.Links?.Next, secondPage?.Content?.Links?.Next);
    }

    [Theory]
    [InlineData(5)]
    public async Task TestStreamPartiesWithAllFieldsSelected(int iterations)
    {
        await foreach (var role in await Register.StreamParties(AltinnRegisterClient.AvailableFields, null, TestContext.Current.CancellationToken))
        {
            if (iterations-- <= 0)
            {
                break;
            }

            Assert.True(role.IsSuccessful);
        }
    }

    private async Task<PlatformResponse<PageStream<PartyModel>>> GetPage(string nextPage = null, CancellationToken cancellationToken = default)
    {
        await foreach (var role in await Register.StreamParties([], nextPage, cancellationToken))
        {
            return role;
        }

        return null;
    }
}
