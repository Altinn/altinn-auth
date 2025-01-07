using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for end user api operations for parties which have provided access to the user or it's organizations.
/// </summary>
[ApiController]
[Route("accessmanagement/api/v1/enduser/access/parties")]
public class AccessPartiesController : ControllerBase
{
    private readonly ILogger<AccessPartiesController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public AccessPartiesController(ILogger<AccessPartiesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get access parties
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult> GetAccessParties()
    {
        _logger.LogInformation("Debug: Get access parties triggered");
        return await Task.FromResult(Ok());
    }
}
