using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessManagement.Core.Models
{
    /// <summary>
    /// Result of a consent delegation check, containing the actions the user is authorized to delegate.
    /// </summary>
    public class ConsentDelegationCheckResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the delegation check was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the list of action URNs (e.g. "urn:oasis:names:tc:xacml:1.0:action:action-id:read") 
        /// that the authenticated user is authorized to delegate on behalf of the party.
        /// </summary>
        public IEnumerable<string> DelegatableActions { get; set; } = [];
    }
}
