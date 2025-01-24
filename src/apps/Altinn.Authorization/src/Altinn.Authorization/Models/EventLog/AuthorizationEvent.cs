#nullable enable

using System;
using Altinn.Authorization.ABAC.Xacml;

namespace Altinn.Platform.Authorization.Models.EventLog
{
    /// <summary>
    /// This model describes an authorization event. An authorization event is an action triggered when a user requests access to an operation
    /// </summary>
    public class AuthorizationEvent
    {
        /// <summary>
        /// Session Id of the request
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Date and time of the authorization event. Set by producer of logevents
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// The userid for the user that requested authorization
        /// </summary>
        public int? SubjectUserId { get; set; }

        /// <summary>
        /// The org code for the org that requested authorization
        /// </summary>
        public string? SubjectOrgCode { get; set; }

        /// <summary>
        /// The org number for the org that requested authorization
        /// </summary>
        public int? SubjectOrgNumber { get; set; }

        /// <summary>
        /// The partyid for the user that requested authorization
        /// </summary>
        public int? SubjectParty { get; set; }

        /// <summary>
        /// The partyId for resource owner when applicable
        /// </summary>
        public int? ResourcePartyId { get; set; }

        /// <summary>
        /// The Main resource Id (app, external resource +)
        /// </summary>
        public string? Resource { get; set; }

        /// <summary>
        /// Instance Id when applicable
        /// </summary>
        public string? InstanceId { get; set; }

        /// <summary>
        /// Type of operation
        /// </summary>
        public required string Operation { get; set; }

        /// <summary>
        /// The Ip adress of the calling party
        /// </summary>
        public string? IpAdress { get; set; }

        /// <summary>
        /// The enriched context request
        /// </summary>
        public required string ContextRequestJson { get; set; }

        /// <summary>
        /// Decision for the authorization request
        /// </summary>
        public XacmlContextDecision? Decision { get; set; }

        /// <summary>
        /// The party uuid
        /// </summary>
        public string? SubjectPartyUuid { get; set; }
    }
}
