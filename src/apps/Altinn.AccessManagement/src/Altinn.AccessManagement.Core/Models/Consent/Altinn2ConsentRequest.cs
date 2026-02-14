using System.Runtime.Serialization;

namespace Altinn.AccessManagement.Core.Models.Consent
{
    /// <summary>
    /// Altinn2 ConsentRequest Model
    /// </summary>
    public class Altinn2ConsentRequest
    {
        /// <summary>
        /// Defines when the consent was given.
        /// </summary>
        public DateTimeOffset? CreatedTime { get; set; }

        /// <summary>
        /// The ID for the consent request.
        /// </summary>
        public Guid ConsentGuid { get; set; }

        /// <summary>
        /// The party that has to consent to the consentRequest
        /// </summary>
        public Guid? OfferedByPartyUUID { get; set; }

        /// <summary>
        /// The party that is required to accept the consent request. 
        /// </summary>
        public Guid? RequiredDelegatorUUID { get; set; }

        /// <summary>
        /// The party requesting consent.
        /// </summary>
        public Guid? CoveredByPartyUUID { get; set; }

        /// <summary>
        /// The party that handles the consent request on behalf of the requesting party.
        /// </summary>
        public Guid? HandledByPartyUUID { get; set; }

        /// <summary>
        /// Defines how long the concent is valid
        /// </summary>
        public DateTimeOffset ValidTo { get; set; }

        /// <summary>
        /// The request message
        /// </summary>
        public Dictionary<string, string> RequestMessage { get; set; }

        /// <summary>
        /// The status of the consent request
        /// </summary>
        public string ConsentRequestStatus { get; set; }

        /// <summary>
        /// Defines when the consent was given.
        /// </summary>
        public DateTimeOffset? Consented { get; set; }

        /// <summary>
        /// when the consent was revoked.
        /// </summary>
        public List<AuthorizationRequestResourceBE> RequestResources { get; set; }

        /// <summary>
        /// when the consent was revoked.
        /// </summary>
        public List<Altinn2ConsentRequestEvent> ConsentHistoryEvents { get; set; }

        /// <summary>
        /// The redirect url for the user to be redirected after consent is given or denied.
        /// </summary>
        public string RedirectUrl { get; set; } = string.Empty;

        /// <summary>
        /// The consent template id.
        /// </summary>
        public string TemplateId { get; set; }
    }

    public class AuthorizationRequestResourceBE
    {
        /// <summary>
        /// Gets or sets the role type id in a request
        /// </summary>
        public string RoleTypeID { get; set; }

        /// <summary>
        /// Gets or sets the ExternalServiceCode in a request
        /// </summary>
        public string ServiceCode { get; set; }

        /// <summary>
        ///  Gets or sets the ExternalServiceEditionCode in a request
        /// </summary>
        public int? ServiceEditionCode { get; set; }

        /// <summary>
        ///  Gets or sets the Service Edition Version ID in the resource
        /// </summary>
        public int? ServiceEditionVersionID { get; set; }

        /// <summary>
        ///  Gets or sets the AltinnAppId in a request
        /// </summary>
        public string AltinnAppId { get; set; }

        /// <summary>
        ///  Gets or sets a list OperationType that is specified for the specific service in the request
        /// </summary>
        public List<string> Operations { get; set; }

        /// <summary>
        ///  Gets or sets a dictionary of metadata-properties on the specific service
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class Altinn2ConsentRequestEvent
    {
        /// <summary>
        /// Gets or sets the timestamp when the event was created.
        /// </summary>
        [DataMember]
        public DateTimeOffset Created { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the principal who performed the event.
        /// </summary>
        [DataMember]
        public Guid? PerformedByPartyUUID { get; set; }

        /// <summary>
        /// Gets or sets the type of event that occurred.
        /// </summary>
        [DataMember]
        public string EventType { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the consent request related to this event.
        /// </summary>
        [DataMember]
        public Guid ConsentRequestID { get; set; }
    }
}
