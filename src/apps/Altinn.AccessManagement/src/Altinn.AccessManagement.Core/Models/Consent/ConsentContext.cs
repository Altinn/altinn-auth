namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Represents the context in which consent is given or requested.
    /// </summary>
    public class ConsentContext
    {
        /// <summary>
        /// The language used when consenting
        /// </summary>
        public required string Language { get; set; }

        /// <summary>
        /// ContextIxtentifier. This is a unique identifier for the context. It is used to identify the context in the system.
        /// </summary>
        public Guid? ContextId { get; set; }
    }
}
