using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Translation;

/// <summary>
/// Extension methods for controllers to easily access translation context
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Gets the language code from HttpContext (set by TranslationMiddleware).
    /// Falls back to parsing Accept-Language header if middleware hasn't run.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>The language code (e.g., "nob", "eng", "nno")</returns>
    public static string GetLanguageCode(this ControllerBase controller)
    {
        // First, try to get the language code set by TranslationMiddleware
        if (controller.HttpContext.Items.TryGetValue(TranslationConstants.LanguageCodeKey, out var languageCode) 
            && languageCode is string code && !string.IsNullOrEmpty(code))
        {
            return code;
        }

        // Fallback: Parse Accept-Language header directly (if middleware not configured)
        if (controller.Request.Headers.TryGetValue(TranslationConstants.AcceptLanguageHeader, out var acceptLanguage))
        {
            var languageHeader = acceptLanguage.ToString();
            if (!string.IsNullOrEmpty(languageHeader))
            {
                // Parse the first language from the Accept-Language header
                // Format: "en-US,en;q=0.9,nb;q=0.8" -> extract first language
                var firstLanguage = languageHeader.Split(',')[0].Trim();

                // Map common language codes to three-letter codes
                return MapLanguageCode(firstLanguage);
            }
        }
        
        return TranslationConstants.DefaultLanguageCode;
    }

    /// <summary>
    /// Gets whether partial translation is allowed based on the X-Accept-Partial-Translation header.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>True if partial translation is allowed, false otherwise</returns>
    public static bool AllowPartialTranslation(this ControllerBase controller)
    {
        try
        {
            if (controller.HttpContext.Items.TryGetValue(TranslationConstants.AllowPartialKey, out var allowPartial) && allowPartial is bool allow)
            {
                return allow;
            }

            return true; // Default to allowing partial translations
        } 
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Sets the Content-Language response header to indicate the language being returned.
    /// This is typically called after translation is performed.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <param name="languageCode">The language code to set in the response header</param>
    public static void SetContentLanguage(this ControllerBase controller, string languageCode)
    {
        controller.Response.Headers[TranslationConstants.ContentLanguageHeader] = languageCode;
    }

    /// <summary>
    /// Maps language codes from Accept-Language header to three-letter ISO 639-2 codes.
    /// </summary>
    /// <param name="languageCode">The language code from the header (e.g., "en", "en-US", "nb", "nn")</param>
    /// <returns>Three-letter language code (e.g., "eng", "nob", "nno")</returns>
    private static string MapLanguageCode(string languageCode)
    {
        // Extract base language code (e.g., "en-US" -> "en", "nb-NO" -> "nb")
        var baseCode = languageCode.Split('-', ';')[0].Trim().ToLowerInvariant();

        return baseCode switch
        {
            "en" => "eng",
            "nb" => "nob",
            "nn" => "nno",
            "no" => "nob", // Default Norwegian to Bokmål
            "eng" => "eng",
            "nob" => "nob",
            "nno" => "nno",
            _ => TranslationConstants.DefaultLanguageCode
        };
    }
}
