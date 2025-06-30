namespace Altinn.Common.PEP.Constants
{
    /// <summary>
    /// Represents a collection of URN values for different Altinn specific XACML attributes.
    /// </summary>
    public static class AltinnXacmlUrns
    {
        /// <summary>
        /// Get the URN value for party id.
        /// </summary>
        public const string PartyId = "urn:altinn:partyid";

        /// <summary>
        /// xacml string that represents party uuid
        /// </summary>
        public const string PartyUuidAttribute = "urn:altinn:party:uuid";

        /// <summary>
        /// Get the URN value for person. Only supported for use in external Authorize API
        /// </summary>
        public const string PersonId = "urn:altinn:person:identifier-no";

        /// <summary>
        /// Get the URN value for organization number. This is the legacy version 
        /// </summary>
        public const string OrganizationNumber = "urn:altinn:organizationnumber";

        /// <summary>
        /// xacml string that represents organization number 
        /// </summary>
        public const string OrganizationNumberAttribute = "urn:altinn:organization:identifier-no";

        /// <summary>
        /// Get the URN value for instance id
        /// </summary>
        public const string InstanceId = "urn:altinn:instance-id";

        /// <summary>
        /// Get the URN value for org (application owner)
        /// </summary>
        public const string OrgId = "urn:altinn:org";

        /// <summary>
        /// Get the URN value for app id
        /// </summary>
        public const string AppId = "urn:altinn:app";

        /// <summary>
        /// Get the URN value for app resource
        /// </summary>
        public const string AppResource = "urn:altinn:appresource";

        /// <summary>
        /// Get the value for for event id
        /// </summary>
        public const string EventId = "urn:altinn:event-id";

        /// <summary>
        /// Get the value task id
        /// </summary>
        public const string TaskId = "urn:altinn:task";

        /// <summary>
        /// Get the value resourceId
        /// </summary>
        public const string ResourceId = "urn:altinn:resource";

        /// <summary>
        /// Get the value Resource Instance
        /// </summary>
        public const string ResourceInstance = "urn:altinn:resource:instance-id";

        /// <summary>
        /// Get the value eventType
        /// </summary>
        public const string EventType = "urn:altinn:eventtype";

        /// <summary>
        /// Get the value EventSource
        /// </summary>
        public const string EventSource = "urn:altinn:eventsource";

        /// <summary>
        /// Get the value scope
        /// </summary>
        public const string Scope = "urn:scope";

        /// <summary>
        /// Get the value sessionid
        /// </summary>
        public const string SessionId = "urn:altinn:sessionid";

        /// <summary>
        /// SystemUserUuid urn
        /// </summary>
        public const string SystemUserUuid = "urn:altinn:systemuser:uuid";

        /// <summary>
        /// xacml string that represents user
        /// </summary>
        public const string UserAttribute = "urn:altinn:userid";

        /// <summary>
        /// xacml string that represents person universally unique identifier
        /// </summary>
        public const string PersonUuidAttribute = "urn:altinn:person:uuid";
    }
}
