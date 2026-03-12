namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

public class RequestLinks
{
    /// <summary>
    /// Link for the end user to confirm the request (change status from draft to pending)
    /// </summary>
    public string ConfirmLink { get; set; }

    /// <summary>
    /// Link to check status of request
    /// </summary>
    public string StatusLink { get; set; }
}
