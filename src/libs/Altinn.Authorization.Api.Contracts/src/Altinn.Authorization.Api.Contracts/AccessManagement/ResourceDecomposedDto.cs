namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Decompose model for a resource
/// </summary>
public class ResourceDecomposedDto
{
    /// <summary>
    /// Resource the delegation check is regarding
    /// </summary>
    public required ResourceDto Resource { get; set; }

    /// <summary>
    /// Actions for which access is being checked on the resource.
    /// </summary>
    public required IEnumerable<RightDecomposedDto> Rights { get; set; }
}
