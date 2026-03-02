namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Internal model representing a right, which is a combination of resource and action. This model is used within the core of the resource registry to manage and process rights associated with resources. It includes properties for a unique key, display name, associated resources, and action identifier.
    /// </summary>
    public class Right
    {
        /// <summary>
        /// Unique key for action
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The name of the right to be presented in frontend, this is not used for any processing but only for display purposes. The name can be derived from the action part of the right key or can be a more user-friendly name associated with the right.
        /// </summary>
        public IEnumerable<string> AccessorUrns { get; set; }

        /// <summary>
        /// Name of the action to present in frontend
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Concatenated key for subresources from policy rule
        /// </summary>
        public IEnumerable<string> Resource { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public string Action { get; set; }
    }
}
