namespace Altinn.Authorization.Host.Pipeline.Builders;

internal class PipelineSinkBuilder(PipelineGroup descriptor) : ISinkBuilder
{
    /// <inheritdoc/>
    public IPipelineGroup Build()
    {
        return descriptor;
    }
}

/// <summary>
/// Builder for completing pipeline configuration.
/// </summary>
public interface ISinkBuilder
{
    /// <summary>
    /// Completes the pipeline configuration and returns to the parent group.
    /// </summary>
    /// <returns>The parent group for additional configuration.</returns>
    IPipelineGroup Build();
}
