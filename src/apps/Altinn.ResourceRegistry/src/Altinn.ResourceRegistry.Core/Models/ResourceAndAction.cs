namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines a resource with associated action. This model is used to represent a combination of resources and actions, typically in the context of permissions or access control. The Resource property holds a collection of resource identifiers, while the Action property specifies the action associated with those resources, such as "read", "write", "delete", etc.
    /// </summary>
    public class ResourceAndAction
    {
        /// <summary>
        /// Gets or sets the collection of resource identifiers associated with the current instance.
        /// </summary>
        public IEnumerable<string> Resource { get; set; }

        /// <summary>
        /// Action part of the permission, e.g. "read", "write", "delete" etc.
        /// </summary>
        public string Action { get; set; }
    }
}
