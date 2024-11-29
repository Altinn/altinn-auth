using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// A JSON object that refernces a policy or policy set.
    /// </summary>
    public class XacmlJsonPolicyIdentifierListExternal
    {
        /// <summary>
        /// Gets or sets list over policy id references.
        /// </summary>
        public List<XacmlJsonIdReferenceExternal> PolicyIdReference { get; set; }

        /// <summary>
        /// Gets or sets list policy sets references.
        /// </summary>
        public List<XacmlJsonIdReferenceExternal> PolicySetIdReference { get; set; }
    }
}
