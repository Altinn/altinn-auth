namespace Altinn.Authorization.Api.Contracts.AccessManagement;

public class ConnectionRequestInputDto
{
    /// <summary>
    /// Urn describing the party
    /// </summary>
    public string From { get; set; }

    /// <summary>
    /// Urn describing the party
    /// </summary>
    public string To { get; set; }
}
