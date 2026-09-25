namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Decompose model for a resource
/// </summary>
public class ResourceDecomposedDto
{
    /// <summary>
    /// Actions for which access is being checked on the resource.
    /// </summary>
    public required IEnumerable<RightDecomposedDto> Rights { get; set; }
}
