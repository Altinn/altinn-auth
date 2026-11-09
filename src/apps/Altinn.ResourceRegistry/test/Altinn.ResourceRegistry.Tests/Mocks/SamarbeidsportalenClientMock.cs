using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Services;

namespace Altinn.ResourceRegistry.Tests.Mocks;

/// <summary>
/// Fake prefix API. Returns whatever was last set with <see cref="Respond"/> or <see cref="Fail"/>,
/// and counts the calls.
/// </summary>
public sealed class SamarbeidsportalenClientMock : ISamarbeidsportalenClient
{
    private Func<Task<IReadOnlyList<MaskinportenScopePrefix>>> _response = () => Task.FromResult<IReadOnlyList<MaskinportenScopePrefix>>([]);
    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    /// <summary>
    /// Returns <paramref name="prefixes"/>, after <paramref name="gate"/> completes if one is given.
    /// </summary>
    public void Respond(IReadOnlyList<MaskinportenScopePrefix> prefixes, Task? gate = null)
        => _response = async () =>
        {
            if (gate is not null)
            {
                await gate;
            }

            return prefixes;
        };

    /// <summary>
    /// Fails every call, as if the API were down.
    /// </summary>
    public void Fail()
        => _response = () => Task.FromException<IReadOnlyList<MaskinportenScopePrefix>>(new HttpRequestException("API is down"));

    public Task<IReadOnlyList<MaskinportenScopePrefix>> GetScopePrefixes(CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _callCount);
        return _response();
    }

    public async Task WaitForCallCount(int count)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (CallCount < count)
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
