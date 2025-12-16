using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Altinn.AccessMgmt.PersistenceEF.Utils;

/// <summary>
/// Translation service implementation for entity framework models.
/// 
/// IMPORTANT: This service expects pre-normalized ISO 639-2 three-letter language codes:
/// - "eng" for English
/// - "nob" for Norwegian Bokmål (base language)
/// - "nno" for Norwegian Nynorsk
/// 
/// In HTTP request contexts, language code normalization is handled by the TranslationMiddleware,
/// which converts Accept-Language header values (e.g., "en-US", "nb-NO") to ISO 639-2 codes
/// before they reach this service.
/// 
/// For direct service calls (e.g., in unit tests), callers are responsible for providing
/// normalized language codes.
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TranslationService(AppDbContext dbContext, IMemoryCache memoryCache)
    {
        _db = dbContext;
        _cache = memoryCache;
    }

    /// <inheritdoc />
    public async ValueTask<T> TranslateAsync<T>(T source, string languageCode, bool allowPartial = true)
    {
        var (success, result) = await TryTranslateAsync(source, languageCode);
        
        if (!success && !allowPartial)
        {
            return source;
        }

        return result;
    }

    /// <inheritdoc />
    public T Translate<T>(T source, string languageCode, bool allowPartial = true)
    {
        return TranslateAsync(source, languageCode, allowPartial).AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask<(bool Success, T Result)> TryTranslateAsync<T>(T source, string languageCode)
    {
        if (source == null)
        {
            return (false, source);
        }

        var type = typeof(T);
        var typeName = type.Name;

        var idProp = type.GetProperty("Id");
        if (idProp == null)
        {
            return (false, source);
        }

        var id = idProp.GetValue(source);
        if (id is not Guid entityId)
        {
            return (false, source);
        }

        // Ensure we have a valid language code, default to Norwegian Bokmål if empty
        var effectiveLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "nob" : languageCode;

        // Norwegian Bokmål is the base language - entities are already in Bokmål
        // Constants only provide translations for English (eng) and Norwegian Nynorsk (nno)
        if (effectiveLanguageCode == "nob")
        {
            return (true, source); // Return original, it's already in Norwegian Bokmål
        }

        // Try to get translations from Constants first (for eng and nno only)
        var constantTranslations = TryGetConstantTranslations(typeName, entityId, effectiveLanguageCode);

        Dictionary<string, string> transMap;

        if (constantTranslations != null && constantTranslations.Any())
        {
            transMap = constantTranslations;
        }
        else
        {
            // Fall back to database with caching
            var cacheKey = $"translation_{typeName}_{entityId}_{effectiveLanguageCode}";
            
            transMap = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                
                return await _db.TranslationEntries
                    .Where(t => t.Type == typeName &&
                                t.Id == entityId &&
                                t.LanguageCode == effectiveLanguageCode)
                    .ToDictionaryAsync(t => t.FieldName, t => t.Value ?? string.Empty);
            });
        }

        if (transMap == null || !transMap.Any())
        {
            return (false, source);
        }

        // Create a new instance or clone to avoid modifying the original
        var translatedFields = 0;
        var stringProperties = type.GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite).ToList();

        foreach (var prop in stringProperties)
        {
            if (transMap.TryGetValue(prop.Name, out var val) && !string.IsNullOrEmpty(val))
            {
                prop.SetValue(source, val);
                translatedFields++;
            }
        }

        // Consider it a success if at least one field was translated
        return (translatedFields > 0, source);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<T>> TranslateCollectionAsync<T>(IEnumerable<T> sources, string languageCode, bool allowPartial = true)
    {
        var result = new List<T>();
        
        foreach (var source in sources)
        {
            var translated = await TranslateAsync(source, languageCode, allowPartial);
            result.Add(translated);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task UpsertTranslationAsync(TranslationEntry translationEntry, CancellationToken cancellationToken = default)
    {
        var entry = await _db.TranslationEntries.SingleOrDefaultAsync(
            t => t.Id == translationEntry.Id && 
                 t.Type == translationEntry.Type && 
                 t.LanguageCode == translationEntry.LanguageCode && 
                 t.FieldName == translationEntry.FieldName, 
            cancellationToken);

        if (entry == null)
        {
            _db.Add(translationEntry);
        }
        else
        {
            entry.Value = translationEntry.Value;
            _db.Update(entry);
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"translation_{translationEntry.Type}_{translationEntry.Id}_{translationEntry.LanguageCode}";
        _cache.Remove(cacheKey);
    }

    /// <summary>
    /// Attempts to retrieve translations from Constants classes (RoleConstants, PackageConstants, etc.)
    /// </summary>
    private static Dictionary<string, string>? TryGetConstantTranslations(string typeName, Guid entityId, string languageCode)
    {
        try
        {
            // Check if we have Constants for this type
            var constantsType = typeName switch
            {
                "Role" or "RoleDto" or "ExtRole" => typeof(RoleConstants),
                "Package" or "PackageDto" or "ExtPackage" => typeof(PackageConstants),
                _ => null
            };

            if (constantsType == null)
            {
                return null;
            }

            // Get all translations from the Constants class
            var allTranslationsMethod = constantsType.GetMethod("AllTranslations");
            if (allTranslationsMethod == null)
            {
                return null;
            }

            var allTranslations = allTranslationsMethod.Invoke(null, null) as IEnumerable<TranslationEntry>;
            if (allTranslations == null)
            {
                return null;
            }

            // Filter to the specific entity and language
            var relevantTranslations = allTranslations
                .Where(t => t.Id == entityId && t.LanguageCode == languageCode)
                .ToDictionary(t => t.FieldName, t => t.Value ?? string.Empty);

            return relevantTranslations.Any() ? relevantTranslations : null;
        }
        catch
        {
            // If anything fails, fall back to database
            return null;
        }
    }
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
