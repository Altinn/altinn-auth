namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Number of received requests for a subunit of a party
/// </summary>
public class ReceivedRequestCountSubunitDto
{
    /// <summary>
    /// The subunit
    /// </summary>
    public PartyEntityDto Party { get; set; }

    /// <summary>
    /// Number of requests received by the subunit
    /// </summary>
    public int Count { get; set; }
}
