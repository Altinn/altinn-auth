using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Interface for managing connections.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Get Connections
    /// </summary>
    Task<Result<IEnumerable<ConnectionDto>>> Get(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a role assignment between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the newly created <see cref="Assignment"/>.
    /// </returns>
    Task<Result<AssignmentDto>> AddAssignment(Guid fromId, Guid toId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific role assignment between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="cascade">If <c>false</c>, stop if there are any dependent records.</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemoveAssignment(Guid fromId, Guid toId, bool cascade = false, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the roles associated with a given entity.
    /// </summary>
    /// <param name="party">The user is operating on behalf of.</param>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="configureConnections">A delegate used to configure connection behavior.</param>
    /// <param name="cancellationToken">A token used to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of role permissions.</returns>
    Task<Result<IEnumerable<RolePermissionDto>>> GetRoles(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken);

    /// <summary>
    /// Get connection packages
    /// </summary>
    /// <returns></returns>
    Task<Result<IEnumerable<PackagePermissionDto>>> GetPackages(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to an assignment (by package ID) based on the role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="packageId">Unique identifier of the package to assign.</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created <see cref="AssignmentPackage"/>.
    /// </returns>
    Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a package to an assignment (by package name or code) based on the role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment is made.</param>
    /// <param name="packageUrn">PackageUrn</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the created <see cref="AssignmentPackage"/>.
    /// </returns>
    Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, string packageUrn, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a package (by package ID) from assignment based on a specific role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="package">Package</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string package, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a package (by package name or code) from assignment based on a specific role between two entities.
    /// </summary>
    /// <param name="fromId">ID of the entity from which the assignment originates.</param>
    /// <param name="toId">ID of the entity to which the assignment was made.</param>
    /// <param name="packageId">packageId</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an authpenticated user is an access manager and has the necessary permissions to delegate a specific access package.
    /// </summary>
    /// <param name="party">ID of the person.</param>
    /// <param name="packageIds">Filter param using unique package identifiers.</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<Result<IEnumerable<AccessPackageDto.Check>>> CheckPackage(Guid party, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an authpenticated user is an access manager and has the necessary permissions to delegate a specific access package.
    /// </summary>
    /// <param name="party">ID of the person.</param>
    /// <param name="packages">Filter param using urn package identifiers.</param>
    /// <param name="packageIds">Filter param using unique package identifiers.</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">
    /// Token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="ValidationProblemInstance"/> indicating success or describing any validation errors.
    /// </returns>
    Task<Result<IEnumerable<AccessPackageDto.Check>>> CheckPackage(Guid party, IEnumerable<string> packages, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get connections given from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="toId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<ConnectionDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Connections recived from party
    /// </summary>
    /// <param name="partyId">Filter for party</param>
    /// <param name="fromId">to party</param>
    /// <param name="roleId">Filter for role</param>
    /// <param name="configureConnection">ConnectionOptions</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<IEnumerable<ConnectionDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of packages with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties you have this permission at
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of resources with a list of parties that have this permission
    /// </summary>
    Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all connections to an agent of the given service provider (viaId)
    /// </summary>
    /// <param name="viaId">The uuid of the service provider party</param>
    /// <param name="toId">The uuid of the agent</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>collection of all connections of the agent</returns>
    Task<IEnumerable<SystemUserClientConnectionDto>> GetConnectionsToAgent(Guid viaId, Guid toId, CancellationToken cancellationToken = default);
}
