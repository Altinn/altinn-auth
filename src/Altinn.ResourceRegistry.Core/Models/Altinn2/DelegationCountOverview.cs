#nullable enable

namespace Altinn.ResourceRegistry.Core.Models.Altinn2
{
    /// <summary>
    /// Model for holding counts of delegations and relations
    /// </summary>
    public class DelegationCountOverview
    {
        /// <summary>
        /// Gets or sets the number of delegations
        /// </summary>
        public int NumberOfDelegations { get; set; }

        /// <summary>
        /// Gets or sets the number of relations
        /// </summary>
        public int NumberOfRelations { get; set; }
    }
}
