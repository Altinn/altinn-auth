using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Utils;

/// <inheritdoc />
public class TranslationService : ITranslationService
{
    public TranslationService(AppDbContext dbContext)
    {
        Db = dbContext;
    }

    private AppDbContext Db { get; }

    /// <inheritdoc />
    public async ValueTask<T> TranslateAsync<T>(T source, string languageCode)
    {
        var type = typeof(T);
        var typeName = type.Name;

        var idProp = type.GetProperty("Id");
        if (idProp == null)
        {
            return source;
        }

        var id = idProp.GetValue(source);
        if (id is not Guid entityId)
        {
            return source;
        }

        //// Add support for history dbcontext

        var transMap = await Db.TranslationEntries
            .Where(t => t.Type == typeName &&
                        t.Id == entityId &&
                        t.LanguageCode == languageCode)
            .ToDictionaryAsync(t => t.FieldName, t => t.Value);

        foreach (var prop in type.GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            if (transMap.TryGetValue(prop.Name, out var val) && val is not null)
            {
                prop.SetValue(source, val);
            }
        }

        return source;
    }

    /// <inheritdoc />
    public T Translate<T>(T source, string languageCode)
    {
        var type = typeof(T);
        var typeName = type.Name;

        var idProp = type.GetProperty("Id");
        if (idProp == null)
        {
            return source;
        }

        var id = idProp.GetValue(source);
        if (id is not Guid entityId)
        {
            return source;
        }

        //// Add support for history dbcontext

        var transMap = Db.TranslationEntries
            .Where(t => t.Type == typeName &&
                        t.Id == entityId &&
                        t.LanguageCode == languageCode)
            .ToDictionary(t => t.FieldName, t => t.Value);

        foreach (var prop in type.GetProperties().Where(p => p.PropertyType == typeof(string)))
        {
            if (transMap.TryGetValue(prop.Name, out var val) && val is not null)
            {
                prop.SetValue(source, val);
            }
        }

        return source;
    }

    public async Task UpsertTranslationAsync(TranslationEntry translationEntry, CancellationToken cancellationToken = default)
    {
        var entry = await Db.TranslationEntries.SingleOrDefaultAsync(t => t.Id == translationEntry.Id && t.Type == translationEntry.Type && t.LanguageCode == translationEntry.LanguageCode && t.FieldName == translationEntry.FieldName, cancellationToken);

        if (entry == null)
        {
            Db.Add(translationEntry);
        }
        else
        {
            entry.Value = translationEntry.Value;
            Db.Update(entry);
        }

        Db.SaveChanges();
    }
}

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
    /// <returns>A <see cref="ValueTask{T}"/> representing the asynchronous operation. The result contains the translated object
    /// of type <typeparamref name="T"/>.</returns>
    ValueTask<T> TranslateAsync<T>(T source, string languageCode);

    /// <summary>
    /// Translates the specified object to the target language synchronously.
    /// </summary>
    /// <typeparam name="T">The type of the object to be translated. The type must support translation or serialization.</typeparam>
    /// <param name="source">The object to be translated. Cannot be <see langword="null"/>.</param>
    /// <param name="languageCode">The language code representing the target language for translation. Must be a valid ISO 639-1 code.</param>
    /// <returns>A <see name="T"/> representing the operation. The result contains the translated object
    /// of type <typeparamref name="T"/>.</returns>
    T Translate<T>(T source, string languageCode);

    Task UpsertTranslationAsync(TranslationEntry translationEntry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Translation entry
/// </summary>
public class TranslationEntry
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Language
    /// </summary>
    public string LanguageCode { get; set; } = default!;

    /// <summary>
    /// Field
    /// </summary>
    public string FieldName { get; set; } = default!;

    /// <summary>
    /// Translated value
    /// </summary>
    public string? Value { get; set; }

    public static List<TranslationEntry> Create(params List<TranslationEntry>[] translations)
    {
        var result = new List<TranslationEntry>();

        foreach (var translation in translations)
        {
            result.AddRange(translation);
        }

        return result;
    }
}

/// <summary>
/// Audit extension of Package
/// </summary>
public class AuditTranslationEntry : TranslationEntry, IAudit
{
    /// <inheritdoc />
    public DateTimeOffset Audit_ValidFrom { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? Audit_ValidTo { get; set; }

    /// <inheritdoc />
    public Guid? Audit_ChangedBy { get; set; }

    /// <inheritdoc />
    public Guid? Audit_ChangedBySystem { get; set; }

    /// <inheritdoc />
    public string Audit_ChangeOperation { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBy { get; set; }

    /// <inheritdoc />
    public Guid? Audit_DeletedBySystem { get; set; }

    /// <inheritdoc />
    public string Audit_DeleteOperation { get; set; }
}

/// <summary>
/// Translation entry
/// </summary>
public class TranslationEntryList
{
    public static TranslationEntryList Create(params KeyValuePair<string, string>[] keyValuePairs)
    {
        return new TranslationEntryList()
        {
            Translations = keyValuePairs.ToDictionary(),
        };
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Language
    /// </summary>
    public string LanguageCode { get; set; } = default!;

    /// <summary>
    /// Fileds and Values
    /// </summary>
    public Dictionary<string, string> Translations { get; set; } = [];

    public List<TranslationEntry> SingleEntries()
    {
        var result = new List<TranslationEntry>();

        foreach (var field in Translations)
        {
            result.Add(new TranslationEntry() { Id = this.Id, Type = this.Type, LanguageCode = this.LanguageCode, FieldName = field.Key, Value = field.Value });
        }

        return result;
    }

    public static implicit operator List<TranslationEntry>(TranslationEntryList def)
        => def.SingleEntries();
}
