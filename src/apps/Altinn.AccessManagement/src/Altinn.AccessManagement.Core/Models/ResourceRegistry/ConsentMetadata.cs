using System.Text.Json.Serialization;

namespace Altinn.AccessManagement.Core.Models.ResourceRegistry
{
    /// <summary>
    /// Model describing the consent metadata for a resource
    /// </summary>
    public class ConsentMetadata
    {
        /// <summary>
        /// Define if metadata is optional
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Optional { get; set; }
    }
}
