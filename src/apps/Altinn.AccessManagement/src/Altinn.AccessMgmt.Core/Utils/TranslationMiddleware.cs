using Altinn.AccessMgmt.Core.Constants.Translation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Middleware that extracts language preferences from HTTP headers and stores them in HttpContext.
/// This middleware normalizes language codes to ISO 639-2 format before downstream processing.
/// </summary>
public class TranslationMiddleware
{
    private readonly RequestDelegate _next;

    public TranslationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract language preference from Accept-Language header
        var languageCode = ExtractLanguageCode(context);
        context.Items[TranslationConstants.LanguageCodeKey] = languageCode;

        // Extract partial translation preference
        var allowPartial = ExtractPartialTranslationPreference(context);
        context.Items[TranslationConstants.AllowPartialKey] = allowPartial;

        await _next(context);

        // Set Content-Language response header to indicate the language used
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[TranslationConstants.ContentLanguageHeader] = languageCode;
        }
    }

    /// <summary>
    /// Extracts the preferred language code from the Accept-Language header.
    /// Supports quality values (q) and returns the highest priority supported language.
    /// </summary>
    private static string ExtractLanguageCode(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TranslationConstants.AcceptLanguageHeader, out var acceptLanguage) ||
            acceptLanguage == StringValues.Empty)
        {
            return TranslationConstants.DefaultLanguageCode;
        }

        var languages = ParseAcceptLanguageHeader(acceptLanguage.ToString());
        
        // Try to normalize each language code in order of preference
        foreach (var lang in languages)
        {
            var normalized = NormalizeLanguageCode(lang);
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }
        }

        return TranslationConstants.DefaultLanguageCode;
    }

    /// <summary>
    /// Parses the Accept-Language header and returns languages ordered by quality value.
    /// Quality values (q) range from 0 to 1, with 1 being the highest priority.
    /// </summary>
    private static List<string> ParseAcceptLanguageHeader(string acceptLanguageHeader)
    {
        var languages = new List<(string Language, double Quality)>();

        var parts = acceptLanguageHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var segments = part.Trim().Split(';');
            var language = segments[0].Trim();
            var quality = 1.0;

            if (segments.Length > 1)
            {
                var qpart = segments[1].Trim();
                if (qpart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(qpart.AsSpan(2), out var parsedQuality))
                    {
                        quality = parsedQuality;
                    }
                }
            }

            languages.Add((language, quality));
        }

        return languages
            .OrderByDescending(x => x.Quality)
            .Select(x => x.Language)
            .ToList();
    }

    /// <summary>
    /// Normalizes language codes to ISO 639-2 three-letter format.
    /// 
    /// This is the PRIMARY normalization point for HTTP requests. Language codes are 
    /// normalized here before being stored in HttpContext, ensuring all downstream 
    /// components (controllers, services) receive properly formatted codes.
    /// 
    /// Supported mappings:
    /// - en, eng, en-US, en-GB → eng (English)
    /// - nb, nob, nb-NO, no → nob (Norwegian Bokmål)
    /// - nn, nno, nn-NO → nno (Norwegian Nynorsk)
    /// - Unknown/unsupported → null (triggers fallback to default)
    /// </summary>
    private static string? NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return TranslationConstants.DefaultLanguageCode;
        }

        // Extract the base language code (remove region codes like -US, -GB)
        var normalized = languageCode.ToLowerInvariant().Split('-')[0];

        return normalized switch
        {
            "en" or "eng" => "eng",
            "nb" or "nob" or "no" => "nob",
            "nn" or "nno" => "nno",
            _ => null // Return null for unsupported languages, allowing fallback to default
        };
    }

    /// <summary>
    /// Extracts the partial translation preference from the custom X-Accept-Partial-Translation header.
    /// </summary>
    private static bool ExtractPartialTranslationPreference(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TranslationConstants.PartialTranslationHeader, out var headerValue) ||
            headerValue == StringValues.Empty)
        {
            return true; // Default to allowing partial translations
        }

        var value = headerValue.ToString().ToLowerInvariant();
        
        return value switch
        {
            "true" or "1" or "yes" => true,
            "false" or "0" or "no" => false,
            _ => true // Default to allowing partial translations
        };
    }
}

/// <summary>
/// Extension methods for adding the translation middleware to the application pipeline.
/// </summary>
public static class TranslationMiddlewareExtensions
{
    /// <summary>
    /// Adds the translation middleware to the application pipeline.
    /// This should be added early in the pipeline to ensure language preferences are available.
    /// </summary>
    public static IApplicationBuilder UseTranslation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TranslationMiddleware>();
    }
}
