namespace Altinn.AccessMgmt.Persistence.Services.Models;

/// <summary>
/// Client connection model for system users
/// </summary>
public class SystemUserClientConnectionDto
{
    /// <summary>
    /// Identity of the client delegation
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The client delegation associated with the client connection
    /// </summary>
    public required ClientDelegation Delegation { get; set; }

    /// <summary>
    /// The client entity of the connection is from
    /// </summary>
    public required Client From { get; set; }

    /// <summary>
    /// The role the agent has for the service provider of the client
    /// </summary>
    public required AgentRole Role { get; set; }

    /// <summary>
    /// The agent entity the client connection is to
    /// </summary>
    public required Agent To { get; set; }

    /// <summary>
    /// The service provider the client connection is related to
    /// </summary>
    public required ServiceProvider Facilitator { get; set; }

    /// <summary>
    /// The role the service provider has for the client
    /// </summary>
    public required ServiceProviderRole FacilitatorRole { get; set; }

    /// <summary>
    /// Delegation of the client connection
    /// </summary>
    public class ClientDelegation
    {
        /// <summary>
        /// The id of the delegation
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The client assignment the delegation is tied to
        /// </summary>
        public Guid FromId { get; set; }

        /// <summary>
        /// The agent assignment the delegation is tied to
        /// </summary>
        public Guid ToId { get; set; }

        /// <summary>
        /// The service provider party id
        /// </summary>
        public Guid FacilitatorId { get; set; }
    }

    /// <summary>
    /// The client entity the client connection
    /// </summary>
    public class Client
    {
        /// <summary>
        /// The party uuid of the client
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type id of the client
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// The variant id of the client
        /// </summary>
        public Guid VariantId { get; set; }

        /// <summary>
        /// The name of the client
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The reference id of the client
        /// </summary>
        public required string RefId { get; set; }

        /// <summary>
        /// The parent party id of the client
        /// </summary>
        public Guid? ParentId { get; set; }
    }

    /// <summary>
    /// The agent role tied to the client connection
    /// </summary>
    public class AgentRole
    {
        /// <summary>
        /// The id of the role
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the role
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The role code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// The description of the role
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Indicates if the role is a key role
        /// </summary>
        public bool IsKeyRole { get; set; }

        /// <summary>
        /// The URN of the role
        /// </summary>
        public required string Urn { get; set; }

        /// <summary>
        /// The legacy role code
        /// </summary>
        public string? LegacyRoleCode { get; set; }

        /// <summary>
        /// The legacy URN of the role
        /// </summary>
        public string? LegacyUrn { get; set; }

        /// <summary>
        /// The provider of the role
        /// </summary>
        public string? Provider { get; set; }
    }

    /// <summary>
    /// The agent entity of the client connection
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// The party uuid of the agent
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type id of the agent
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// The variant id of the agent
        /// </summary>
        public Guid VariantId { get; set; }

        /// <summary>
        /// The name of the agent
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The reference id of the agent
        /// </summary>
        public required string RefId { get; set; }

        /// <summary>
        /// The parent party id of the agent
        /// </summary>
        public Guid? ParentId { get; set; }
    }

    /// <summary>
    /// The service provider entity the client connection is related to
    /// </summary>
    public class ServiceProvider
    {
        /// <summary>
        /// The party uuid of the service provider
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The type id of the service provider
        /// </summary>
        public Guid TypeId { get; set; }

        /// <summary>
        /// The variant id of the service provider
        /// </summary>
        public Guid VariantId { get; set; }

        /// <summary>
        /// The name of the service provider
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The reference id of the service provider
        /// </summary>
        public required string RefId { get; set; }

        /// <summary>
        /// The parent party id of the service provider
        /// </summary>
        public Guid? ParentId { get; set; }
    }

    /// <summary>
    /// The role the service provider has for the client 
    /// </summary>
    public class ServiceProviderRole
    {
        /// <summary>
        /// The id of the role
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the role
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The role code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// The description of the role
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Indicates if the role is a key role
        /// </summary>
        public bool IsKeyRole { get; set; }

        /// <summary>
        /// The URN of the role
        /// </summary>
        public required string Urn { get; set; }

        /// <summary>
        /// The legacy role code
        /// </summary>
        public string? LegacyRoleCode { get; set; }

        /// <summary>
        /// The legacy URN of the role
        /// </summary>
        public string? LegacyUrn { get; set; }

        /// <summary>
        /// The provider of the role
        /// </summary>
        public string? Provider { get; set; }
    }
}
