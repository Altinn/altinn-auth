namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Dto for response of Resource decomposed
/// </summary>
public class RightDecomposedDto
{
    /// <summary>
    /// Right key data
    /// </summary>
    public required RightDto Right { get; set; }
}
