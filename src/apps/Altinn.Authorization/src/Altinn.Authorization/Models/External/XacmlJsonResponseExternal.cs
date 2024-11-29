using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// The JSON Response.
    /// https://docs.oasis-open.org/xacml/xacml-json-http/v1.1/os/xacml-json-http-v1.1-os.html#_Toc5116225
    /// </summary>
    public class XacmlJsonResponseExternal
    {
        /// <summary>
        /// Gets or sets a list over JSON XACML results.
        /// </summary>
        public List<XacmlJsonResultExternal> Response { get; set; }
    }
}
