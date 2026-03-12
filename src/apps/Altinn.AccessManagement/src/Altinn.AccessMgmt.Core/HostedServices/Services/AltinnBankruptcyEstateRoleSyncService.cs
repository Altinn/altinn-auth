using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.SblBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class AltinnBankruptcyEstateRoleSyncService : BaseSyncService, IAltinnBankruptcyEstateRoleSyncService
    {
        public AltinnBankruptcyEstateRoleSyncService(
            IAltinnSblBridge role,
            IServiceProvider serviceProvider,
            ILogger<AltinnBankruptcyEstateRoleSyncService> logger
        )
        {
            _role = role;
            _serviceProivider = serviceProvider;
            _logger = logger;
        }

        private readonly IAltinnSblBridge _role;
        private readonly ILogger<AltinnBankruptcyEstateRoleSyncService> _logger;
        private readonly IServiceProvider _serviceProivider;

        public async Task SyncBankruptcyEstateRoles(ILease lease, CancellationToken cancellationToken)
        {
            
            var leaseData = await lease.Get<AltinnBankruptcyEstateRoleLease>(cancellationToken);
            var adminDelegations = await _role.StreamRoles("13", leaseData.AltinnBankruptcyEstateRoleStreamNextPageLink, cancellationToken);

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

                        bool alreadyProcessed = await rightImportProgressService.IsImportAlreadyProcessed(item.AltinnRoleDelegationEventId, "Bankruptcy", cancellationToken);
                        if (alreadyProcessed)
                        {
                            continue;
                        }

                        AuditValues values = new AuditValues(
                            item.PerformedByUserUuid ?? SystemEntityConstants.Altinn2RoleImportSystem,
                            SystemEntityConstants.Altinn2RoleImportSystem,
                            batchId.ToString(),
                            item.DelegationChangeDateTime?.ToUniversalTime() ?? DateTime.UtcNow);

                        List<string> packageUrns = GetBankruptcyEstatePackageFromRoleTypeCode(item.RoleTypeCode, cancellationToken);

                        if (packageUrns == null || packageUrns.Count == 0)
                        {
                            ErrorQueue error = new ErrorQueue
                            {
                                DelegationChangeId = item.AltinnRoleDelegationEventId,
                                OriginType = "Bankruptcy",
                                ErrorItem = JsonSerializer.Serialize(item),
                                ErrorMessage = $"No package URNs mapped for delegation FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}. RoleType: {item.RoleTypeCode} Skipping import."
                            };
                            await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                            continue;
                        }

                        if (item.DelegationAction == DelegationAction.Revoke)
                        {
                            // If the action is Revoke, we should delete the assignmentPackages
                            if (item.ToUserPartyUuid == null)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Bankruptcy",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"The delegation is missing ToUserPartyUuid so it is not a valid bankruptcy estate delegation {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
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
                                    OriginType = "Bankruptcy",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"Failed to delete assignmentpackages for FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;
                            }
                        }
                        else
                        {
                            if (item.ToUserPartyUuid == null)
                            {
                                ErrorQueue error = new ErrorQueue
                                {
                                    DelegationChangeId = item.AltinnRoleDelegationEventId,
                                    OriginType = "Bankruptcy",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"The delegation is missing ToUserPartyUuid so it is not a valid admin delegation {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
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
                                    OriginType = "Bankruptcy",
                                    ErrorItem = JsonSerializer.Serialize(item),
                                    ErrorMessage = $"Failed to import delegation for FromParty: {item.FromPartyUuid}, ToParty: {item.ToUserPartyUuid}, PackageUrns: {string.Join(", ", packageUrns)}"
                                };
                                await errorQueueService.AddErrorQueue(error, values, cancellationToken);
                                continue;                                
                            }
                        }

                        await rightImportProgressService.MarkImportAsProcessed(item.AltinnRoleDelegationEventId, "Bankruptcy", values, cancellationToken);
                    }
                }

                if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
                {
                    return;
                }

                await lease.Update<AltinnBankruptcyEstateRoleLease>(d => d.AltinnBankruptcyEstateRoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }
        }

        private List<string> GetBankruptcyEstatePackageFromRoleTypeCode(string roleTypeCode, CancellationToken cancellationToken = default)
        {
            List<string> packages = new List<string>();
            
            switch (roleTypeCode.ToUpper())
            {
                case "BOBEL":
                    packages.Add(PackageConstants.BankruptcyEstateReadAccess.Entity.Urn);
                    break;
                case "BOBES":
                    packages.Add(PackageConstants.BankruptcyEstateWriteAccess.Entity.Urn);
                    break;                
            }

            return packages;
        }
    }
}
