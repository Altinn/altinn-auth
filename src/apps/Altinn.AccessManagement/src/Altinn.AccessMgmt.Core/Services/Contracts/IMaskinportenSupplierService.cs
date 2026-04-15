using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Interface for managing Maskinporten supplier relationships and scope delegations
/// </summary>
public interface IMaskinportenSupplierService
{
    /// <summary>
    /// Adds a supplier assignment between two organizations for MaskinportenSchema resources
    /// </summary>
    /// <param name="consumerId">The organization granting access (consumer)</param>
    /// <param name="supplierId">The organization receiving access (supplier)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created assignment</returns>
    Task<Result<AssignmentDto>> AddSupplier(Guid consumerId, Guid supplierId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a supplier assignment
    /// </summary>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="supplierId">The supplier organization ID</param>
    /// <param name="cascade">Whether to cascade delete related delegations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation problem if any</returns>
    Task<ValidationProblemInstance> RemoveSupplier(Guid consumerId, Guid supplierId, bool cascade = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all suppliers for a consumer organization
    /// </summary>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="supplierId">Optional supplier filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of suppliers</returns>
    Task<Result<IEnumerable<ConnectionDto>>> GetSuppliers(Guid consumerId, Guid? supplierId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consumers for a supplier organization
    /// </summary>
    /// <param name="supplierId">The supplier organization ID</param>
    /// <param name="consumerId">Optional consumer filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of consumers</returns>
    Task<Result<IEnumerable<ConnectionDto>>> GetConsumers(Guid supplierId, Guid? consumerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets MaskinportenSchema resources delegated to suppliers
    /// </summary>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="supplierId">Optional supplier filter</param>
    /// <param name="resourceId">Optional resource filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resource permissions</returns>
    Task<Result<IEnumerable<ResourcePermissionDto>>> GetSupplierResources(
        Guid consumerId,
        Guid? supplierId = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets MaskinportenSchema resources delegated from consumers
    /// </summary>
    /// <param name="supplierId">The supplier organization ID</param>
    /// <param name="consumerId">Optional consumer filter</param>
    /// <param name="resourceId">Optional resource filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resource permissions</returns>
    Task<Result<IEnumerable<ResourcePermissionDto>>> GetConsumerResources(
        Guid supplierId,
        Guid? consumerId = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a delegation check for a MaskinportenSchema resource
    /// </summary>
    /// <param name="authenticatedUserUuid">UUID of the authenticated user performing the check</param>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="resource">The resource identifier</param>
    /// <param name="languageCode">Language code for translations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource check result</returns>
    Task<Result<ResourceCheckDto>> ResourceDelegationCheck(
        Guid authenticatedUserUuid,
        Guid consumerId,
        string resource,
        string languageCode = "nb",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a MaskinportenSchema resource delegation to a supplier
    /// </summary>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="supplierId">The supplier organization ID</param>
    /// <param name="resource">The resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    Task<Result<bool>> AddResource(
        Guid consumerId,
        Guid supplierId,
        string resource,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a MaskinportenSchema resource delegation from a supplier
    /// </summary>
    /// <param name="consumerId">The consumer organization ID</param>
    /// <param name="supplierId">The supplier organization ID</param>
    /// <param name="resource">The resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation problem if any</returns>
    Task<ValidationProblemInstance> RemoveResource(
        Guid consumerId,
        Guid supplierId,
        string resource,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by organization number
    /// </summary>
    /// <param name="organizationNumber">The organization number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity</returns>
    Task<Result<Entity>> GetEntity(string organizationNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by RefId
    /// </summary>
    /// <param name="resourceRefId">The resource RefId</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resource</returns>
    Task<Result<Resource>> GetResourceByRefId(string resourceRefId, CancellationToken cancellationToken = default);
}
