#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Altinn.ResourceRegistry.Tests.Utils;

public class TestWebApplicationFactory 
    : WebApplicationFactory<TestWebApplicationFactory>
{
    protected virtual WebApplicationBuilder CreateWebApplicationBuilder()
    {
        var partManager = new ApplicationPartManager();
        ConfigureApplicationParts(partManager);

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(partManager);

        return builder;
    }

    protected virtual WebApplication CreateWebApplication(WebApplicationBuilder builder)
        => builder.Build();

    protected virtual void ConfigureWebApplication(WebApplication app)
    {
    }

    protected virtual void ConfigureApplicationParts(ApplicationPartManager partManager)
    {
    }

    protected sealed override IHostBuilder? CreateHostBuilder()
        => new TestHostBuilderWrapper(CreateWebApplicationBuilder());

    protected sealed override IHost CreateHost(IHostBuilder builder)
    {
        var appBuilder = ((TestHostBuilderWrapper)builder).Builder;
        var app = CreateWebApplication(appBuilder);

        ConfigureWebApplication(app);
        app.Start();

        return app;
    }

    private class TestHostBuilderWrapper(WebApplicationBuilder builder) 
        : IHostBuilder
    {
        public IDictionary<object, object> Properties => builder.Host.Properties;

        internal WebApplicationBuilder Builder => builder;

        IHost IHostBuilder.Build()
        {
            throw new NotSupportedException();
        }

        [SuppressMessage("Usage", "ASP0013:Suggest switching from using Configure methods to WebApplicationBuilder.Configuration", Justification = "Forwarding interface")]
        public IHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            builder.Host.ConfigureAppConfiguration(configureDelegate);

            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            builder.Host.ConfigureContainer(configureDelegate);

            return this;
        }

        [SuppressMessage("Usage", "ASP0013:Suggest switching from using Configure methods to WebApplicationBuilder.Configuration", Justification = "Forwarding interface")]
        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            builder.Host.ConfigureHostConfiguration(configureDelegate);

            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            builder.Host.ConfigureServices(configureDelegate);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory) where TContainerBuilder : notnull
        {
            builder.Host.UseServiceProviderFactory(factory);

            return this;
        }

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory) where TContainerBuilder : notnull
        {
            builder.Host.UseServiceProviderFactory(factory);

            return this;
        }
    }
}

public class TestControllerApplicationFactory
    : TestWebApplicationFactory
{
    protected static void AddTestController(ApplicationPartManager partManager, TypeInfo type)
    {
        partManager.ApplicationParts.Add(new TestControllerApplicationPart(type));
        if (!partManager.FeatureProviders.Contains(TestControllerApplicationFeatureProvider.Instance))
        {
            partManager.FeatureProviders.Add(TestControllerApplicationFeatureProvider.Instance);
        }
    }

    protected override WebApplicationBuilder CreateWebApplicationBuilder()
    {
        var builder = base.CreateWebApplicationBuilder();

        builder.Services.AddMvcCore().AddControllersAsServices();
        return builder;
    }

    protected override WebApplication CreateWebApplication(WebApplicationBuilder builder)
    {
        var app = base.CreateWebApplication(builder);

        app.MapControllers();
        return app;
    }

    private class TestControllerApplicationPart(TypeInfo type)
        : ApplicationPart
    {
        public override string Name => $"{nameof(TestControllerApplicationPart)}:{type.FullName}";

        public TypeInfo Type => type;
    }

    private class TestControllerApplicationFeatureProvider
        : IApplicationFeatureProvider<ControllerFeature>
    {
        public static IApplicationFeatureProvider Instance { get; } = new TestControllerApplicationFeatureProvider();

        private TestControllerApplicationFeatureProvider()
        {
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var part in parts.OfType<TestControllerApplicationPart>())
            {
                feature.Controllers.Add(part.Type);
            }
        }
    }
}

public class TestControllerApplicationFactory<T>
    : TestControllerApplicationFactory
{
    protected override void ConfigureApplicationParts(ApplicationPartManager partManager)
    {
        AddTestController(partManager, typeof(T).GetTypeInfo());

        base.ConfigureApplicationParts(partManager);
    }
}
