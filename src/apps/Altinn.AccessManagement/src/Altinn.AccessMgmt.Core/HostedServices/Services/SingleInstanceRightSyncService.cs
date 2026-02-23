using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.AccessManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class SingleInstanceRightSyncService : BaseSyncService, ISingleInstanceRightSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnAdminRoleSyncService"/> class.
        /// </summary>
        /// <param name="singleRights">The single rights service used for streaming roles.</param>
        /// <param name="serviceProvider">object used for creating a scope and fetching a scoped service (IDelegationService) based on this scope</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        public SingleInstanceRightSyncService(
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

        public async Task SyncSingleInstanceRights(ILease lease, CancellationToken cancellationToken)
        {
            var leaseData = await lease.Get<SingleInstanceRightLease>(cancellationToken);
            var singleInstanceRightDelegations = await _singleRights.StreamInstanceRightDelegations(leaseData.SingleInstanceRightStreamNextPageLink, cancellationToken);

            await foreach (var page in singleInstanceRightDelegations)
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
                _logger.LogInformation("Starting processing instance delegation page '{0}'", batchName);

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        try
                        {
                            await using var scope = _serviceProvider.CreateAsyncScope();
                            IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
                            IRightImportProgressService rightImportProgressService = scope.ServiceProvider.GetRequiredService<IRightImportProgressService>();

                            bool alreadyProcessed = await rightImportProgressService.IsImportAlreadyProcessed(item.InstanceDelegationChangeId, "Instance", cancellationToken);
                            if (alreadyProcessed)
                            {
                                continue;
                            }

                            if (!Guid.TryParse(item.PerformedBy, out Guid performedByGuid))
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
                                int revokes = await assignmentService.RevokeImportedInstanceAssignment(
                                    item.FromUuid,
                                    item.ToUuid,
                                    item.ResourceId,
                                    item.InstanceId,
                                    values,
                                    cancellationToken);

                                if (revokes == 0)
                                {
                                    _logger.LogWarning(
                                        "Failed to delete assignmentresource for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}, Instance: {instance}",
                                        item.FromUuid,
                                        item.ToUuid,
                                        item.ResourceId,
                                        item.InstanceId);
                                }
                            }
                            else
                            {
                                int adds = await assignmentService.ImportInstanceAssignmentChange(
                                    item.FromUuid,
                                    item.ToUuid,
                                    item.ResourceId,
                                    item.BlobStoragePolicyPath,
                                    item.BlobStorageVersionId,
                                    item.InstanceId,
                                    item.InstanceDelegationChangeId,
                                    values,
                                    cancellationToken);

                                if (adds == 0)
                                {
                                    _logger.LogWarning(
                                        "Failed to import delegation for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}, Instance: {instance}",
                                        item.FromUuid,
                                        item.ToUuid,
                                        item.ResourceId,
                                        item.InstanceId);
                                }
                            }

                            await rightImportProgressService.MarkImportAsProcessed(item.InstanceDelegationChangeId, "Instance", values, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception ex)
                        {
                            bool addToErrorQueue = DelegationCheckHelper.CheckIfErrorShouldBePushedToErrorQueue(ex);

                            if (addToErrorQueue)
                            {
                                // Log and continue processing other items
                                await using var scope = _serviceProvider.CreateAsyncScope();
                                IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.InstanceDelegationChangeId,
                                    OriginType = "Instance",
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

                await lease.Update<SingleInstanceRightLease>(d => d.SingleInstanceRightStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }
        }

        public async Task SyncFailedSingleInstanceRights(CancellationToken cancellationToken)
        {
            Guid batchId = Guid.CreateVersion7();

            await using var scope = _serviceProvider.CreateAsyncScope();
            IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

            var items = await errorQueueService.RetrieveItemsForReProcessing("Instance", cancellationToken);

            foreach (var item in items)
            {
                try
                {
                    IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();

                    var element = JsonSerializer.Deserialize<InstanceDelegationChange>(item.ErrorItem);

                    if (!Guid.TryParse(element.PerformedBy, out Guid performedByGuid))
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
                        int revokes = await assignmentService.RevokeImportedInstanceAssignment(
                            element.FromUuid,
                            element.ToUuid,
                            element.ResourceId,
                            element.InstanceId,
                            values,
                            cancellationToken);

                        if (revokes == 0)
                        {
                            _logger.LogWarning(
                                "Failed to delete assignmentresource for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}, Instance: {instance}",
                                element.FromUuid,
                                element.ToUuid,
                                element.ResourceId,
                                element.InstanceId);
                        }
                    }
                    else
                    {
                        int adds = await assignmentService.ImportInstanceAssignmentChange(
                            element.FromUuid,
                            element.ToUuid,
                            element.ResourceId,
                            element.BlobStoragePolicyPath,
                            element.BlobStorageVersionId,
                            element.InstanceId,
                            element.InstanceDelegationChangeId,
                            values,
                            cancellationToken);

                        if (adds == 0)
                        {
                            _logger.LogWarning(
                                "Failed to import delegation for FromParty: {FromParty}, ToParty: {ToParty}, Resource: {resource}, Instance: {instance}",
                                element.FromUuid,
                                element.ToUuid,
                                element.ResourceId,
                                element.InstanceId);
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
    }
}
