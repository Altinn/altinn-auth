namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// A XACML Json object for status Code.
    /// </summary>
    public class XacmlJsonStatusCodeExternal
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a nested status code.
        /// </summary>
        public XacmlJsonStatusCodeExternal StatusCode { get; set; }
    }
}
