using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.AccessMgmt.PersistenceEF.Utils;

// See: overhaul part-2 step 14
namespace Altinn.AccessMgmt.Core.Tests.Extensions;

/// <summary>
/// Pure-unit tests for <see cref="TranslationExtensions"/>. Pins the
/// null/default short-circuit behavior on every overload and the
/// service-delegation contract for non-null inputs.
/// </summary>
public class TranslationExtensionsTest
{
    private sealed record Dto(string Name);

    private sealed class RecordingTranslationService : ITranslationService
    {
        public int TranslateAsyncCalls { get; private set; }

        public int TranslateCollectionAsyncCalls { get; private set; }

        public List<string> LanguageCodes { get; } = new();

        public List<bool> AllowPartialFlags { get; } = new();

        public ValueTask<T> TranslateAsync<T>(T source, string languageCode, bool allowPartial = true)
        {
            TranslateAsyncCalls++;
            LanguageCodes.Add(languageCode);
            AllowPartialFlags.Add(allowPartial);
            return ValueTask.FromResult(source);
        }

        public T Translate<T>(T source, string languageCode, bool allowPartial = true) => source;

        public ValueTask<(bool Success, T Result)> TryTranslateAsync<T>(T source, string languageCode)
            => ValueTask.FromResult((true, source));

        public ValueTask<IEnumerable<T>> TranslateCollectionAsync<T>(IEnumerable<T> sources, string languageCode, bool allowPartial = true)
        {
            TranslateCollectionAsyncCalls++;
            LanguageCodes.Add(languageCode);
            AllowPartialFlags.Add(allowPartial);
            return ValueTask.FromResult(sources);
        }

        public Task UpsertTranslationAsync(TranslationEntry translationEntry, Guid changedBy, Guid changedBySystem, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    // ── TranslateAsync(TDto, ...) ─────────────────────────────────────────────
    [Fact]
    public async Task TranslateAsync_NullDto_ShortCircuits_NoServiceCall()
    {
        var service = new RecordingTranslationService();
        Dto? input = null;

        var result = await TranslationExtensions.TranslateAsync(input, service, "nob");

        Assert.Null(result);
        Assert.Equal(0, service.TranslateAsyncCalls);
    }

    [Fact]
    public async Task TranslateAsync_NonNullDto_CallsServiceWithLanguageAndAllowPartial()
    {
        var service = new RecordingTranslationService();
        var dto = new Dto("Hello");

        var result = await TranslationExtensions.TranslateAsync(dto, service, "eng", allowPartial: false);

        Assert.Equal(dto, result);
        Assert.Equal(1, service.TranslateAsyncCalls);
        Assert.Equal("eng", service.LanguageCodes[0]);
        Assert.False(service.AllowPartialFlags[0]);
    }

    // ── TranslateAsync(IEnumerable<TDto>, ...) ────────────────────────────────
    [Fact]
    public async Task TranslateAsync_NullCollection_ShortCircuits_NoServiceCall()
    {
        var service = new RecordingTranslationService();
        IEnumerable<Dto>? input = null;

        var result = await TranslationExtensions.TranslateAsync(input, service, "nob");

        Assert.Null(result);
        Assert.Equal(0, service.TranslateCollectionAsyncCalls);
    }

    [Fact]
    public async Task TranslateAsync_EmptyCollection_DelegatesToServiceCollectionMethod()
    {
        // Empty (but not null) collection is not short-circuited — the service
        // gets the chance to translate (e.g. validate the language code).
        var service = new RecordingTranslationService();

        var result = await TranslationExtensions.TranslateAsync(Enumerable.Empty<Dto>(), service, "nob");

        Assert.Empty(result);
        Assert.Equal(1, service.TranslateCollectionAsyncCalls);
    }

    [Fact]
    public async Task TranslateAsync_PopulatedCollection_DelegatesToServiceCollectionMethod()
    {
        var service = new RecordingTranslationService();
        var input = new[] { new Dto("a"), new Dto("b") };

        var result = await TranslationExtensions.TranslateAsync((IEnumerable<Dto>)input, service, "nob");

        Assert.Equal(input, result);
        Assert.Equal(1, service.TranslateCollectionAsyncCalls);
        Assert.Equal(0, service.TranslateAsyncCalls); // collection path doesn't call the singular method
    }

    // ── TranslateAsync(Task<TDto>, ...) ───────────────────────────────────────
    [Fact]
    public async Task TranslateAsync_TaskWithNullResult_AwaitsThenShortCircuits()
    {
        var service = new RecordingTranslationService();
        var task = Task.FromResult<Dto?>(null);

        var result = await TranslationExtensions.TranslateAsync(task, service, "nob");

        Assert.Null(result);
        Assert.Equal(0, service.TranslateAsyncCalls);
    }

    [Fact]
    public async Task TranslateAsync_TaskWithNonNullResult_AwaitsThenTranslates()
    {
        var service = new RecordingTranslationService();
        var dto = new Dto("Hello");
        var task = Task.FromResult<Dto?>(dto);

        var result = await TranslationExtensions.TranslateAsync(task, service, "eng");

        Assert.Equal(dto, result);
        Assert.Equal(1, service.TranslateAsyncCalls);
    }

    // ── MapAndTranslateAsync(TSource, ...) ────────────────────────────────────
    [Fact]
    public async Task MapAndTranslateAsync_NullSource_ReturnsDefault_MapperNotInvoked()
    {
        var service = new RecordingTranslationService();
        Dto? input = null;
        bool mapperInvoked = false;

        var result = await TranslationExtensions.MapAndTranslateAsync<Dto, string>(
            input,
            d =>
            {
                mapperInvoked = true;
                return d.Name;
            },
            service,
            "nob");

        Assert.Null(result);
        Assert.False(mapperInvoked);
        Assert.Equal(0, service.TranslateAsyncCalls);
    }

    [Fact]
    public async Task MapAndTranslateAsync_NonNullSource_InvokesMapperThenTranslates()
    {
        var service = new RecordingTranslationService();
        var input = new Dto("Hello");
        int mapperCalls = 0;

        var result = await TranslationExtensions.MapAndTranslateAsync<Dto, string>(
            input,
            d =>
            {
                mapperCalls++;
                return d.Name;
            },
            service,
            "nob");

        Assert.Equal("Hello", result);
        Assert.Equal(1, mapperCalls);
        Assert.Equal(1, service.TranslateAsyncCalls);
    }

    // ── MapAndTranslateAsync(IEnumerable<TSource>, ...) ───────────────────────
    [Fact]
    public async Task MapAndTranslateAsync_NullSourceCollection_ReturnsEmpty_MapperNotInvoked()
    {
        var service = new RecordingTranslationService();
        IEnumerable<Dto>? input = null;
        bool mapperInvoked = false;

        var result = await TranslationExtensions.MapAndTranslateAsync<Dto, string>(
            input,
            d =>
            {
                mapperInvoked = true;
                return d.Name;
            },
            service,
            "nob");

        Assert.Empty(result);
        Assert.False(mapperInvoked);
        Assert.Equal(0, service.TranslateCollectionAsyncCalls);
    }

    [Fact]
    public async Task MapAndTranslateAsync_PopulatedSourceCollection_MapsAndDelegatesToCollectionMethod()
    {
        var service = new RecordingTranslationService();
        var sources = new[] { new Dto("a"), new Dto("b"), new Dto("c") };
        var mapped = new List<string>();

        var result = await TranslationExtensions.MapAndTranslateAsync<Dto, string>(
            sources,
            d =>
            {
                mapped.Add(d.Name);
                return d.Name.ToUpper();
            },
            service,
            "nob");

        Assert.Equal(["A", "B", "C"], result);
        Assert.Equal(["a", "b", "c"], mapped);
        Assert.Equal(1, service.TranslateCollectionAsyncCalls);
    }
}
