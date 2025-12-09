using Altinn.AccessManagement.Api.Metadata.Translation;
using Microsoft.Extensions.Primitives;

namespace Altinn.AccessManagement.Api.Metadata.Middleware;

/// <summary>
/// Middleware that extracts language preferences from HTTP headers and stores them in HttpContext
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
    /// Supports quality values (q) and returns the highest priority language.
    /// </summary>
    private static string ExtractLanguageCode(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TranslationConstants.AcceptLanguageHeader, out var acceptLanguage) ||
            acceptLanguage == StringValues.Empty)
        {
            return TranslationConstants.DefaultLanguageCode;
        }

        var languages = ParseAcceptLanguageHeader(acceptLanguage.ToString());
        return languages.FirstOrDefault() ?? TranslationConstants.DefaultLanguageCode;
    }

    /// <summary>
    /// Parses the Accept-Language header and returns languages ordered by quality value
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
                var qPart = segments[1].Trim();
                if (qPart.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(qPart.Substring(2), out var parsedQuality))
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
    /// Extracts the partial translation preference from the custom header
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
/// Extension methods for adding the translation middleware to the application pipeline
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
