namespace Altinn.ResourceRegistry.Models
{
    /// <summary>
    /// This model represents a pair of attribute type and value, which can be used for matching in XACML policies, such as resources, users, parties, or actions. It is a simplified version of the internal AttributeMatchV3 model, designed for external communication and data transfer purposes.
    /// </summary>
    public class AttributeMatchDTO
    {
        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }
    }
}
