namespace Altinn.AccessMgmt.Persistence.Core.Models;

/// <summary>
/// Options used when changing data
/// </summary>
public class ChangeRequestOptions
{
    /// <summary>
    /// The user or entity changing the data
    /// </summary>
    public Guid ChangedBy { get; set; }

    /// <summary>
    /// The system used for changing the data
    /// (e.g a2-import, accessmgmt-api, bff)
    /// </summary>
    public Guid ChangedBySystem { get; set; }

    /// <summary>
    /// Identify operation spanning multiple tables and cascades
    /// </summary>
    public string ChangeOperationId { get; set; } = Guid.CreateVersion7().ToString();
}
