namespace Altinn.Authorization.Api.Contracts.Consent
{
    /// <summary>
    /// Represents a right in a consent.
    /// </summary>
    public class ConsentRightDto
    {
        /// <summary>
        /// The action in the consent. Read, write etc. Can be multiple but in most concents it is only one.
        /// </summary>
        public required List<string> Action { get; set; }

        /// <summary>
        /// The resource attribute that identifies the resource part of the right. Can be multiple but in most concents it is only one.
        /// </summary>
        public required List<ConsentResourceAttributeDto> Resource
        {
            get; set;
        }

        /// <summary>
        /// Metadata for consent resource right not required
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
