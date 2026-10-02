namespace Altinn.Platform.Authorization.Models
{
    using Altinn.Authorization.Enums;

    /// <summary>
    /// Defines the resource attributes in a xacml request
    /// </summary>
    public class XacmlResourceAttributes
    {
        /// <summary>
        /// Gets or sets the value for org attribute
        /// </summary>
        public string OrgValue { get; set; }

        /// <summary>
        /// Gets or sets the value for app attribute
        /// </summary>
        public string AppValue { get; set; }

        /// <summary>
        /// Gets or sets the value for instance attribute
        /// </summary>
        public string InstanceValue { get; set; }

        /// <summary>
        /// Gets or sets the value for a resource instance attribute
        /// </summary>
        public string ResourceInstanceValue { get; set; }

        /// <summary>
        /// Gets or sets the value for an app instance id
        /// </summary>
        public string AppInstanceIdValue { get; set; }

        /// <summary>
        /// Gets or sets the value for resource party id attribute
        /// </summary>
        public string ResourcePartyValue { get; set; }

        /// <summary>
        /// Gets or sets the value for task attribute
        /// </summary>
        public string TaskValue { get; set; }

        /// <summary>
        /// Gets or sets the value for app resource. 
        /// </summary>
        public string AppResourceValue { get; set; }

        /// <summary>
        /// Gets or sets the resource registry Id
        /// </summary>
        public string ResourceRegistryId { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationNumber for the org owning the resource
        /// </summary>
        public string OrganizationNumber { get; set; }

        /// <summary>
        /// Gets or sets the via-party organization number (Norwegian organization number) through which
        /// client-delegated access should be resolved. Populated from the
        /// <c>urn:altinn:via-party:organization:identifier-no</c> attribute.
        /// </summary>
        public string ViaPartyOrganizationNumber { get; set; }

        /// <summary>
        /// Gets or sets the authorization context mode controlling how client-delegated access is considered
        /// when authorizing the request. Populated from the <c>urn:altinn:authorization:auth-context</c>
        /// attribute (per request, Resource category). Defaults to <see cref="AuthContext.All"/>.
        /// </summary>
        public AuthContext AuthContext { get; set; } = AuthContext.All;

        /// <summary>
        /// Gets or sets a value indicating whether the <c>urn:altinn:authorization:auth-context</c> attribute
        /// was present but could not be parsed into a valid <see cref="Enums.AuthContext"/> value.
        /// </summary>
        public bool HasInvalidAuthContext { get; set; }

        /// <summary>
        /// Gets or sets the ssn for the person owning the resource
        /// </summary>
        public string PersonId { get; set; }

        /// <summary>
        /// Gets or sets the value for resource party uuid attribute
        /// </summary>
        public Guid PartyUuid { get; set; }

        /// <summary>
        /// Gets or sets the value for resource endevent attribute
        /// </summary>
        public string EndEventValue { get; set; }
    }
}
