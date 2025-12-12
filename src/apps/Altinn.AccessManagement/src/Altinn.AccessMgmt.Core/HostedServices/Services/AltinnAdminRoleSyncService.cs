using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.SblBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class AltinnAdminRoleSyncService : BaseSyncService, IAltinnAdminRoleSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnAdminRoleSyncService"/> class.
        /// </summary>
        /// <param name="role">The role service used for streaming roles.</param>
        /// <param name="serviceProvider">object used for creating a scope and fetching a scoped service (IDelegationService) based on this scope</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        public AltinnAdminRoleSyncService(
            IAltinnSblBridge role,
            IServiceProvider serviceProvider,
            ILogger<AltinnClientRoleSyncService> logger
        )
        {
            _role = role;
            _serviceProivider = serviceProvider;
            _logger = logger;
        }

        private readonly IAltinnSblBridge _role;
        private readonly ILogger<AltinnClientRoleSyncService> _logger;
        private readonly IServiceProvider _serviceProivider;

        public async Task SyncAdminRoles(ILease lease, CancellationToken cancellationToken)
        {
            var leaseData = await lease.Get<AltinnAdminRoleLease>(cancellationToken);
            var adminDelegations = await _role.StreamRoles("11", leaseData.AltinnAdminRoleStreamNextPageLink, cancellationToken);

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
                _logger.LogInformation("Starting proccessing role page '{0}'", batchName);

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        // Do not process admin roles for EC-Users
                        if (item.ToUserType == UserType.EnterpriseIdentified)
                        {
                            continue;
                        }

                        await using var scope = _serviceProivider.CreateAsyncScope();
                        IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();

                        AuditValues values = new AuditValues(
                            item.PerformedByUserUuid ?? SystemEntityConstants.Altinn2RoleImportSystem,
                            SystemEntityConstants.Altinn2RoleImportSystem,
                            batchId.ToString(),
                            item.DelegationChangeDateTime?.ToUniversalTime() ?? DateTime.UtcNow);

                        List<string> packageUrns = GetAdminPackageFromRoleTypeCode(item.RoleTypeCode, cancellationToken);

                        if (item.DelegationAction == DelegationAction.Revoke)
                        {
                            // If the action is Revoke, we should delete the assignmentPackages
                            if (item.ToUserPartyUuid == null)
                            {
                                _logger.LogWarning(
                                    "The delegation is missing ToUserPartyUuid so it is not a valid admin delegation {FromParty}, ToParty: {ToParty}, PackageUrns: {PackageUrn}",
                                    item.FromPartyUuid,
                                    item.ToUserPartyUuid,
                                    string.Join(", ", packageUrns));
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
                                _logger.LogWarning(
                                    "Failed to delete assignmentpackages for FromParty: {FromParty}, ToParty: {ToParty}, PackageUrns: {packageUrn}",
                                    item.FromPartyUuid,
                                    item.ToUserPartyUuid,
                                    string.Join(", ", packageUrns));
                            }
                        }
                        else
                        {
                            if (item.ToUserPartyUuid == null)
                            {
                                _logger.LogWarning(
                                    "The delegation is missing ToUserPartyUuid so it is not a valid admin delegation {FromParty}, ToParty: {ToParty}, PackageUrns: {PackageUrn}",
                                    item.FromPartyUuid,
                                    item.ToUserPartyUuid,
                                    string.Join(", ", packageUrns));
                                continue;
                            }

                            List<AssignmentPackageDto> adds = await assignmentService.ImportAssignmentPackages(item.FromPartyUuid, item.ToUserPartyUuid.Value, packageUrns, values, cancellationToken);
                            if (adds.Count == 0)
                            {
                                _logger.LogWarning(
                                    "Failed to import delegation for FromParty: {FromParty}, ToParty: {ToParty}, PackageUrns: {packageUrn}",
                                    item.FromPartyUuid,
                                    item.ToUserPartyUuid,
                                    string.Join(", ", packageUrns));
                            }
                        }

                    }
                }

                if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
                {
                    return;
                }

                await lease.Update<AltinnAdminRoleLease>(d => d.AltinnAdminRoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }
        }

        private List<string> GetAdminPackageFromRoleTypeCode(string roleTypeCode, CancellationToken cancellationToken = default)
        {
            List<string> packages = new List<string>();

            switch (roleTypeCode.ToUpper())
            {
                case "ADMAI":
                    packages.Add("urn:altinn:accesspackage:tilgangsstyrer");
                    break;
                case "APIADM":
                    packages.Add("urn:altinn:accesspackage:maskinporten-administrator");
                    packages.Add("urn:altinn:accesspackage:maskinporten-scopes");
                    break;
                case "APIADMNUF":
                    packages.Add("urn:altinn:accesspackage:maskinporten-administrator");
                    packages.Add("urn:altinn:accesspackage:maskinporten-scopes-nuf");
                    break;
                case "BOADM":
                    packages.Add("urn:altinn:accesspackage:konkursbo-tilgangsstyrer");
                    break;
                case "HADM":
                    packages.Add("urn:altinn:accesspackage:hovedadministrator");
                    break;
                case "KLADM":
                    packages.Add("urn:altinn:accesspackage:klientadministrator");
                    break;
            }

            return packages;
        }
    }
}
