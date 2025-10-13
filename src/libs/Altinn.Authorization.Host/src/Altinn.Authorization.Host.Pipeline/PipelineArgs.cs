using Altinn.Authorization.Host.Lease;

namespace Altinn.Authorization.Host.Pipeline;

/// <summary>
/// Shared state for a pipeline execution.
/// </summary>
public class PipelineArgs
{
    /// <summary>
    /// Stage Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Descriptor
    /// </summary>
    public IPipelineDescriptor Descriptor { get; internal set; }
    
    /// <summary>
    /// The distributed lease associated with this pipeline, if configured.
    /// </summary>
    public ILease? Lease { get; internal set; }
}
