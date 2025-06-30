using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Model describing the consent metadata for a resource
    /// </summary>
    public class ConsentMetadata
    {
        /// <summary>
        /// Define if metadata is optional
        /// </summary>
        public bool Optional { get; set; }
    }
}
