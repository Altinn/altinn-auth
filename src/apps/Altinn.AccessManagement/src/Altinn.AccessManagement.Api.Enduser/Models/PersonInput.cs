using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Altinn.AccessManagement.Api.Enduser.Models;

/// <summary>
/// Input for connection controller.
/// </summary>
public class PersonInput
{
    /// <summary>
    /// The person identifier.
    /// </summary>
    [SwaggerSchema(Description = "Person identifier", Format = "string")]
    public string PersonIdentifier { get; set; }

    /// <summary>
    /// The last name of the person.
    /// </summary>
    [SwaggerSchema(Description = "Lastname", Format = "string")]
    public string LastName { get; set; }
}
