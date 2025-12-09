using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessManagement.Api.Metadata.Translation;

/// <summary>
/// Extension methods for translating DTOs and collections
/// </summary>
public static class TranslationExtensions
{
    /// <summary>
    /// Translates a DTO to the specified language asynchronously
    /// </summary>
    /// <typeparam name="TDto">The DTO type to translate</typeparam>
    /// <param name="dto">The DTO instance to translate</param>
    /// <param name="translationService">The translation service</param>
    /// <param name="languageCode">The target language code</param>
    /// <param name="allowPartial">Whether to allow partial translation</param>
    /// <returns>The translated DTO</returns>
    public static async ValueTask<TDto> TranslateAsync<TDto>(
        this TDto dto,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (dto == null)
        {
            return dto;
        }

        return await translationService.TranslateAsync(dto, languageCode, allowPartial);
    }

    /// <summary>
    /// Translates a collection of DTOs to the specified language asynchronously
    /// </summary>
    /// <typeparam name="TDto">The DTO type to translate</typeparam>
    /// <param name="dtos">The collection of DTOs to translate</param>
    /// <param name="translationService">The translation service</param>
    /// <param name="languageCode">The target language code</param>
    /// <param name="allowPartial">Whether to allow partial translation</param>
    /// <returns>The translated collection</returns>
    public static async ValueTask<IEnumerable<TDto>> TranslateAsync<TDto>(
        this IEnumerable<TDto> dtos,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (dtos == null)
        {
            return dtos;
        }

        return await translationService.TranslateCollectionAsync(dtos, languageCode, allowPartial);
    }

    /// <summary>
    /// Maps and translates a single entity to a DTO
    /// </summary>
    /// <typeparam name="TSource">The source entity type</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <param name="source">The source entity</param>
    /// <param name="mapper">Function to map from source to DTO</param>
    /// <param name="translationService">The translation service</param>
    /// <param name="languageCode">The target language code</param>
    /// <param name="allowPartial">Whether to allow partial translation</param>
    /// <returns>The mapped and translated DTO</returns>
    public static async ValueTask<TDto> MapAndTranslateAsync<TSource, TDto>(
        this TSource source,
        Func<TSource, TDto> mapper,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (source == null)
        {
            return default;
        }

        var dto = mapper(source);
        return await translationService.TranslateAsync(dto, languageCode, allowPartial);
    }

    /// <summary>
    /// Maps and translates a collection of entities to DTOs
    /// </summary>
    /// <typeparam name="TSource">The source entity type</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <param name="sources">The collection of source entities</param>
    /// <param name="mapper">Function to map from source to DTO</param>
    /// <param name="translationService">The translation service</param>
    /// <param name="languageCode">The target language code</param>
    /// <param name="allowPartial">Whether to allow partial translation</param>
    /// <returns>The mapped and translated collection</returns>
    public static async ValueTask<IEnumerable<TDto>> MapAndTranslateAsync<TSource, TDto>(
        this IEnumerable<TSource> sources,
        Func<TSource, TDto> mapper,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        if (sources == null)
        {
            return Enumerable.Empty<TDto>();
        }

        var dtos = sources.Select(mapper);
        return await translationService.TranslateCollectionAsync(dtos, languageCode, allowPartial);
    }

    /// <summary>
    /// Translates a Task result
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <param name="dtoTask">The task returning the DTO</param>
    /// <param name="translationService">The translation service</param>
    /// <param name="languageCode">The target language code</param>
    /// <param name="allowPartial">Whether to allow partial translation</param>
    /// <returns>The translated DTO</returns>
    public static async ValueTask<TDto> TranslateAsync<TDto>(
        this Task<TDto> dtoTask,
        ITranslationService translationService,
        string languageCode,
        bool allowPartial = true)
    {
        var dto = await dtoTask;
        if (dto == null)
        {
            return dto;
        }

        return await translationService.TranslateAsync(dto, languageCode, allowPartial);
    }
}
