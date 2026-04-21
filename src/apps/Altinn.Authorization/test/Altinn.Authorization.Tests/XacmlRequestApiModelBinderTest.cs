using Altinn.Platform.Authorization.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;

namespace Altinn.Platform.Authorization.Tests;

public class XacmlRequestApiModelBinderTest
{
    [Fact]
    public async Task BindModelAsync_NullContext_ThrowsArgumentNullException()
    {
        var binder = new XacmlRequestApiModelBinder();

        await Assert.ThrowsAsync<ArgumentNullException>(() => binder.BindModelAsync(null));
    }

    [Fact]
    public async Task BindModelAsync_ValidBody_SetsModelWithBodyContent()
    {
        var binder = new XacmlRequestApiModelBinder();
        string expectedBody = "{\"Request\":{}}";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(expectedBody));

        var bindingContext = CreateBindingContext(httpContext, isTopLevel: true);

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);
        var model = Assert.IsType<XacmlRequestApiModel>(bindingContext.Result.Model);
        Assert.Equal(expectedBody, model.BodyContent);
    }

    [Fact]
    public async Task BindModelAsync_EmptyBody_SetsModelWithEmptyString()
    {
        var binder = new XacmlRequestApiModelBinder();

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());

        var bindingContext = CreateBindingContext(httpContext, isTopLevel: true);

        await binder.BindModelAsync(bindingContext);

        Assert.True(bindingContext.Result.IsModelSet);
        var model = Assert.IsType<XacmlRequestApiModel>(bindingContext.Result.Model);
        Assert.Equal(string.Empty, model.BodyContent);
    }

    private static DefaultModelBindingContext CreateBindingContext(HttpContext httpContext, bool isTopLevel)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(typeof(XacmlRequestApiModel));

        return new DefaultModelBindingContext
        {
            ModelMetadata = metadata,
            ModelName = string.Empty,
            ModelState = new ModelStateDictionary(),
            ActionContext = new ActionContext { HttpContext = httpContext },
            IsTopLevelObject = isTopLevel,
        };
    }
}
