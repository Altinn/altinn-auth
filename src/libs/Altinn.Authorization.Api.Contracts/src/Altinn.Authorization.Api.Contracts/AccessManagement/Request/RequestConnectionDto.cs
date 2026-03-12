namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Definition of connection from one party to another
/// </summary>
public class RequestConnectionDto
{
    /// <summary>
    /// Party that is requested to grant access
    /// </summary>
    public PartyEntityDto From { get; set; }

    /// <summary>
    /// Party that access is requested for
    /// </summary>
    public PartyEntityDto To { get; set; }
}
