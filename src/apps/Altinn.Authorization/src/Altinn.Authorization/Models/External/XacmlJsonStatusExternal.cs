using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// XACML Json object for status.
    /// </summary>
    public class XacmlJsonStatusExternal
    {
        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        ///  Gets or sets list over status details.
        /// </summary>
        public List<string> StatusDetails { get; set; }

        /// <summary>
        /// Gets or sets the defined status code.
        /// </summary>
        public XacmlJsonStatusCodeExternal StatusCode { get; set; }
    }
}
