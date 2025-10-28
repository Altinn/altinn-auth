namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public class ImportClientDelegationRequestDto
    {
        /// <summary>
        /// Client party uuid
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// Agent party uuid
        /// </summary>
        public Guid AgentId { get; set; }

        /// <summary>
        /// Agent role (Facilitator -> Agent)
        /// e.g Agent
        /// </summary>
        public string AgentRole { get; set; } = string.Empty;

        /// <summary>
        /// The Service unit that performed the delegation
        /// </summary>
        public Guid? Facilitator { get; set; } = null;

        /// <summary>
        /// Packages to be delegated to Agent
        /// </summary>
        public List<ImportClientDelegationRolePackageDto> RolePackages { get; set; } = [];
    }

    /// <summary>
    /// Role and packages
    /// </summary>
    public class ImportClientDelegationRolePackageDto
    {
        /// <summary>
        /// REGN, REVI
        /// The Role the Client has delegated to the Facilitator, 
        /// providing the AccessPackage,
        /// through which the faciliator now wants to further Delegate
        /// to the Agent SystemUser.
        /// </summary>
        public required string RoleIdentifier { get; set; }

        /// <summary>
        /// The AccessPackage is a child of one or more Roles, 
        /// and contains one or several Rights.    
        /// This field uses the urn notation, such as:
        /// urn:altinn:accesspackage:ansvarlig-revisor
        /// </summary>
        public required string PackageUrn { get; set; }        
    }
}
