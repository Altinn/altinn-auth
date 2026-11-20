namespace Altinn.Platform.Authentication.Core.Models.Rights;

/// <summary>
/// Dto for list of right keys when calling AM Connections API when delegating resource to systemuser
/// </summary>
public class RightKeyListDto
{
    /// <summary>
    /// The right keys to delegate directly to the system user
    /// </summary>
    public required IEnumerable<string> DirectRightKeys { get; set; }
}
