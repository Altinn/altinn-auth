using System.Collections.Generic;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// Defines a Json object for ObligationOrAdvice.
    /// </summary>
    public class XacmlJsonObligationOrAdviceExternal
    {
        /// <summary>
        /// Gets or sets a string containing a XACML obligation or advice URI.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets an array of AttributeAssignment objects.
        /// </summary>
        public List<XacmlJsonAttributeAssignmentExternal> AttributeAssignment { get; set; }
    }
}
