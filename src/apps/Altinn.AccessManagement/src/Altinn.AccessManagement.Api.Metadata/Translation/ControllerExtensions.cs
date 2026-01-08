using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Metadata.Translation;

/// <summary>
/// Extension methods for controllers to easily access translation context
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Gets the language code from the current HTTP context.
    /// This value is set by the TranslationMiddleware based on Accept-Language header.
    /// </summary>
    /// <param name="controller">The controller instance</param>
    /// <returns>The language code (e.g., "nob", "eng", "nno")</returns>
    public static string GetLanguageCode(this ControllerBase controller)
    {
        try
        {
            if (controller.HttpContext.Items.TryGetValue(TranslationConstants.LanguageCodeKey, out var languageCode) &&
                languageCode is string lang)
            {
                return lang;
            }

            return TranslationConstants.DefaultLanguageCode;
        }
        catch
        {
            return TranslationConstants.DefaultLanguageCode;
        }

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
}
