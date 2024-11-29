using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// Defines a list of json request
    /// </summary>
    public class XacmlJsonRequestsExternal
    {
        /// <summary>
        /// A list of requests
        /// </summary>
        public List<XacmlJsonRequestExternal> Requests { get; set; }
    }
}
