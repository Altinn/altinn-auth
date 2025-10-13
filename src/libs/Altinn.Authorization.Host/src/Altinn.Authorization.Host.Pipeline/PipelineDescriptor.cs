using Altinn.Authorization.Host.Pipeline.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.Authorization.Host.Pipeline;

/// <summary>
/// Descriptor for configuring an individual pipeline.
/// </summary>
internal class PipelineDescriptor(PipelineGroup descriptor) : IPipelineDescriptor
{
    /// <inheritdoc/>
    public string? Name { get; private set; } 

    /// <inheritdoc/>
    public Func<IServiceProvider, IServiceScope>? ServiceScope { get; set; }
    
    internal PipelineSourceBuilder? Source { get; private set; }

    internal string? LeaseName { get; private set; }

    /// <inheritdoc/>
    public IPipelineDescriptor WithName(string name)
    {
        Name = name;
        return this;
    }

    /// <inheritdoc/>
    public IPipelineDescriptor WithLease(string lease)
    {
        LeaseName = lease;
        return this;
    }

    /// <inheritdoc/>
    public IPipelineDescriptor WithServiceScope(Func<IServiceProvider, IServiceScope> func)
    {
        ServiceScope = func;
        return this;
    }

    /// <inheritdoc/>
    public ISourceBuilder WithStages()
    {
        Source = new PipelineSourceBuilder(descriptor);
        return Source;
    }
}

/// <summary>
/// Interface for configuring a pipeline.
/// </summary>
public interface IPipelineDescriptor
{
    /// <summary>
    /// Gets the pipeline name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the custom service scope factory for this pipeline.
    /// </summary>
    Func<IServiceProvider, IServiceScope>? ServiceScope { get; }

    /// <summary>
    /// Sets the pipeline name.
    /// </summary>
    /// <param name="name">The pipeline name.</param>
    /// <returns>The pipeline descriptor for chaining.</returns>
    IPipelineDescriptor WithName(string name);

    /// <summary>
    /// Associates a distributed lease with this pipeline.
    /// </summary>
    /// <param name="lease">The lease name.</param>
    /// <returns>The pipeline descriptor for chaining.</returns>
    IPipelineDescriptor WithLease(string lease);

    /// <summary>
    /// Configures a custom service scope factory for this pipeline.
    /// </summary>
    /// <param name="func">The service scope factory function.</param>
    /// <returns>The pipeline descriptor for chaining.</returns>
    IPipelineDescriptor WithServiceScope(Func<IServiceProvider, IServiceScope> func);

    /// <summary>
    /// Begins configuring the pipeline stages (source, segments, and sink).
    /// </summary>
    /// <returns>A source builder to configure the pipeline source.</returns>
    ISourceBuilder WithStages();
}
