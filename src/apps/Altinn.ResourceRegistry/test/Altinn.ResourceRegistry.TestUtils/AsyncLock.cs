namespace Altinn.ResourceRegistry.TestUtils;

internal sealed class AsyncLock()
    : AsyncConcurrencyLimiter(1)
{
}
