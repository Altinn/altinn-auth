namespace Altinn.ResourceRegistry.Core.Enums
{
    /// <summary>
    /// Enum representation of the different types of ResourceAccessListModes supported by the resource registry
    /// NOTE! This enum is also used in the access management database, and should not be changed without considering the implications for that database.
    /// </summary>
    public enum ResourceAccessListMode
    {
        Disabled = 0,

        Enabled = 1,
    }
}
