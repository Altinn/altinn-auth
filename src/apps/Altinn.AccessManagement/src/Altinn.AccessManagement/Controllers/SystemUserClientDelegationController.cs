using Altinn.AccessManagement.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller for internal api operations for system user client delegation.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/internal/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
public class SystemUserClientDelegationController : ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemUserClientDelegationController"/> class.
    /// </summary>
    public SystemUserClientDelegationController()
    {
    }

    /// <summary>
    /// Post client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="client">The client the authenticated user is delegating access from</param>
    /// <param name="systemUser">The system user the authenticated user is delegating access to</param>
    /// <param name="package">The package the authenticated user is delegating access to</param>
    [HttpPost]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> PostClientDelegation([FromQuery] Guid party, [FromQuery] Guid client, [FromQuery] Guid systemUser, [FromQuery] string package)
    {
        return await Task.FromResult(Ok());
    }

    /// <summary>
    /// Delete client delegation
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="client">The client the authenticated user is removing access from</param>
    /// <param name="systemUser">The system user the authenticated user is removing client access to</param>
    [HttpDelete]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_WRITE)]
    public async Task<ActionResult> DeleteClientDelegation([FromQuery] Guid party, [FromQuery] Guid client, [FromQuery] Guid systemUser)
    {
        return await Task.FromResult(Ok());
    }

    /// <summary>
    /// Gets all client delegations for a given system user
    /// </summary>
    /// <param name="party">The party the authenticated user is performing client administration on behalf of</param>
    /// <param name="systemUser">The system user the authenticated user is delegating access to</param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_CLIENTDELEGATION_READ)]
    public async Task<ActionResult> GetClientDelegations([FromQuery] Guid party, [FromQuery] Guid systemUser)
    {
        return await Task.FromResult(Ok());
    }
}
