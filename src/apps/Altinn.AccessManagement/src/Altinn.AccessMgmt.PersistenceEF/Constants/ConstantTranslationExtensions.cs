using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// In-memory translation of constant definition fields from their EN/NN entry lists — the
/// same content StaticDataIngest writes to the translation table, so surfaces serving
/// constants directly stay consistent with database-translated surfaces without a lookup.
/// </summary>
public static class ConstantTranslationExtensions
{
    /// <summary>
    /// Returns the field's translation for the three-letter language code used by the
    /// translation middleware ("eng", "nno"), or <paramref name="fallback"/> for bokmål,
    /// unknown codes and untranslated fields.
    /// </summary>
    public static string TranslateField<T>(this ConstantDefinition<T> definition, string languageCode, string field, string fallback)
        where T : class, IEntityId
    {
        var translations = languageCode switch
        {
            "eng" => definition.EN,
            "nno" => definition.NN,
            _ => null,
        };

        return translations?.Translations.GetValueOrDefault(field) ?? fallback;
    }
}
