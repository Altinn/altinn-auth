#nullable enable

using Microsoft.AspNetCore.Authorization;

namespace Altinn.Common.PEP.Authorization;

/// <summary>
/// This interface describes the implementation of a scope access requirement in policy based authorization
/// </summary>
public interface IScopeAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets or sets the scope defined for the policy using this requirement
    /// </summary>
    string[] Scope { get; set; }
}
