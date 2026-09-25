using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// Entry describing a resource that has been changed. A change is any new version of the
/// resource (created or updated metadata) or an update to the resource policy.
/// </summary>
public class ResourceChange
{
    /// <summary>
    /// The resource identifier
    /// </summary>
    public string ResourceId { get; set; }

    /// <summary>
    /// When the resource was last changed
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// The global change id of the resource. Used as continuation cursor
    /// when paginating over changes and not exposed in the API response.
    /// </summary>
    [JsonIgnore]
    public long ChangeId { get; set; }
}
