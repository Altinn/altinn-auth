namespace Altinn.Authorization.Host.Pipeline;

/// <summary>
/// Registry for all configured pipeline groups.
/// </summary>
internal class PipelineRegistry : IPipelineRegistry
{
    /// <inheritdoc/>
    public List<PipelineGroup> Groups { get; } = [];
}

/// <summary>
/// Interface for accessing registered pipeline groups.
/// </summary>
internal interface IPipelineRegistry
{
    /// <summary>
    /// Gets the list of all registered pipeline groups.
    /// </summary>
    public List<PipelineGroup> Groups { get; }
}
