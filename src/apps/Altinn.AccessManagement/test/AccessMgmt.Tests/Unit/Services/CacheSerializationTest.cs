using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Tests.Utils;
using Microsoft.Extensions.Caching.Hybrid;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// The types the consent, consent-template, org-code and translation caches hold survive the cache serializer.
/// <see cref="ContextRetrievalServiceCacheTest"/> covers the context-retrieval types.
/// </summary>
[UnitTest]
public class CacheSerializationTest
{
    private readonly HybridCache _cache = HybridCacheUtil.Create();

    [Fact]
    public async Task RemainingCachedTypes_RoundTripThroughTheCacheSerializer()
    {
        await AssertRoundTrip(ConsentPartyUrn.Parse("urn:altinn:party:uuid:00000000-0000-0000-0000-000000050001"));
        await AssertRoundTrip(new List<ConsentTemplate>
        {
            new() { Id = "samtykke", Version = 1, Title = "Samtykke", IsPoa = false, Texts = new ConsentTemplateTexts { Expiration = new Dictionary<string, string> { ["nb"] = "Gyldig til" } } },
        });
        await AssertRoundTrip(new Dictionary<string, string> { ["Name"] = "Navn", ["Description"] = "Beskrivelse" });
        await AssertRoundTrip("ttd");
    }

    private async Task AssertRoundTrip<T>(T value)
    {
        string key = $"roundtrip:{typeof(T).Name}";
        await _cache.SetAsync(key, value, cancellationToken: TestContext.Current.CancellationToken);
        T read = await _cache.GetOrCreateAsync<T>(key, _ => throw new InvalidOperationException("expected a cache hit"), cancellationToken: TestContext.Current.CancellationToken);

        JsonSerializer.Serialize(read).Should().Be(JsonSerializer.Serialize(value), $"{typeof(T).Name} must survive the cache serializer");
    }
}
