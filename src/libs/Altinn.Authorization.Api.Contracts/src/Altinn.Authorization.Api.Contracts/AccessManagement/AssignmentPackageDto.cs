namespace Altinn.Authorization.Api.Contracts.AccessManagement;

public class AssignmentPackageDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Assignment identity
    /// </summary>
    public Guid AssignmentId { get; set; }

    /// <summary>
    /// Package identifier
    /// </summary>
    public Guid PackageId { get; set; }
}

