using System.ComponentModel.DataAnnotations;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Entity to hold ResourceRegister resource with information needed for AccessManagement
    /// </summary>
    public class AccessManagementResource
    {
        /// <summary>
        /// Constructor to create an AccessManagementResource based on an input ServiceResource
        /// </summary>
        /// <param name="serviceResource">inut to create the entity based on</param>
        public AccessManagementResource(ServiceResource serviceResource)
        {
            this.ResourceRegistryId = serviceResource.Identifier;
            this.ResourceType = serviceResource.ResourceType.ToString();
        }

        /// <summary>
        /// Constructor to create an emptyAccessManagementResource
        /// </summary>
        public AccessManagementResource()
        {
        }

        /// <summary>
        /// Primary key created when inserted in Access management
        /// </summary>
        public int? ResourceId { get; set; }

        /// <summary>
        /// The resource registry id
        /// </summary>
        [Required]
        public string ResourceRegistryId { get; set; }

        /// <summary>
        /// The type of resource
        /// </summary>
        [Required]
        public string ResourceType { get; set; }

        /// <summary>
        /// When the resource was created in access management
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// The last time modified in access management
        /// </summary>
        public DateTime? Modified { get; set; }
    }
}
