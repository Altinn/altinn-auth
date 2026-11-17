using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Extensions;

/// <summary>
/// Pure-unit tests for <see cref="DeepTranslationExtensions.TranslateDistinctInPlaceAsync"/>:
/// each distinct resource is looked up once, every instance sharing its id gets the result,
/// fields the mapping left empty stay empty, and bokmål or no language skips the lookup.
/// </summary>
[UnitTest]
public class TranslateDistinctInPlaceTest
{
    private sealed class EnglishResourceTranslator : ITranslationService
    {
        public int ResourceLookups { get; private set; }

        public ValueTask<T> TranslateAsync<T>(T source, string languageCode, bool allowPartial = true)
        {
            if (source is ResourceDto resource)
            {
                ResourceLookups++;
                resource.Name = $"{resource.RefId} (en)";
                resource.Description = "English description";
                resource.RightDescription = "English right description";
            }

            return ValueTask.FromResult(source);
        }

        public T Translate<T>(T source, string languageCode, bool allowPartial = true) => source;

        public ValueTask<(bool Success, T Result)> TryTranslateAsync<T>(T source, string languageCode)
            => ValueTask.FromResult((true, source));

        public ValueTask<IEnumerable<T>> TranslateCollectionAsync<T>(IEnumerable<T> sources, string languageCode, bool allowPartial = true)
            => ValueTask.FromResult(sources);

        public Task UpsertTranslationAsync(TranslationEntry e, Guid c, Guid s, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static ResourceDto Compact(Guid id, string refId) => new() { Id = id, RefId = refId, Name = refId };

    [Fact]
    public async Task RepeatedResource_IsLookedUpOnce_AndEveryInstanceIsTranslated()
    {
        var id = Guid.CreateVersion7();
        var first = Compact(id, "res-a");
        var second = Compact(id, "res-a");
        var other = Compact(Guid.CreateVersion7(), "res-b");
        var svc = new EnglishResourceTranslator();

        await new[] { first, second, other }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal(2, svc.ResourceLookups);
        Assert.Equal("res-a (en)", first.Name);
        Assert.Equal("res-a (en)", second.Name);
        Assert.Equal("res-b (en)", other.Name);
    }

    [Fact]
    public async Task CompactResource_KeepsItsShape()
    {
        var resource = Compact(Guid.CreateVersion7(), "res-a");
        var svc = new EnglishResourceTranslator();

        await new[] { resource }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal("res-a (en)", resource.Name);
        Assert.Null(resource.Description);
        Assert.Null(resource.RightDescription);
    }

    [Fact]
    public async Task FullResource_GetsEveryTextField()
    {
        var resource = new ResourceDto { Id = Guid.CreateVersion7(), RefId = "res-a", Name = "Navn", Description = "Beskrivelse", RightDescription = "Rettighet" };
        var svc = new EnglishResourceTranslator();

        await new[] { resource }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal("res-a (en)", resource.Name);
        Assert.Equal("English description", resource.Description);
        Assert.Equal("English right description", resource.RightDescription);
    }

    [Fact]
    public async Task CompactThenFull_EachKeepsItsOwnShape()
    {
        var id = Guid.CreateVersion7();
        var compact = Compact(id, "res-a");
        var full = new ResourceDto { Id = id, RefId = "res-a", Name = "Navn", Description = "Beskrivelse", RightDescription = "Rettighet" };
        var svc = new EnglishResourceTranslator();

        await new[] { compact, full }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal(1, svc.ResourceLookups);
        Assert.Equal("res-a (en)", compact.Name);
        Assert.Null(compact.Description);
        Assert.Null(compact.RightDescription);
        Assert.Equal("res-a (en)", full.Name);
        Assert.Equal("English description", full.Description);
        Assert.Equal("English right description", full.RightDescription);
    }

    [Fact]
    public async Task FullThenCompact_EachKeepsItsOwnShape()
    {
        var id = Guid.CreateVersion7();
        var full = new ResourceDto
        {
            Id = id,
            RefId = "res-a",
            Name = "Navn",
            Description = "Beskrivelse",
            RightDescription = "Rettighet",
            Provider = new ProviderDto { Id = Guid.CreateVersion7(), Name = "Leverandør" },
            Type = new ResourceTypeDto { Id = Guid.CreateVersion7(), Name = "Type" },
        };
        var compact = Compact(id, "res-a");
        var svc = new EnglishResourceTranslator();

        await new[] { full, compact }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal(1, svc.ResourceLookups);
        Assert.Equal("English description", full.Description);
        Assert.NotNull(full.Provider);
        Assert.NotNull(full.Type);
        Assert.Equal("res-a (en)", compact.Name);
        Assert.Null(compact.Description);
        Assert.Null(compact.RightDescription);
        Assert.Null(compact.Provider);
        Assert.Null(compact.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nob")]
    public async Task BokmalOrNoLanguage_SkipsLookup(string? languageCode)
    {
        var resource = Compact(Guid.CreateVersion7(), "res-a");
        var svc = new EnglishResourceTranslator();

        await new[] { resource }.TranslateDistinctInPlaceAsync(svc, languageCode);

        Assert.Equal(0, svc.ResourceLookups);
        Assert.Equal("res-a", resource.Name);
    }

    [Fact]
    public async Task NullsInTheSequence_AreIgnored()
    {
        var resource = Compact(Guid.CreateVersion7(), "res-a");
        var svc = new EnglishResourceTranslator();

        await new ResourceDto?[] { null, resource }.TranslateDistinctInPlaceAsync(svc, "eng");

        Assert.Equal("res-a (en)", resource.Name);
    }
}
