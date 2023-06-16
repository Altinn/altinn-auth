using Newtonsoft.Json;

namespace Altinn.ResourceRegistry.Core.Models
{
    /// <summary>
    /// Defines a list of orgs
    /// </summary>
    public class OrgList
    {
        /// <summary>
        /// Dictionary of orgs
        /// </summary>
        [JsonProperty("orgs")]
        public Dictionary<string, Org> Orgs { get; set; }
    }
}
