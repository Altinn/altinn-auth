using Microsoft.AspNetCore.Mvc;

namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Properties for querying requests (service owner)
/// </summary>
public class RequestServiceOwnerQuery
{
    /// <summary>
    /// Urn describing the party
    /// </summary>
    [FromQuery(Name = "from")]
    public string From { get; set; }

    /// <summary>
    /// Urn describing the party
    /// </summary>
    [FromQuery(Name = "to")]
    public string To { get; set; }
}
