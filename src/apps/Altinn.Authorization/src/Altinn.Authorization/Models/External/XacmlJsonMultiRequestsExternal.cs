using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// A JSON object that defines references to multiple requests.
    /// </summary>
    public class XacmlJsonMultiRequestsExternal
    {
        /// <summary>
        /// Gets or sets the request reference.
        /// </summary>
        public List<XacmlJsonRequestReferenceExternal> RequestReference { get; set; }
    }
}
