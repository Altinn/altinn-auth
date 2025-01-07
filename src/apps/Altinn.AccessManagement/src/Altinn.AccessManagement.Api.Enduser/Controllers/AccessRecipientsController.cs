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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public AccessRecipientsController(ILogger<AccessRecipientsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get access recipients
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> GetAccessRecipients()
    {
        _logger.LogInformation("Debug: Get access recipients triggered");
        return await Task.FromResult(Ok());
    }
}
