namespace Altinn.AccessMgmt.Core.Utils.Models;

/// <summary>
/// Bundles the per-request language preference and the partial-translation fallback flag
/// so that service operations can pass them as a single argument.
/// </summary>
/// <param name="LanguageCode">ISO 639-2 language code (e.g. "nob").</param>
/// <param name="AllowPartial">If true, missing translations fall back to the source language instead of returning null.</param>
public sealed record TranslationOptions(string LanguageCode = "nob", bool AllowPartial = true);
