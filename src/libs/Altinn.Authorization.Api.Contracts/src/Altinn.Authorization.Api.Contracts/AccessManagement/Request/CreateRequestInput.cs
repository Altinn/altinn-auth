namespace Altinn.Authorization.Api.Contracts.AccessManagement.Request;

/// <summary>
/// Base input dto for creating a new request
/// </summary>
public class CreateRequestInput
{
    /// <summary>
    /// Reference to the resource
    /// </summary>
    public RequestRefrenceDto Resource { get; set; }

    /// <summary>
    /// Reference to the access package
    /// </summary>
    public RequestRefrenceDto Package { get; set; }
}

/// <summary>
/// Base input dto for creating a new request
/// </summary>
public class CreateServiceOwnerRequest
{
    /// <summary>
    /// Request connection
    /// </summary>
    public ConnectionRequestInputDto Connection { get; set; }

    /// <summary>
    /// Reference to the resource
    /// </summary>
    public RequestRefrenceDto Resource { get; set; }

    /// <summary>
    /// Reference to the access package
    /// </summary>
    public RequestRefrenceDto Package { get; set; }
}
