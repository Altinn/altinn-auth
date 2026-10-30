namespace Altinn.AccessMgmt.PersistenceEF.Models.Enums;

/// <summary>
/// Access list mode of a resource, with the same values as in the resource registry
/// </summary>
public enum ResourceAccessListMode
{
    /// <summary>
    /// Access lists are not used for the resource
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Access to the resource is limited to the parties on its access lists
    /// </summary>
    Enabled = 1,
}
