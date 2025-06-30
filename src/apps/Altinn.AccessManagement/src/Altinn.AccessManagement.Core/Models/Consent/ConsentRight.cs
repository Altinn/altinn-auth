namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents a right in a consent.
    /// </summary>
    public class ConsentRight
    {
        /// <summary>
        /// The action in the consent. Read, write etc. Can be multiple but in most concents it is only one.
        /// </summary>
        public required List<string> Action { get; set; }

        /// <summary>
        /// The resource attribute that identifies the resource part of the right. Can be multiple but in most concents it is only one.
        /// </summary>
        public required List<ConsentResourceAttribute> Resource
        {
            get; set;
        }

        /// <summary>
        /// The metadata for the right. Can be multiple but in most concents it is only one.   
        /// Keys are case insensitive.
        /// </summary>
        public MetadataDictionary Metadata { get; set; }

        /// <summary>
        /// Adds metadata values to the ConsentRight.
        /// </summary>
        public void AddMetadataValues(IReadOnlyDictionary<string, string> dictionary)
        {
            if (dictionary != null)
            {
                Metadata = [];
                foreach (var item in dictionary)
                {
                    Metadata.Add(item.Key, item.Value);
                }
            }   
        }
     }
}
