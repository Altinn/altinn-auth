using Altinn.Platform.Authorization.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Altinn.Platform.Authorization.Tests;

public class XacmlRequestApiModelBinderProviderTest
{
    [Fact]
    public void GetBinder_NullContext_ThrowsArgumentNullException()
    {
        var provider = new XacmlRequestApiModelBinderProvider();

        Assert.Throws<ArgumentNullException>(() => provider.GetBinder(null));
    }

    [Fact]
    public void GetBinder_XacmlRequestApiModelType_ReturnsModelBinder()
    {
        var provider = new XacmlRequestApiModelBinderProvider();
        var context = CreateProviderContext(typeof(XacmlRequestApiModel));

        IModelBinder result = provider.GetBinder(context);

        Assert.NotNull(result);
        Assert.IsType<XacmlRequestApiModelBinder>(result);
    }

    [Fact]
    public void GetBinder_OtherType_ReturnsNull()
    {
        var provider = new XacmlRequestApiModelBinderProvider();
        var context = CreateProviderContext(typeof(string));

        IModelBinder result = provider.GetBinder(context);

        Assert.Null(result);
    }

    private static ModelBinderProviderContext CreateProviderContext(Type modelType)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var metadata = metadataProvider.GetMetadataForType(modelType);
        return new TestModelBinderProviderContext(metadata);
    }

    private sealed class TestModelBinderProviderContext : ModelBinderProviderContext
    {
        public TestModelBinderProviderContext(ModelMetadata metadata)
        {
            Metadata = metadata;
            MetadataProvider = new EmptyModelMetadataProvider();
        }

        public override ModelMetadata Metadata { get; }

        public override IModelMetadataProvider MetadataProvider { get; }

        public override BindingInfo BindingInfo => throw new NotImplementedException();

        public override IModelBinder CreateBinder(ModelMetadata metadata) => throw new NotImplementedException();

        public override IModelBinder CreateBinder(ModelMetadata metadata, BindingInfo bindingInfo) => throw new NotImplementedException();
    }
}
