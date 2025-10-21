namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Assignment resource 
/// </summary>
public class AssignmentResourceDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the resource assignment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Assignment identity
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Resource identifier
    /// </summary>
    public Guid ResourceId { get; set; }
}
