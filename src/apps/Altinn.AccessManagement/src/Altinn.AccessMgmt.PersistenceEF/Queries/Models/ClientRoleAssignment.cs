namespace Altinn.AccessMgmt.PersistenceEF.Queries.Models
{
    public class ClientRoleAssignment
    {
        /// <summary>
        /// The Id for the A2ClientRole, which is the unique identifier for the client role.
        /// </summary>
        public Guid Id { get; set; }
    
        /// <summary>
        /// The FromId for the A2ClientRole, which is the Id of the Client that the role is assigned from.
        /// </summary>
        public Guid FromId { get; set; }

        /// <summary>
        /// The ToId for the A2ClientRole, which is the Id of the receiver of the client role.
        /// </summary>
        public Guid ToId { get; set; }

        /// <summary>
        /// The FacilitatorId for the A2ClientRole, which is the Id of the Service unit that facilitates the role assignment.
        /// </summary>
        public Guid FacilitatorId { get; set; }

        /// <summary>
        /// The PerformedBy for the A2ClientRole, which is the Id of the user who performed the role assignment in A2.
        /// </summary>
        public Guid PerformedBy { get; set; }

        /// <summary>
        /// The RoleCode for the A2ClientRole, which is the code representing the role assigned.
        /// </summary>
        public string RoleCode { get; set; }

        /// <summary>
        /// The CreatedDate for the A2ClientRole, which is the date and time when the role was created.
        /// </summary>
        public DateTimeOffset CreatedDate { get; set; }

        /// <summary>
        /// The AssignmentId for the A2ClientRole, which is the Id of the assignment associated with the role.
        /// </summary>
        public Guid AssignmentId { get; set; }
    }
}
