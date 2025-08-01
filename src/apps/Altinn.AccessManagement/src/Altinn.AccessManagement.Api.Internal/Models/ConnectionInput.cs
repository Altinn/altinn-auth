using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.AccessManagement.Api.Internal.Models;

/// <summary>
/// Input for connection controller.
/// </summary>
public class ConnectionInput
{
    /// <summary>
    /// making request on behalf of.
    /// </summary>
    [FromQuery(Name = "party")]
    [SwaggerSchema(Description = "party")]
    public Guid Party { get; set; }

    /// <summary>
    /// to party
    /// </summary>
    [FromQuery(Name = "to")]
    [SwaggerSchema(Description = "to")]
    public Guid To { get; set; }
}
