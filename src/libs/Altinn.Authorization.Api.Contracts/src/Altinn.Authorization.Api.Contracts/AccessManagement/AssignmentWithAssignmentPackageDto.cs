using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    public class AssignmentWithAssignmentPackageDto : AssignmentDto
    {
        public IEnumerable<AssignmentPackageDto> AssignmentPackages { get; set; }

        public AssignmentWithAssignmentPackageDto() 
        {
        }

        public AssignmentWithAssignmentPackageDto(AssignmentDto assignment, IEnumerable<AssignmentPackageDto> assignmentPackages)
        {
            this.Id = assignment.Id;
            this.RoleId = assignment.RoleId;
            this.FromId = assignment.FromId;
            this.ToId = assignment.ToId;
            this.AssignmentPackages = assignmentPackages;
        }
    }
}
