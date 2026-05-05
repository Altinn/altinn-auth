using Altinn.AccessMgmt.Core.Constants.Translation;
using Altinn.AccessMgmt.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

// See: overhaul part-2 step 15
namespace Altinn.AccessMgmt.Core.Tests.Extensions;

/// <summary>
/// Pure-unit tests for <see cref="ControllerExtensions"/> language-code
/// resolution and partial-translation flag reading. Covers the
/// HttpContext.Items → Accept-Language header → DefaultLanguageCode
/// fallback chain and the standalone Accept-Language parser used when
/// TranslationMiddleware hasn't run.
/// </summary>
public class ControllerExtensionsTest
{
    private sealed class TestController : ControllerBase
    {
    }

    private static TestController NewController(Action<HttpContext>? configure = null)
    {
        var ctx = new DefaultHttpContext();
        configure?.Invoke(ctx);
        return new TestController
        {
            ControllerContext = new ControllerContext { HttpContext = ctx },
        };
    }

    // ── GetLanguageCode: HttpContext.Items takes precedence ───────────────────
    [Fact]
    public void GetLanguageCode_ItemsKeySet_ReturnsItValue()
    {
        var controller = NewController(c => c.Items[TranslationConstants.LanguageCodeKey] = "eng");
        Assert.Equal("eng", controller.GetLanguageCode());
    }

    [Fact]
    public void GetLanguageCode_ItemsKeyEmpty_FallsThroughToHeaderParser()
    {
        var controller = NewController(c =>
        {
            c.Items[TranslationConstants.LanguageCodeKey] = string.Empty;
            c.Request.Headers["Accept-Language"] = "nb";
        });
        Assert.Equal("nob", controller.GetLanguageCode());
    }

    [Fact]
    public void GetLanguageCode_ItemsKeyMissing_FallsThroughToHeaderParser()
    {
        var controller = NewController(c => c.Request.Headers["Accept-Language"] = "nn");
        Assert.Equal("nno", controller.GetLanguageCode());
    }

    [Fact]
    public void GetLanguageCode_NoItemsNoHeader_ReturnsDefault()
    {
        var controller = NewController();
        Assert.Equal("nob", controller.GetLanguageCode());
    }

    // ── GetLanguageCode: Accept-Language header parsing fallback ──────────────
    [Theory]
    [InlineData("en", "eng")]
    [InlineData("nb", "nob")]
    [InlineData("nn", "nno")]
    [InlineData("no", "nob")]
    [InlineData("eng", "eng")]
    [InlineData("nob", "nob")]
    [InlineData("nno", "nno")]
    [InlineData("fr", "nob")]      // unsupported → DefaultLanguageCode
    [InlineData("xx-YY", "nob")]   // unsupported region-suffixed → DefaultLanguageCode
    public void GetLanguageCode_HeaderFallback_NormalizesCodes(string header, string expected)
    {
        var controller = NewController(c => c.Request.Headers["Accept-Language"] = header);
        Assert.Equal(expected, controller.GetLanguageCode());
    }

    [Fact]
    public void GetLanguageCode_HeaderWithMultipleLanguages_TakesFirstOne()
    {
        // Unlike TranslationMiddleware, ControllerExtensions does NOT honor q
        // values — it just splits on comma and takes the first entry.
        var controller = NewController(c => c.Request.Headers["Accept-Language"] = "nn-NO,en;q=0.9,nb;q=0.8");
        Assert.Equal("nno", controller.GetLanguageCode());
    }

    [Fact]
    public void GetLanguageCode_HeaderRegionSuffix_StrippedBeforeMapping()
    {
        var controller = NewController(c => c.Request.Headers["Accept-Language"] = "en-US");
        Assert.Equal("eng", controller.GetLanguageCode());
    }

    // ── AllowPartialTranslation ───────────────────────────────────────────────
    [Fact]
    public void AllowPartialTranslation_ItemsKeyTrue_ReturnsTrue()
    {
        var controller = NewController(c => c.Items[TranslationConstants.AllowPartialKey] = true);
        Assert.True(controller.AllowPartialTranslation());
    }

    [Fact]
    public void AllowPartialTranslation_ItemsKeyFalse_ReturnsFalse()
    {
        var controller = NewController(c => c.Items[TranslationConstants.AllowPartialKey] = false);
        Assert.False(controller.AllowPartialTranslation());
    }

    [Fact]
    public void AllowPartialTranslation_ItemsKeyMissing_DefaultsToTrue()
    {
        var controller = NewController();
        Assert.True(controller.AllowPartialTranslation());
    }

    [Fact]
    public void AllowPartialTranslation_ItemsKeyWrongType_DefaultsToTrue()
    {
        var controller = NewController(c => c.Items[TranslationConstants.AllowPartialKey] = "true");
        Assert.True(controller.AllowPartialTranslation());
    }

    // ── SetContentLanguage ────────────────────────────────────────────────────
    [Fact]
    public void SetContentLanguage_WritesContentLanguageResponseHeader()
    {
        var controller = NewController();
        controller.SetContentLanguage("eng");
        Assert.Equal("eng", controller.Response.Headers["Content-Language"].ToString());
    }
}
