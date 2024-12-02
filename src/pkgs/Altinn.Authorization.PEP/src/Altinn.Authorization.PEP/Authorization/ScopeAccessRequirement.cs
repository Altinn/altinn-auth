#nullable enable

using Microsoft.AspNetCore.Authorization;

namespace Altinn.Common.PEP.Authorization
{
    /// <summary>
    /// Requirement for authorization policies used for validating a client scope.
    /// <see href="https://docs.asp.net/en/latest/security/authorization/policies.html"/> for details about authorization
    /// in asp.net core.
    /// </summary>
    public class ScopeAccessRequirement : IScopeAccessRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeAccessRequirement"/> class and 
        /// pupulates the Scope property with the given scope.
        /// </summary>
        /// <param name="scope">The scope for this requirement</param>
        public ScopeAccessRequirement(string scope)
        {
            Scope = new string[] { scope };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeAccessRequirement"/> class with the given scopes.
        /// </summary>
        /// <param name="scopes">The scope for this requirement</param>
        public ScopeAccessRequirement(string[] scopes)
        {
            Scope = scopes;
        }

        /// <summary>
        /// Gets or sets the scope defined for the policy using this requirement
        /// </summary>
        public string[] Scope { get; set; }
    }
}
