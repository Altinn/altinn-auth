using System.Collections.Generic;
using Altinn.Platform.Authorization.Models.Oed;

namespace Altinn.Platform.Authorization.Models
{
    /// <summary>
    /// Model for a list of OED/Digitalt dødsbo role assignment
    /// </summary>
    public class OedRoleAssignments
    {
        /// <summary>
        /// The list of OED/Digitalt dødsbo role assignments
        /// </summary>
        public List<OedRoleAssignment> RoleAssignments { get; set; }
    }
}