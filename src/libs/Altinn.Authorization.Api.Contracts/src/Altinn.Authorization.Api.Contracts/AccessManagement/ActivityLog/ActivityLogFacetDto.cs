namespace Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

/// <summary>
/// One value occurring in the activity log for a facet field, as an (id, name) pair. The same
/// id can appear more than once with different names, since names are point-in-time snapshots.
/// </summary>
public class ActivityLogFacetDto
{
    /// <summary>
    /// The value identifier, usable directly in the corresponding filter parameter.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name as it occurs in the log (snapshot), or from the catalog for source and
    /// activity type values.
    /// </summary>
    public string Name { get; set; }
}
