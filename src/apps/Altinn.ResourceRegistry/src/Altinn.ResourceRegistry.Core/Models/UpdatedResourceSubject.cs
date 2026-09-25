namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// Entry describing resource/subject pair that has been updated.
/// </summary>
public class UpdatedResourceSubject
{
    /// <summary>
    /// The subject with a relation to the resource
    /// </summary>
    public Uri SubjectUrn { get; set; }

    /// <summary>
    /// The resource that the subject has a relation to
    /// </summary>
    public Uri ResourceUrn { get; set; }

    /// <summary>
    /// When the relation was created/deleted
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// If the relation has been deleted and should be disregarded
    /// </summary>
    public bool Deleted { get; set; }
}
