namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

public class CreateRequestDto
{
    /// <summary>
    /// From
    /// </summary>
    public Guid From { get; set; }

    /// <summary>
    /// To
    /// </summary>
    public Guid To { get; set; }

    /// <summary>
    /// Role
    /// </summary>
    public Guid Role { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public RequestStatus Status { get; set; }

    /// <summary>
    /// Resource (opt)
    /// </summary>
    public Guid? Resource { get; set; }

    /// <summary>
    /// Package (opt)
    /// </summary>
    public Guid? Package { get; set; }
}
