namespace Altinn.Platform.Authorization.Models
{
    /// <summary>
    /// The type of delegation change
    /// </summary>
    public enum PolicyResourceType
    {
        /// <summary>
        /// Undefined default value
        /// </summary>
        // ReSharper disable UnusedMember.Global
        Undefined = 0,

        /// <summary>
        /// Policy for an application hosted in Altinn Apps
        /// </summary>
        AltinnApps = 1,

        /// <summary>
        /// Revo
        /// </summary>
        ResourceRegistry = 2,
    }
}
