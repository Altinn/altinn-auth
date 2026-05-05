using Altinn.AccessMgmt.Core.Constants.Translation;
using Altinn.AccessMgmt.Core.Utils;
using Microsoft.AspNetCore.Http;

// See: overhaul part-2 step 15
namespace Altinn.AccessMgmt.Core.Tests.Utils;

/// <summary>
/// Pure-unit tests for <see cref="TranslationMiddleware"/>. Pins
/// Accept-Language parsing (including q-value ordering),
/// language-code normalization to ISO 639-2 three-letter codes,
/// X-Accept-Partial-Translation parsing, and the Content-Language
/// response-header set behavior.
/// </summary>
public class TranslationMiddlewareTest
{
    private static async Task<HttpContext> Run(
        Action<HttpContext>? configureRequest = null,
        Action<HttpContext>? configureMidware = null)
    {
        var context = new DefaultHttpContext();
        configureRequest?.Invoke(context);

        bool nextCalled = false;
        var middleware = new TranslationMiddleware(ctx =>
        {
            nextCalled = true;
            configureMidware?.Invoke(ctx);
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);
        Assert.True(nextCalled);
        return context;
    }

    // ── Accept-Language parsing ───────────────────────────────────────────────
    [Fact]
    public async Task NoAcceptLanguageHeader_DefaultsToNob()
    {
        var ctx = await Run();
        Assert.Equal("nob", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Theory]
    [InlineData("en", "eng")]
    [InlineData("eng", "eng")]
    [InlineData("nb", "nob")]
    [InlineData("nob", "nob")]
    [InlineData("no", "nob")]
    [InlineData("nn", "nno")]
    [InlineData("nno", "nno")]
    public async Task SingleLanguage_NormalizedToIso639_2(string input, string expected)
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = input);
        Assert.Equal(expected, ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Theory]
    [InlineData("en-US", "eng")]
    [InlineData("nb-NO", "nob")]
    [InlineData("nn-NO", "nno")]
    public async Task RegionSuffix_StrippedBeforeNormalization(string input, string expected)
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = input);
        Assert.Equal(expected, ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Fact]
    public async Task MultipleLanguages_HighestQualityWins()
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = "en;q=0.5,nb;q=0.9");
        Assert.Equal("nob", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Fact]
    public async Task MultipleLanguages_NoQualityValues_FirstWins()
    {
        // No q-values → all default to 1.0 → OrderByDescending preserves input order (stable sort).
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = "en,nb");
        Assert.Equal("eng", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Fact]
    public async Task UnsupportedLanguage_FallsThroughToDefault()
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = "fr");
        Assert.Equal("nob", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Fact]
    public async Task UnsupportedLanguageHighQuality_SupportedLanguageLowQuality_SupportedWins()
    {
        // The middleware normalizes each language in q-order; the first one
        // that normalizes to a non-null code wins. So fr (q=0.9, returns null)
        // is skipped and en (q=0.1, returns "eng") wins.
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = "fr;q=0.9,en;q=0.1");
        Assert.Equal("eng", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    [Fact]
    public async Task EmptyAcceptLanguageHeader_DefaultsToNob()
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = string.Empty);
        Assert.Equal("nob", ctx.Items[TranslationConstants.LanguageCodeKey]);
    }

    // ── X-Accept-Partial-Translation parsing ──────────────────────────────────
    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("TRUE", true)]
    [InlineData("YES", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("FALSE", false)]
    [InlineData("garbage", true)] // unrecognized → default true
    public async Task PartialTranslationHeader_ParsedCorrectly(string headerValue, bool expected)
    {
        var ctx = await Run(c => c.Request.Headers["X-Accept-Partial-Translation"] = headerValue);
        Assert.Equal(expected, ctx.Items[TranslationConstants.AllowPartialKey]);
    }

    [Fact]
    public async Task PartialTranslationHeader_Missing_DefaultsToTrue()
    {
        var ctx = await Run();
        Assert.Equal(true, ctx.Items[TranslationConstants.AllowPartialKey]);
    }

    // ── Content-Language response header ──────────────────────────────────────
    [Fact]
    public async Task ResponseNotStarted_ContentLanguageHeaderSet()
    {
        var ctx = await Run(c => c.Request.Headers["Accept-Language"] = "nb");

        Assert.Equal("nob", ctx.Response.Headers["Content-Language"].ToString());
    }

    [Fact]
    public async Task ResponseNotStarted_DefaultLanguage_ContentLanguageHeaderSetToDefault()
    {
        var ctx = await Run();

        Assert.Equal("nob", ctx.Response.Headers["Content-Language"].ToString());
    }
}
