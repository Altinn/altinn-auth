namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

public class RequestReferenceDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Reference identifier
    /// </summary>
    public string ReferenceId { get; set; }

    public bool HasValue()
    {
        return Id.HasValue || !string.IsNullOrEmpty(ReferenceId);
    }
}
