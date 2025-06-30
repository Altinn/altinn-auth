namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Wrapper class for ConsentRequest to be used in the consent request service to return if the consent request already existed or not.
    /// </summary>
    public class ConsentRequestDetailsWrapper
    {
        /// <summary>
        /// Represents a consent request.
        /// </summary>
        public required ConsentRequestDetails ConsentRequest { get; set; }

        /// <summary>
        /// Indicates if the consent request already existed in the system.
        /// </summary>
        public bool AlreadyExisted { get; set; }
    }
}
