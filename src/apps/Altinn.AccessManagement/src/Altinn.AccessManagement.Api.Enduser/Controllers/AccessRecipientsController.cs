using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for end user api operations for recipients which have received access from the user or it's organizations.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/access/recipients")]
public class AccessRecipientsController : ControllerBase
{
    private readonly ILogger<AccessRecipientsController> _logger;
    private readonly IEndUserAuthorizationService _endUserAuthorizationService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger instance to log information</param>
    /// <param name="endUserAuthorizationService">Service to handle end user authorization</param>
    public AccessRecipientsController(ILogger<AccessRecipientsController> logger, IEndUserAuthorizationService endUserAuthorizationService)
    {
        _logger = logger;
        _endUserAuthorizationService = endUserAuthorizationService;
    }

    /// <summary>
    /// Get access recipients
    /// </summary>
    /// <param name="party">The party identifier</param>
    /// <param name="from">The identifier of the party from which access is given</param>
    /// <param name="to">The identifier of the party to which access is given</param>
    /// <returns>List of access recipients</returns>
    [HttpGet]
    [Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ_WITH_PASS_TROUGH)]
    public async Task<ActionResult> GetAccessRecipients([FromQuery] Guid party, [FromQuery] Guid from, [FromQuery] Guid to)
    {
        _logger.LogInformation("Debug: Get access recipients triggered");

        if (HttpContext.Items.FirstOrDefault(i => i.Key.Equals("HasRequestedPermission")).Value is not bool result)
        {
            return await Task.FromResult(Forbid());
        }

        bool hasReadAccess = false;

        if (!result)
        {
            hasReadAccess = await _endUserAuthorizationService.HasPartyInAuthorizedParties(party, from, to);
        }

        return await Task.FromResult(Ok(result || hasReadAccess));
    }
}
