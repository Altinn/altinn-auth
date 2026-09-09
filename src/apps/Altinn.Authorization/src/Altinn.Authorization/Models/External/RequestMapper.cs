using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Riok.Mapperly.Abstractions;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// Maps between the external XACML JSON models exposed by the authorize endpoint
    /// and the internal XACML JSON profile models.
    /// </summary>
    [Mapper(UseDeepCloning = true)]
    public static partial class RequestMapper
    {
        /// <summary>
        /// Maps an external XACML JSON request to the internal representation.
        /// </summary>
        /// <param name="request">The external request root.</param>
        /// <returns>The internal request root.</returns>
        public static partial XacmlJsonRequestRoot ToInternal(this XacmlJsonRequestRootExternal request);

        /// <summary>
        /// Maps an internal XACML JSON response to the external representation.
        /// </summary>
        /// <param name="response">The internal response.</param>
        /// <returns>The external response.</returns>
        public static partial XacmlJsonResponseExternal ToExternal(this XacmlJsonResponse response);

        // XForwardedForHeader is enrichment set from the incoming HTTP request, never part of the request body.
        [MapperIgnoreTarget(nameof(XacmlJsonRequest.XForwardedForHeader))]
        private static partial XacmlJsonRequest ToInternal(this XacmlJsonRequestExternal request);
    }
}
