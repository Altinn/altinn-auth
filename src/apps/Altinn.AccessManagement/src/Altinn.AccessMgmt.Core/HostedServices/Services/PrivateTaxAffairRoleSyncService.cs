using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.SblBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class PrivateTaxAffairRoleSyncService : BaseSyncService, IPrivateTaxAffairRoleSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateTaxAffairRoleSyncService"/> class.
        /// </summary>
        /// <param name="role">The role service used for streaming roles.</param>
        /// <param name="serviceProvider">object used for creating a scope and fetching a scoped service (IDelegationService) based on this scope</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        public PrivateTaxAffairRoleSyncService(
            IAltinnSblBridge role,
            IServiceProvider serviceProvider,
            ILogger<PrivateTaxAffairRoleSyncService> logger
        )
        {
            _role = role;
            _serviceProivider = serviceProvider;
            _logger = logger;
        }

        private readonly IAltinnSblBridge _role;
        private readonly ILogger<PrivateTaxAffairRoleSyncService> _logger;
        private readonly IServiceProvider _serviceProivider;

        public async Task SyncPrivateTaxAffairRoles(ILease lease, CancellationToken cancellationToken)
        {
            var leaseData = await lease.Get<PrivateTaxAffairRoleLease>(cancellationToken);
            var adminDelegations = await _role.StreamRoles("14", leaseData.PrivateTaxAffairRoleStreamNextPageLink, cancellationToken);

            await foreach (var page in adminDelegations)
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
                _logger.LogInformation("Starting processing role page '{0}'", batchName);

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        await using var scope = _serviceProivider.CreateAsyncScope();
                        IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
                        IRightImportProgressService rightImportProgressService = scope.ServiceProvider.GetRequiredService<IRightImportProgressService>();
                        IErrorQueueService errorQueueService = scope.ServiceProvider.GetRequiredService<IErrorQueueService>();

                        bool alreadyProcessed = await rightImportProgressService.IsImportAlreadyProcessed(item.AltinnRoleDelegationEventId, "Skatteforhold", cancellationToken);
                        if (alreadyProcessed)
                        {
                            continue;
                        }

                        AuditValues values = new AuditValues(
                            item.PerformedByUserUuid ?? item.PerformedByPartyUuid ?? SystemEntityConstants.Altinn2RoleImportSystem,
                            SystemEntityConstants.Altinn2RoleImportSystem,
                            batchId.ToString(),
                            item.DelegationChangeDateTime?.ToUniversalTime() ?? DateTime.UtcNow);

                        List<string> packageUrns = GetPrivateTaxAffairPackageFromRoleTypeCode(item.RoleTypeCode, cancellationToken);

                        if (item.DelegationAction == DelegationAction.Revoke)
                        {
                            // If the action is Revoke, we should delete the assignmentPackages
                            if (item.ToUserPartyUuid == null)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Skatteforhold",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"The delegation is missing ToUserPartyUuid so it is not a valid private tax affair delegation {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }

                            int revokes = await assignmentService.RevokeImportedAssignmentPackages(
                                item.FromPartyUuid,
                                item.ToUserPartyUuid.Value,
                                packageUrns,
                                values,
                                true,
                                cancellationToken);

                            if (revokes == 0)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Skatteforhold",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"Failed to delete assignmentpackages for FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }
                        }
                        else
                        {
                            if (!item.DelegationChangeDateTime.HasValue || item.DelegationChangeDateTime.Value < new DateTimeOffset(2021, 3, 1, 0, 0, 0, new TimeSpan(1, 0, 0)))
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Skatteforhold",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"Skipping privatetaxaffair delegation FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)} since it is before the cut-off date"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }

                            if (item.ToUserPartyUuid == null)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Skatteforhold",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"The delegation is missing ToUserPartyUuid so it is not a valid privatetaxaffair delegation {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }

                            List<AssignmentPackageDto> adds = await assignmentService.ImportAssignmentPackages(item.FromPartyUuid, item.ToUserPartyUuid.Value, packageUrns, values, cancellationToken);
                            if (adds.Count == 0)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Skatteforhold",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"Failed to import delegation for FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }
                        }

                        await rightImportProgressService.MarkImportAsProcessed(item.AltinnRoleDelegationEventId, "Skatteforhold", values, cancellationToken);
                    }
                }

                if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
                {
                    return;
                }

                await lease.Update<PrivateTaxAffairRoleLease>(d => d.PrivateTaxAffairRoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }
        }

        private List<string> GetPrivateTaxAffairPackageFromRoleTypeCode(string roleTypeCode, CancellationToken cancellationToken = default)
        {
            List<string> packages = new List<string>();

            switch (roleTypeCode.ToUpper())
            {
                case "A0282":
                    packages.Add(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Urn);
                    break;
            }

            return packages;
        }
    }
}
