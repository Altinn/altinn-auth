namespace Altinn.AccessMgmt.PersistenceEF.Utils;

/// <summary>
/// Translation service for EF model
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates the specified object to the target language asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object to be translated. The type must support translation or serialization.</typeparam>
    /// <param name="source">The object to be translated. Cannot be <see langword="null"/>.</param>
    /// <param name="languageCode">The language code representing the target language for translation. Must be a valid ISO 639-1 code.</param>
    /// <param name="allowPartial">If true, returns partial translation when some fields cannot be translated. If false, returns original object when any translation fails.</param>
    /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The result contains the translated object
    /// of type <typeparamref name="T"/>.</returns>
    ValueTask<T> TranslateAsync<T>(T source, string languageCode, bool allowPartial = true);

    /// <summary>
    /// Translates the specified object to the target language synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object to be translated. The type must support translation or serialization.</typeparam>
    /// <param name="source">The object to be translated. Cannot be <see langword="null"/>.</param>
    /// <param name="languageCode">The language code representing the target language for translation. Must be a valid ISO 639-1 code.</param>
    /// <param name="allowPartial">If true, returns partial translation when some fields cannot be translated. If false, returns original object when any translation fails.</param>
    /// <returns>A <see name="T"/> representing the operation. The result contains the translated object
    /// of type <typeparamref name="T"/>.</returns>
    T Translate<T>(T source, string languageCode, bool allowPartial = true);

    /// <summary>
    /// Attempts to translate the specified object to the target language asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object to be translated.</typeparam>
    /// <param name="source">The object to be translated.</param>
    /// <param name="languageCode">The target language code.</param>
    /// <returns>A tuple containing success status and the translated object (or original if translation failed).</returns>
    ValueTask<(bool Success, T Result)> TryTranslateAsync<T>(T source, string languageCode);

    /// <summary>
    /// Translates a collection of objects to the target language asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="sources">The collection of objects to translate.</param>
    /// <param name="languageCode">The target language code.</param>
    /// <param name="allowPartial">If true, returns partial translation when some fields cannot be translated.</param>
    /// <returns>The translated collection.</returns>
    ValueTask<IEnumerable<T>> TranslateCollectionAsync<T>(IEnumerable<T> sources, string languageCode, bool allowPartial = true);

    /// <summary>
    /// Inserts a new translation entry or updates an existing one in the database.
    /// </summary>
    /// <param name="translationEntry">The translation entry to insert or update.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertTranslationAsync(TranslationEntry translationEntry, CancellationToken cancellationToken = default);
}
