namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

public class RequestRefrenceDto
{
    /// <summary>
    /// Uniqueidentifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// URN
    /// </summary>
    public string Urn { get; set; }

    public bool HasValue()
    {
        return Id.HasValue || !string.IsNullOrEmpty(Urn);
    }
}
