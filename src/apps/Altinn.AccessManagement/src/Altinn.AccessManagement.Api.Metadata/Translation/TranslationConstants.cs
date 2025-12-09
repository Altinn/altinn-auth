namespace Altinn.AccessManagement.Api.Metadata.Translation;

/// <summary>
/// Constants used for translation functionality in the API
/// </summary>
public static class TranslationConstants
{
    /// <summary>
    /// Custom header to control partial translation behavior.
    /// If set to "true" or "1", allows partial translations where some fields may not be translated.
    /// If set to "false" or "0", requires all fields to be translated or returns original.
    /// </summary>
    public const string PartialTranslationHeader = "X-Accept-Partial-Translation";

    /// <summary>
    /// Standard HTTP header for specifying accepted languages
    /// Format: Accept-Language: en-US,en;q=0.9,nb;q=0.8
    /// </summary>
    public const string AcceptLanguageHeader = "Accept-Language";

    /// <summary>
    /// Standard HTTP header for specifying content language
    /// Format: Content-Language: nb-NO
    /// </summary>
    public const string ContentLanguageHeader = "Content-Language";

    /// <summary>
    /// HttpContext.Items key for storing the resolved language code
    /// </summary>
    public const string LanguageCodeKey = "TranslationLanguageCode";

    /// <summary>
    /// HttpContext.Items key for storing the partial translation preference
    /// </summary>
    public const string AllowPartialKey = "AllowPartialTranslation";

    /// <summary>
    /// Default language code when no preference is specified
    /// </summary>
    public const string DefaultLanguageCode = "nob"; // Norwegian Bokmål
}
