using Altinn.ResourceRegistry.Core.Configuration;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.ScopePrefixes;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Altinn.ResourceRegistry.Tests.ScopePrefixes;

public class ScopePrefixValidatorTests
{
    private const string Digdir = "991825827";
    private const string Skatteetaten = "974761076";

    private static readonly TimeSpan StaleAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan ExpiredAge = TimeSpan.FromHours(2);

    private static readonly MaskinportenScopePrefix[] DefaultPrefixes =
    [
        new() { Prefix = "altinn", OwnerOrgNo = Digdir, Active = true },
        new() { Prefix = "digdir", OwnerOrgNo = Digdir, Active = true },
        new() { Prefix = "difi", OwnerOrgNo = Digdir, Active = false },
        new() { Prefix = "skd", OwnerOrgNo = Skatteetaten, Active = true },
        new() { Prefix = null, OwnerOrgNo = Skatteetaten, Active = true },
        new() { Prefix = "orphan", OwnerOrgNo = null, Active = true },
    ];

    private readonly SamarbeidsportalenClientMock _client = new();
    private readonly AdvanceableTimeProvider _timeProvider = new();
    private readonly ScopePrefixValidator _target;

    public ScopePrefixValidatorTests()
    {
        _client.Respond(DefaultPrefixes);

        var services = new ServiceCollection();
        services.AddSingleton<ISamarbeidsportalenClient>(_client);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var settings = Options.Create(new SamarbeidsportalenSettings
        {
            PrefixEndpoint = "https://example.test/api/v1/prefix/all",
            PrefixCacheStaleAge = StaleAge,
            PrefixCacheExpiredAge = ExpiredAge,
        });

        _target = new ScopePrefixValidator(NullLogger<ScopePrefixValidator>.Instance, scopeFactory, _timeProvider, settings);
    }

    [Fact]
    public async Task OwnedPrefixes_AreAccepted()
    {
        var result = await _target.GetUnownedScopes(Digdir, ["altinn:resourceregistry/resource.write", "digdir:test"]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task PrefixOwnedByAnotherOrganization_IsRejected()
    {
        var result = await _target.GetUnownedScopes(Skatteetaten, ["skd:test", "altinn:test"]);

        Assert.Equal(["altinn:test"], result);
    }

    [Fact]
    public async Task InactivePrefix_IsStillOwned()
    {
        // a deactivated prefix must not stop its owner from updating resources that use it
        var result = await _target.GetUnownedScopes(Digdir, ["difi:test"]);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("000000000")]
    [InlineData(null)]
    public async Task UnknownOrganization_OwnsNoPrefixes(string? organizationNumber)
    {
        var result = await _target.GetUnownedScopes(organizationNumber, ["altinn:test", "skd:test"]);

        Assert.Equal(["altinn:test", "skd:test"], result);
    }

    [Theory]
    [InlineData("skdx:test")] // longer prefix that starts with an owned one
    [InlineData("skd")] // the prefix alone, with no scope name
    [InlineData("SKD:test")] // prefixes are compared case-sensitively
    [InlineData("test:skd:test")]
    public async Task ScopeMustStartWithPrefixAndColon(string scope)
    {
        var result = await _target.GetUnownedScopes(Skatteetaten, [scope]);

        Assert.Equal([scope], result);
    }

    [Fact]
    public async Task FreshList_IsFetchedOnce()
    {
        await _target.GetUnownedScopes(Digdir, ["altinn:test"]);
        _timeProvider.Advance(StaleAge - TimeSpan.FromMinutes(1));
        await _target.GetUnownedScopes(Skatteetaten, ["skd:test"]);

        Assert.Equal(1, _client.CallCount);
    }

    [Fact]
    public async Task ConcurrentFirstCalls_ShareOneFetch()
    {
        var gate = new TaskCompletionSource();
        _client.Respond(DefaultPrefixes, gate.Task);

        var calls = Enumerable.Range(0, 10)
            .Select(_ => _target.GetUnownedScopes(Digdir, ["altinn:test"]).AsTask())
            .ToList();

        gate.SetResult();
        await Task.WhenAll(calls);

        Assert.Equal(1, _client.CallCount);
    }

    [Fact]
    public async Task StaleList_IsUsedWhileRefreshing()
    {
        await _target.GetUnownedScopes(Digdir, ["altinn:test"]);

        // the refresh removes the altinn prefix, but is held back until we release it
        var gate = new TaskCompletionSource();
        _client.Respond([new() { Prefix = "digdir", OwnerOrgNo = Digdir, Active = true }], gate.Task);
        _timeProvider.Advance(StaleAge + TimeSpan.FromMinutes(1));

        var stale = await _target.GetUnownedScopes(Digdir, ["altinn:test"]);
        Assert.Empty(stale);

        gate.SetResult();
        await _client.WaitForCallCount(2);
        await WaitUntil(async () => (await _target.GetUnownedScopes(Digdir, ["altinn:test"])).Count == 1);
    }

    [Fact]
    public async Task StaleList_IsUsed_WhenRefreshFails()
    {
        await _target.GetUnownedScopes(Digdir, ["altinn:test"]);

        _client.Fail();
        _timeProvider.Advance(StaleAge + TimeSpan.FromMinutes(1));

        var result = await _target.GetUnownedScopes(Digdir, ["altinn:test", "skd:test"]);

        Assert.Equal(["skd:test"], result);
    }

    [Fact]
    public async Task NoList_AndFetchFails_Throws()
    {
        _client.Fail();

        var ex = await Assert.ThrowsAsync<ScopePrefixDataUnavailableException>(
            () => _target.GetUnownedScopes(Digdir, ["altinn:test"]).AsTask());

        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task ExpiredList_AndFetchFails_Throws()
    {
        await _target.GetUnownedScopes(Digdir, ["altinn:test"]);

        _client.Fail();
        _timeProvider.Advance(ExpiredAge + TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<ScopePrefixDataUnavailableException>(
            () => _target.GetUnownedScopes(Digdir, ["altinn:test"]).AsTask());
    }

    [Fact]
    public async Task ExpiredList_IsReplaced()
    {
        await _target.GetUnownedScopes(Digdir, ["altinn:test"]);

        _client.Respond([new() { Prefix = "digdir", OwnerOrgNo = Digdir, Active = true }]);
        _timeProvider.Advance(ExpiredAge + TimeSpan.FromMinutes(1));

        var result = await _target.GetUnownedScopes(Digdir, ["altinn:test", "digdir:test"]);

        Assert.Equal(["altinn:test"], result);
        Assert.Equal(2, _client.CallCount);
    }

    [Fact]
    public async Task ListWithoutPrefixes_IsTreatedAsFailure()
    {
        _client.Respond([]);

        await Assert.ThrowsAsync<ScopePrefixDataUnavailableException>(
            () => _target.GetUnownedScopes(Digdir, ["altinn:test"]).AsTask());
    }

    [Fact]
    public async Task FailedFetch_IsRetriedOnNextCall()
    {
        _client.Fail();
        await Assert.ThrowsAsync<ScopePrefixDataUnavailableException>(
            () => _target.GetUnownedScopes(Digdir, ["altinn:test"]).AsTask());

        _client.Respond(DefaultPrefixes);
        var result = await _target.GetUnownedScopes(Digdir, ["altinn:test"]);

        Assert.Empty(result);
        Assert.Equal(2, _client.CallCount);
    }

    private static async Task WaitUntil(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!await condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
