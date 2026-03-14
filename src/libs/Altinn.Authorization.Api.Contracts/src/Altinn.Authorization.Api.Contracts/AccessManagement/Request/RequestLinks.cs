namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

public class RequestLinks
{
    /// <summary>
    /// Link to request details
    /// </summary>
    public string DetailsLink { get; set; }

    /// <summary>
    /// Link to check status of request
    /// </summary>
    public string StatusLink { get; set; }
}
