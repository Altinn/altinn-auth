using Altinn.AccessManagement.Core.Models;

namespace Altinn.AccessManagement.TestUtils.Models.ResourceRegistry
{
    /// <summary>
    /// Copy from Resource Registry Right model, but with AccessorUrns instead of HasPermit and CanDelegate, as this is the model used in the ResourceRegistryClientMock
    /// </summary>
    public class Right
    {
        /// <summary>
        /// Unique key for action
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Set of accessor URNs that define which subjects or access packages are allowed for this right in the mocked resource registry.
        /// </summary>
        public HashSet<string> AccessorUrns { get; set; }

        /// <summary>
        /// Name of the action to present in frontend
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Concatenated key for subresources from policy rule
        /// </summary>
        public IEnumerable<PolicyAttributeMatch> Resource { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public PolicyAttributeMatch Action { get; set; }
    }
}
