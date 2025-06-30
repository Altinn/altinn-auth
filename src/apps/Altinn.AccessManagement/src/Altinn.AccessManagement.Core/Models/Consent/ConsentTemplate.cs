namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Defines a consent template
    /// </summary>
    public class ConsentTemplate
    {
        /// <summary>
        /// Template id
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Every update the template is save a new version is created
        /// </summary>
        public int? Version { get; set; }

        /// <summary>
        /// Title of the consent template
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// If template is used for a power of attorney (POA) or consent
        /// </summary>
        public bool IsPoa { get; set; }

        /// <summary>
        /// If custom message is set when sending consent request
        /// </summary>
        public bool IsMessageSetInRequest { get; set; }

        /// <summary>
        /// Only service owners in this list can use this template in consent resources
        /// </summary>
        public List<string> RestrictedToServiceOwners { get; set; }

        /// <summary>
        /// Texts for consent content and history
        /// </summary>
        public required ConsentTemplateTexts Texts { get; set; }
    }
}
