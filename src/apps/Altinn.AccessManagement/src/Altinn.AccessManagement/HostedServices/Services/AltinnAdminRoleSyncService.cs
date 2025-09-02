using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.SblBridge;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices.Services
{
    public class AltinnAdminRoleSyncService : BaseSyncService, IAltinnAdminRoleSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnClientRoleSyncService"/> class.
        /// </summary>
        /// <param name="lease">The lease service used for managing leases.</param>
        /// <param name="role">The role service used for streaming roles.</param>
        /// <param name="assignmentService">The delegation service used for managing delegations.</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        /// <param name="featureManager">The feature manager for handling feature flags.</param>
        public AltinnAdminRoleSyncService(
            IAltinnLease lease,
            IAltinnSblBridge role,
            IAssignmentService assignmentService,
            ILogger<AltinnClientRoleSyncService> logger,
            IFeatureManager featureManager
        ) : base(lease, featureManager)
        {
            _role = role;
            _assignmentService = assignmentService;
            _logger = logger;
        }

        private readonly IAltinnSblBridge _role;
        private readonly IAssignmentService _assignmentService;
        private readonly ILogger<AltinnClientRoleSyncService> _logger;

        /// <inheritdoc />
        public async Task SyncAdminRoles(LeaseResult<AltinnAdminRoleLease> ls, CancellationToken cancellationToken)
        {
            var adminDelegations = await _role.StreamRoles("11", ls.Data?.AltinnAdminRoleStreamNextPageLink, cancellationToken);

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

                Guid batchId = Guid.NewGuid();
                var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
                _logger.LogInformation("Starting proccessing role page '{0}'", batchName);

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        ChangeRequestOptions options = new ChangeRequestOptions()
                        {
                            ChangedBy = item.PerformedByUserUuid ?? AuditDefaults.Altinn2RoleImportSystem,
                            ChangedBySystem = AuditDefaults.Altinn2RoleImportSystem,
                            ChangedAt = item.DelegationChangeDateTime ?? DateTime.UtcNow,
                        };

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
                            
                            int revokes = await _assignmentService.RevokeAdminAssignmentPackages(
                                item.ToUserPartyUuid.Value,
                                item.FromPartyUuid,
                                packageUrns,
                                options,
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

                            int adds = await _assignmentService.ImportAdminAssignmentPackages(item.ToUserPartyUuid.Value, item.FromPartyUuid, packageUrns, options, cancellationToken);
                            if (adds == 0)
                            {
                                _logger.LogWarning(
                                    "Failed to delete delegation for FromParty: {FromParty}, ToParty: {ToParty}, PackageUrns: {packageUrn}",
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

                await UpdateLease(ls, data => data.AltinnAdminRoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);
            }
        }

        private List<string> GetAdminPackageFromRoleTypeCode(string roleTypeCode, CancellationToken cancellationToken = default)
        {
            List<string> packages = new List<string>();
            
            switch (roleTypeCode)
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
