namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// The JSON object root needed to be able to parse the request.
    /// </summary>
    public class XacmlJsonRequestRootExternal
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        public XacmlJsonRequestExternal Request { get; set; }
    }
}
