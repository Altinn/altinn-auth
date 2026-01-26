using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn.Authorization.ABAC.Xacml
{
    /// <summary>
    /// The <Decision /> element contains the result of policy evaluation.
    ///
    /// The <Decision /> element is of DecisionType simple type.
    /// The values of the<Decision /> element have the following meanings:
    /// “Permit”: the requested access is permitted
    /// “Deny”: the requested access is denied.
    /// “Indeterminate”: the PDP is unable to evaluate the requested access.  Reasons for such inability include: missing attributes, network errors while
    /// retrieving policies, division by zero during policy evaluation, syntax errors in the decision request or in the policy, etc.
    /// “NotApplicable”: the PDP does not have any policy that applies to this decision request.
    /// </summary>
    /// <remarks>
    /// The numercial values are in use. Do not change them.
    /// </remarks>
    public enum XacmlContextDecision
    {
        /// <summary>
        /// “Permit”: the requested access is permitted
        /// </summary>
        Permit = 0,

        /// <summary>
        /// “Deny”: the requested access is denied.
        /// </summary>
        Deny = 1,

        /// <summary>
        /// “Indeterminate”: the PDP is unable to evaluate the requested access.  Reasons for such inability include: missing attributes, network errors while retrieving policies, division by zero during policy evaluation, syntax errors in the decision request or in the policy, etc.
        /// </summary>
        Indeterminate = 2,

        /// <summary>
        ///  “NotApplicable”: the PDP does not have any policy that applies to this decision request.
        /// </summary>
        NotApplicable = 3,
    }
}
