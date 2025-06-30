namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents the context of a resource in a consent request.
    /// </summary>
    public class ResourceContext
    {
        /// <summary>
        /// The resource id of the consent
        /// </summary>
        public required string ResourceId { get; set; }

        /// <summary>
        /// langauge used of the resource context when presented the context text
        /// </summary>
        public required string Language { get; set; }

        /// <summary>
        /// The resource context of the consent
        /// </summary>
        public required string Context { get; set; }
    }
}
