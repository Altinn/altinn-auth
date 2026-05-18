using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
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
                    throw new InvalidOperationException("Stream page is not successful");
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
                            IAMPartyService partyService = scope.ServiceProvider.GetRequiredService<IAMPartyService>();
                            IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

                            bool alreadyProcessed = await rightImportProgressService.IsImportAlreadyProcessed(item.InstanceDelegationChangeId, "Instance", cancellationToken);
                            if (alreadyProcessed)
                            {
                                continue;
                            }

                            if (!Guid.TryParse(item.PerformedBy, out Guid performedByGuid))
                            {
                                performedByGuid = SystemEntityConstants.InstanceRightImportSystem.Id;
                            }

                            AuditValues values = new AuditValues(
                                performedByGuid,
                                SystemEntityConstants.InstanceRightImportSystem.Id,
                                item.PerformedBy,
                                item.Created?.ToUniversalTime() ?? DateTime.UtcNow);

                            var party = await partyService.GetByUid(item.FromUuid, cancellationToken);

                            if (party == null)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.InstanceDelegationChangeId,
                                    OriginType = "Instance",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"From party not found for uuid: {item.FromUuid}"
                                };

                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }

                            if (item.DelegationChangeType == DelegationChangeType.RevokeLast)
                            {
                                int revokes = await assignmentService.RevokeImportedInstanceAssignment(
                                    item.FromUuid,
                                    item.ToUuid,
                                    item.ResourceId.ToLower(),
                                    CreateInstanceUrnFromInstanceIdAndPartyId(item.InstanceId, party.PartyId),
                                    values,
                                    cancellationToken);

                                if (revokes == 0)
                                {
                                    ErrorQueue error = new ErrorQueue
                                    {
                                        DelegationChangeId = item.InstanceDelegationChangeId,
                                        OriginType = "Instance",
                                        ErrorItem = JsonSerializer.Serialize(item),
                                        ErrorMessage = $"Failed to delete assignmentresource for FromParty: {item.FromUuid}, ToParty: {item.ToUuid}, Resource: {item.ResourceId}, Instance: {item.InstanceId}"
                                    };

                                    await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                    continue;
                                }
                            }
                            else
                            {
                                int adds = await assignmentService.ImportInstanceAssignmentChange(
                                    item.FromUuid,
                                    item.ToUuid,
                                    item.ResourceId.ToLower(),
                                    item.BlobStoragePolicyPath,
                                    item.BlobStorageVersionId,
                                    CreateInstanceUrnFromInstanceIdAndPartyId(item.InstanceId, party.PartyId),
                                    item.InstanceDelegationChangeId,
                                    values,
                                    party.PartyId,
                                    cancellationToken);

                                if (adds == 0)
                                {
                                    ErrorQueue error = new ErrorQueue
                                    {
                                        DelegationChangeId = item.InstanceDelegationChangeId,
                                        OriginType = "Instance",
                                        ErrorItem = JsonSerializer.Serialize(item),
                                        ErrorMessage = $"Failed to import delegation for FromParty: {item.FromUuid}, ToParty: {item.ToUuid}, Resource: {item.ResourceId}, Instance: {item.InstanceId}"
                                    };

                                    await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                    continue;
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
                                SystemEntityConstants.InstanceRightImportSystem.Id,
                                SystemEntityConstants.InstanceRightImportSystem.Id,
                                batchId.ToString(),
                                DateTime.UtcNow);

                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                            }
                            else
                            {
                                _logger.LogError(ex, "Error processing single resource registry right delegation from {FromParty} to {ToParty} for resource {ResourceId} instance {InstanceId}", item.FromUuid, item.ToUuid, item.ResourceId, item.InstanceId);
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
            AuditValues values = null;

            foreach (var item in items)
            {
                try
                {
                    IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
                    IAMPartyService partyService = scope.ServiceProvider.GetRequiredService<IAMPartyService>();

                    var element = JsonSerializer.Deserialize<InstanceDelegationChange>(item.ErrorItem);

                    if (!Guid.TryParse(element.PerformedBy, out Guid performedByGuid))
                    {
                        performedByGuid = SystemEntityConstants.InstanceRightImportSystem.Id;
                    }

                    values = new AuditValues(
                        performedByGuid,
                        SystemEntityConstants.InstanceRightImportSystem.Id,
                        batchId.ToString(),
                        element.Created?.ToUniversalTime() ?? DateTime.UtcNow);

                    var party = await partyService.GetByUid(element.FromUuid, cancellationToken);

                    if (party == null)
                    {
                        await errorQueueService.UpdateErrorMessage(item.Id, values, $"From party not found for uuid: {element.FromUuid}", cancellationToken);
                        continue;
                    }

                    if (element.DelegationChangeType == DelegationChangeType.RevokeLast)
                    {
                        int revokes = await assignmentService.RevokeImportedInstanceAssignment(
                            element.FromUuid,
                            element.ToUuid,
                            element.ResourceId.ToLower(),
                            CreateInstanceUrnFromInstanceIdAndPartyId(element.InstanceId, party.PartyId),
                            values,
                            cancellationToken);

                        if (revokes == 0)
                        {
                            await errorQueueService.UpdateErrorMessage(item.Id, values, $"Failed to delete assignmentresource for FromParty: {element.FromUuid}, ToParty: {element.ToUuid}, Resource: {element.ResourceId}, Instance: {element.InstanceId}", cancellationToken);
                            continue;
                        }
                    }
                    else
                    {
                        int adds = await assignmentService.ImportInstanceAssignmentChange(
                            element.FromUuid,
                            element.ToUuid,
                            element.ResourceId.ToLower(),
                            element.BlobStoragePolicyPath,
                            element.BlobStorageVersionId,
                            CreateInstanceUrnFromInstanceIdAndPartyId(element.InstanceId, party.PartyId),
                            element.InstanceDelegationChangeId,
                            values,
                            party.PartyId,
                            cancellationToken);

                        if (adds == 0)
                        {
                            await errorQueueService.UpdateErrorMessage(item.Id, values, $"Failed to import delegation for FromParty: {element.FromUuid}, ToParty: {element.ToUuid}, Resource: {element.ResourceId}, Instance: {element.InstanceId}", cancellationToken);
                            continue;
                        }
                    }

                    await errorQueueService.MarkErrorQueueElementProcessed(item.Id, values, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException is null ? ex.Message : ex.InnerException.Message;
                    await errorQueueService.UpdateErrorMessage(item.Id, values, errorMessage, cancellationToken);
                }
            }
        }

        private static string CreateInstanceUrnFromInstanceIdAndPartyId(string instanceId, int partyid) => $"{AltinnXacmlConstants.MatchAttributeIdentifiers.InstanceAttribute}:{partyid}/{instanceId.ToLower()}";
    }
}
