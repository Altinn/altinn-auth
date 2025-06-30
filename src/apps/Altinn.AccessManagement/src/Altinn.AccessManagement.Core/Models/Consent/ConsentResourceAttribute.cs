namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// A resurce attribute identifying part or whole resource
    /// </summary>
    public class ConsentResourceAttribute
    {
        /// <summary>
        /// The type of resource attribute. is a urn
        /// </summary>
        public required string Type { get; set; }

        /// <summary>
        /// The value of the resource attribute
        /// </summary>
        public required string Value { get; set; }

        /// <summary>
        /// The version of the resource attribute
        /// </summary>
        public string Version { get; set; }
    }
}
