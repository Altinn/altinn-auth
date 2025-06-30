namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents a dictionary of metadata.
    /// </summary>
    public class MetadataDictionary
       : Dictionary<string, string>
    {
        /// <summary>
        /// The default constructor for MetadataDictionary.
        /// </summary>
        public MetadataDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
