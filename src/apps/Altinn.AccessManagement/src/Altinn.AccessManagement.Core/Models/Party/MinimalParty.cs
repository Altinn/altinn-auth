namespace Altinn.AccessManagement.Core.Models.Party
{
    /// <summary>
    /// Represents a minimal party model used in various contexts where only basic party information is needed.
    /// </summary>
    public class MinimalParty
    {
        /// <summary>
        /// The partyUuid of the party
        /// </summary>
        public Guid PartyUuid { get; set; }

        /// <summary>
        /// The partyId of the party
        /// </summary>
        public int PartyId { get; set; }

        /// <summary>
        /// The name of the party
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        ///  The organization number of the party
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// The person number of the party
        /// </summary>
        public string PersonId { get; set; }

        /// <summary>
        /// The partyType of the party
        /// </summary>
        public Guid PartyType { get; set; }
    }
}
