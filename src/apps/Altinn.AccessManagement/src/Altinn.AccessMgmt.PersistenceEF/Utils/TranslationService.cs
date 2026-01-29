using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<TranslationService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TranslationService(AppDbContext dbContext, IMemoryCache memoryCache, ILogger<TranslationService> logger)
    {
        _db = dbContext;
        _cache = memoryCache;
        _logger = logger;
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
            _logger.LogTrace("Type {TypeName} does not have an Id property, skipping translation", typeName);
            return (false, source);
        }

        var id = idProp.GetValue(source);
        if (id is not Guid entityId)
        {
            _logger.LogTrace("Type {TypeName} Id property is not a Guid, skipping translation", typeName);
            return (false, source);
        }

        // Ensure we have a valid language code, default to Norwegian Bokmål if empty
        var effectiveLanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "nob" : languageCode;

        // Norwegian Bokmål is the base language - entities are already in Bokmål
        // Constants only provide translations for English (eng) and Norwegian Nynorsk (nno)
        if (effectiveLanguageCode == "nob")
        {
            _logger.LogTrace("Language code is 'nob' (base language), returning original {TypeName} with ID {EntityId}", 
                typeName, entityId);
            return (true, source); // Return original, it's already in Norwegian Bokmål
        }

        _logger.LogDebug("Attempting to translate {TypeName} with ID {EntityId} to language {LanguageCode}", 
            typeName, entityId, effectiveLanguageCode);

        // Try to get translations from Constants first (for eng and nno only)
        var constantTranslations = TryGetConstantTranslations(typeName, entityId, effectiveLanguageCode);

        Dictionary<string, string> transMap;

        if (constantTranslations != null && constantTranslations.Any())
        {
            _logger.LogDebug("Found {Count} constant translations for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                constantTranslations.Count, typeName, entityId, effectiveLanguageCode);
            transMap = constantTranslations;
        }
        else
        {
            // Fall back to database with caching
            var cacheKey = $"translation_{typeName}_{entityId}_{effectiveLanguageCode}";
            
            _logger.LogTrace("Checking cache for translation key: {CacheKey}", cacheKey);
            
            transMap = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                
                _logger.LogDebug("Cache miss for {TypeName} with ID {EntityId}, querying database for language {LanguageCode}", 
                    typeName, entityId, effectiveLanguageCode);
                
                var dbTranslations = await _db.TranslationEntries
                    .Where(t => t.Type == typeName &&
                                t.Id == entityId &&
                                t.LanguageCode == effectiveLanguageCode)
                    .ToDictionaryAsync(t => t.FieldName, t => t.Value ?? string.Empty);
                
                if (dbTranslations.Any())
                {
                    _logger.LogDebug("Found {Count} database translations for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                        dbTranslations.Count, typeName, entityId, effectiveLanguageCode);
                }
                else
                {
                    _logger.LogInformation("No database translations found for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                        typeName, entityId, effectiveLanguageCode);
                }
                
                return dbTranslations;
            });
        }

        if (transMap == null || !transMap.Any())
        {
            _logger.LogWarning("No translations available for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                typeName, entityId, effectiveLanguageCode);
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
                _logger.LogTrace("Translated field {FieldName} for {TypeName} with ID {EntityId}", 
                    prop.Name, typeName, entityId);
            }
        }

        if (translatedFields > 0)
        {
            _logger.LogDebug("Successfully translated {TranslatedFields} fields for {TypeName} with ID {EntityId} to language {LanguageCode}", 
                translatedFields, typeName, entityId, effectiveLanguageCode);
        }
        else
        {
            _logger.LogWarning("No fields were translated for {TypeName} with ID {EntityId} in language {LanguageCode} (found {TranslationCount} translations but none matched writable string properties)", 
                typeName, entityId, effectiveLanguageCode, transMap.Count);
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
    public async Task UpsertTranslationAsync(TranslationEntry translationEntry, Guid changedBy, Guid changedBySystem, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Upserting translation for {Type} with ID {Id}, field {FieldName} in language {LanguageCode}", 
            translationEntry.Type, translationEntry.Id, translationEntry.FieldName, translationEntry.LanguageCode);
        
        var entry = await _db.TranslationEntries.SingleOrDefaultAsync(
            t => t.Id == translationEntry.Id && 
                 t.Type == translationEntry.Type && 
                 t.LanguageCode == translationEntry.LanguageCode && 
                 t.FieldName == translationEntry.FieldName, 
            cancellationToken);

        if (entry == null)
        {
            _logger.LogInformation("Creating new translation entry for {Type} with ID {Id}, field {FieldName} in language {LanguageCode}", 
                translationEntry.Type, translationEntry.Id, translationEntry.FieldName, translationEntry.LanguageCode);
            _db.Add(translationEntry);
        }
        else
        {
            _logger.LogDebug("Updating existing translation entry for {Type} with ID {Id}, field {FieldName} in language {LanguageCode}", 
                translationEntry.Type, translationEntry.Id, translationEntry.FieldName, translationEntry.LanguageCode);
            entry.Value = translationEntry.Value;
            _db.Update(entry);
        }

        // Translation entries are not audited entities, but AppDbContext.SaveChangesAsync requires audit values.
        // The caller specifies the system context for audit tracking.
        var systemAudit = new AuditValues(
            changedBy: changedBy,
            changedBySystem: changedBySystem
        );
        
        await _db.SaveChangesAsync(systemAudit, cancellationToken);

        // Invalidate cache
        var cacheKey = $"translation_{translationEntry.Type}_{translationEntry.Id}_{translationEntry.LanguageCode}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache for key: {CacheKey}", cacheKey);
    }

    /// <summary>
    /// Attempts to retrieve translations from Constants classes (RoleConstants, PackageConstants, etc.)
    /// </summary>
    private Dictionary<string, string>? TryGetConstantTranslations(string typeName, Guid entityId, string languageCode)
    {
        try
        {
            // Check if we have Constants for this type
            var constantsType = typeName switch
            {
                "Role" or "RoleDto" or "ExtRole" => typeof(RoleConstants),
                "Package" or "PackageDto" or "ExtPackage" => typeof(PackageConstants),
                "Area" or "AreaDto" => typeof(AreaConstants),
                "AreaGroup" or "AreaGroupDto" => typeof(AreaGroupConstants),
                "Provider" or "ProviderDto" => typeof(ProviderConstants),
                _ => null
            };

            if (constantsType == null)
            {
                _logger.LogTrace("No constant class mapped for type {TypeName}", typeName);
                return null;
            }

            _logger.LogTrace("Attempting to get constant translations from {ConstantsType} for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                constantsType.Name, typeName, entityId, languageCode);

            // Get all translations from the Constants class
            var allTranslationsMethod = constantsType.GetMethod("AllTranslations");
            if (allTranslationsMethod == null)
            {
                _logger.LogWarning("Constants class {ConstantsType} does not have AllTranslations method", constantsType.Name);
                return null;
            }

            var allTranslations = allTranslationsMethod.Invoke(null, null) as IEnumerable<TranslationEntry>;
            if (allTranslations == null)
            {
                _logger.LogWarning("AllTranslations method in {ConstantsType} returned null", constantsType.Name);
                return null;
            }

            // Filter to the specific entity and language
            var relevantTranslations = allTranslations
                .Where(t => t.Id == entityId && t.LanguageCode == languageCode)
                .ToDictionary(t => t.FieldName, t => t.Value ?? string.Empty);

            if (relevantTranslations.Any())
            {
                _logger.LogDebug("Found {Count} constant translations for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                    relevantTranslations.Count, typeName, entityId, languageCode);
                return relevantTranslations;
            }
            else
            {
                _logger.LogTrace("No constant translations found for {TypeName} with ID {EntityId} in language {LanguageCode}", 
                    typeName, entityId, languageCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            // If anything fails, fall back to database
            _logger.LogError(ex, "Error retrieving constant translations for {TypeName} with ID {EntityId} in language {LanguageCode}, falling back to database", 
                typeName, entityId, languageCode);
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
    /// Fields and Values
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
