using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// JSON object for request references.
    /// </summary>
    public class XacmlJsonRequestReferenceExternal
    {
        /// <summary>
        /// Gets or sets the reference Id.
        /// </summary>
        public List<string> ReferenceId { get; set; }
    }
}
