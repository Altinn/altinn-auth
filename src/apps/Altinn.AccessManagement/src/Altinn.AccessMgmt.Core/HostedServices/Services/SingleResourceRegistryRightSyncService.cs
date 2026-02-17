using System.Text.Json;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.AccessManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class SingleResourceRegistryRightSyncService : BaseSyncService, ISingleResourceRegistryRightSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnAdminRoleSyncService"/> class.
        /// </summary>
        /// <param name="singleRights">The single rights service used for streaming roles.</param>
        /// <param name="serviceProvider">object used for creating a scope and fetching a scoped service (IDelegationService) based on this scope</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        public SingleResourceRegistryRightSyncService(
            IServiceProvider serviceProvider,
            IAltinnAccessManagement singleRights,
            ILogger<SingleAppRightSyncService> logger
        )
        {
            _singleRights = singleRights;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private readonly IAltinnAccessManagement _singleRights;
        private readonly ILogger<SingleAppRightSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public async Task SyncSingleResourceRegistryRights(ILease lease, CancellationToken cancellationToken)
        {
            var leaseData = await lease.Get<SingleAppRightLease>(cancellationToken);
            var singleResourceRightDelegations = await _singleRights.StreamResourceRegistryRightDelegations(leaseData.SingleAppRightStreamNextPageLink, cancellationToken);

            await foreach (var page in singleResourceRightDelegations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!page.IsSuccessful)
                {
                    Log.ResponseError(_logger, page.StatusCode);
                    throw new Exception("Stream page is not successful");
                }

                Guid batchId = Guid.CreateVersion7();
                var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
                _logger.LogInformation("Starting processing resource delegation page '{0}'", batchName);

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        try
                        {
                            await using var scope = _serviceProvider.CreateAsyncScope();
                            IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();                            

                            if (!Guid.TryParse(item.PerformedByUuid, out Guid performedByGuid))
                            {
                                performedByGuid = SystemEntityConstants.SingleRightImportSystem.Id;
                            }

                            AuditValues values = new AuditValues(
                                performedByGuid,
                                SystemEntityConstants.SingleRightImportSystem.Id,
                                batchId.ToString(),
                                item.Created?.ToUniversalTime() ?? DateTime.UtcNow);
                        
                            if (item.DelegationChangeType == AccessManagement.Core.Models.DelegationChangeType.RevokeLast)
                            {
                                int revokes = await assignmentService.RevokeImportedAssignmentResource(
                                    item.FromUuid.Value,
                                    item.ToUuid.Value,
                                    item.ResourceId,
                                    values,
                                    cancellationToken);

                                if (revokes == 0)
                                {
                                    _logger.LogWarning(
                                        "Failed to delete assignmentresource for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}",
                                        item.FromUuid,
                                        item.ToUuid,
                                        item.ResourceId);
                                }
                            }
                            else
                            {
                                int adds = await assignmentService.ImportAssignmentResourceChange(
                                    item.FromUuid.Value,
                                    item.ToUuid.Value,
                                    item.ResourceId,
                                    item.BlobStoragePolicyPath,
                                    item.BlobStorageVersionId,
                                    item.ResourceRegistryDelegationChangeId,
                                    values,
                                    cancellationToken);

                                if (adds == 0)
                                {
                                    _logger.LogWarning(
                                        "Failed to import delegation for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}",
                                        item.FromUuid,
                                        item.ToUuid,
                                        item.ResourceId);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            bool addToErrorQueue = CheckIfErrorShouldBePushedToErrorQueue(ex, item, cancellationToken);

                            if (addToErrorQueue)
                            {
                                // Log and continue processing other items
                                await using var scope = _serviceProvider.CreateAsyncScope();
                                IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.ResourceRegistryDelegationChangeId,
                                    OriginType = "ResourceRegistry",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = ex.InnerException is null ? ex.Message : ex.InnerException.Message
                                };

                                AuditValues values = new AuditValues(
                                SystemEntityConstants.SingleRightImportSystem.Id,
                                SystemEntityConstants.SingleRightImportSystem.Id,
                                batchId.ToString(),
                                DateTime.UtcNow);

                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                            }
                            else
                            {
                                _logger.LogError(ex, "Error processing single resource registry right delegation from {FromParty} to {ToParty} for resource {ResourceId}", item.FromUuid, item.ToUuid, item.ResourceId);
                                throw;
                            }                                
                        }
                    }
                }

                if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
                {
                    return;
                }

                await lease.Update<SingleAppRightLease>(d => d.SingleAppRightStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }            
        }

        public async Task SyncFailedSingleResourceRegistryRights(CancellationToken cancellationToken)
        {
            Guid batchId = Guid.CreateVersion7();

            await using var scope = _serviceProvider.CreateAsyncScope();
            IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

            var items = await errorQueueService.RetrieveItemsForReProcessing("ResourceRegistry", cancellationToken);

            foreach (var item in items)
            {
                try
                {
                    IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();

                    var element = JsonSerializer.Deserialize<DelegationChange>(item.ErrorItem);

                    if (!Guid.TryParse(element.PerformedByUuid, out Guid performedByGuid))
                    {
                        performedByGuid = SystemEntityConstants.SingleRightImportSystem.Id;
                    }

                    AuditValues values = new AuditValues(
                        performedByGuid,
                        SystemEntityConstants.SingleRightImportSystem.Id,
                        batchId.ToString(),
                        element.Created?.ToUniversalTime() ?? DateTime.UtcNow);

                    if (element.DelegationChangeType == AccessManagement.Core.Models.DelegationChangeType.RevokeLast)
                    {
                        int revokes = await assignmentService.RevokeImportedAssignmentResource(
                            element.FromUuid.Value,
                            element.ToUuid.Value,
                            element.ResourceId,
                            values,
                            cancellationToken);

                        if (revokes == 0)
                        {
                            _logger.LogWarning(
                                "Failed to delete assignmentresource for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}",
                                element.FromUuid,
                                element.ToUuid,
                                element.ResourceId);
                        }
                    }
                    else
                    {
                        int adds = await assignmentService.ImportAssignmentResourceChange(
                            element.FromUuid.Value,
                            element.ToUuid.Value,
                            element.ResourceId,
                            element.BlobStoragePolicyPath,
                            element.BlobStorageVersionId,
                            element.ResourceRegistryDelegationChangeId,
                            values,
                            cancellationToken);

                        if (adds == 0)
                        {
                            _logger.LogWarning(
                                "Failed to import delegation for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}",
                                element.FromUuid,
                                element.ToUuid,
                                element.ResourceId);
                        }
                    }

                    var result = errorQueueService.MarkErrorQueueElementProcessed(item.Id, values, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }                
            }
        }

        private bool CheckIfErrorShouldBePushedToErrorQueue(Exception ex, DelegationChange item, CancellationToken cancellationToken)
        {
            if (ex.InnerException != null && ex.InnerException.Message.StartsWith("23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_toid\"", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException != null && ex.InnerException.Message.StartsWith("23503: insert or update on table \"assignment\" violates foreign key constraint \"fk_assignment_entity_fromid\"", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
