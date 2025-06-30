namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents the texts used in consent templates for different parties.
    /// </summary>
    public class ConsentTemplateTypeText
    {
        /// <summary>
        /// Texts used in consent for organization
        /// </summary>
        public Dictionary<string, string> Org { get; set; }

        /// <summary>
        /// Texts used in consent for person
        /// </summary>
        public Dictionary<string, string> Person { get; set; }
    }
}
