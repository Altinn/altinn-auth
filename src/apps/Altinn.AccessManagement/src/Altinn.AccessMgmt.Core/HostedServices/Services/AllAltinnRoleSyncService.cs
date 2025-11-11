using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.SblBridge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Altinn.AccessMgmt.Core.HostedServices.Services
{
    public class AllAltinnRoleSyncService : IAllAltinnRoleSyncService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AltinnClientRoleSyncService"/> class.
        /// </summary>
        /// <param name="role">The role service used for streaming roles.</param>
        /// <param name="serviceProvider">object used for creating a scope and fetching a scoped service (IDelegationService) based on this scope</param>
        /// <param name="logger">The logger instance for logging information and errors.</param>
        public AllAltinnRoleSyncService(
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

        /// <inheritdoc />
        public async Task SyncAllAltinnRoles(ILease lease, CancellationToken cancellationToken)
        {
            var batchData = new List<Assignment>();
            Guid batchId = Guid.CreateVersion7();

            var leaseData = await lease.Get<AllAltinnRoleLease>(cancellationToken);

            var allRoles = await _role.StreamRoles("10", leaseData.AllAltinnRoleStreamNextPageLink, cancellationToken);

            AuditValues currentOptions = new AuditValues(SystemEntityConstants.Altinn2RoleImportSystem);

            using var scope = _serviceProivider.CreateEFScope(currentOptions);
            var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContextFactory>().CreateDbContext();
            var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();

            await foreach (var page in allRoles)
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

                var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
                _logger.LogInformation("Starting proccessing role page '{0}'", batchName);

                AuditValues previousOptions = null;

                if (page.Content != null)
                {
                    foreach (var item in page.Content.Data)
                    {
                        if (item.ToUserType != UserType.EnterpriseIdentified && item.RoleTypeCode == RoleConstants.Eckeyrole.Entity.Code)
                        {                             
                            // Skip ECKEYROLE for non-enterprise users
                            continue;
                        }

                        var assignment = await ConvertRoleDelegationModelToAssignment(appDbContext, item, batchId.ToString(), cancellationToken);
                        if (assignment.Assignment == null)
                        {
                            throw new Exception("Failed to convert RoleModel to Assignment");
                        }

                        currentOptions = assignment.Options;

                        if (batchData.Any(t => t.FromId == assignment.Assignment.FromId && t.ToId == assignment.Assignment.ToId && t.RoleId == assignment.Assignment.RoleId) || (previousOptions != null && currentOptions.ChangedBy != previousOptions.ChangedBy))
                        {
                            // If changes on same assignment or performed in a difrent user then execute as-is before continuing.
                            await Flush(batchId);
                        }

                        if (item.DelegationAction == DelegationAction.Delegate)
                        {
                            batchData.Add(assignment.Assignment);
                        }
                        else
                        {
                            var deleteAssignment = await appDbContext.Assignments
                            .AsTracking()
                            .Where(a => a.FromId == assignment.Assignment.FromId && a.ToId == assignment.Assignment.ToId && a.RoleId == assignment.Assignment.RoleId)
                            .FirstOrDefaultAsync(cancellationToken);

                            if (deleteAssignment is { })
                            {
                                appDbContext.Remove(deleteAssignment);
                                await appDbContext.SaveChangesAsync(assignment.Options, cancellationToken);
                            }
                        }

                        previousOptions = currentOptions;
                    }
                }

                if (batchData.Count > 0)
                {
                    await Flush(batchId);
                }

                if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
                {
                    return;
                }

                await lease.Update<AllAltinnRoleLease>(l => l.AllAltinnRoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);

                async Task Flush(Guid batchId)
                {
                    try
                    {
                        _logger.LogInformation("Ingest and Merge Assignment batch '{0}' to db", batchName);
                        var ingested = await ingestService.IngestTempData<Assignment>(batchData, batchId, cancellationToken);

                        if (ingested != batchData.Count)
                        {
                            _logger.LogWarning("Ingest partial complete: Assignment ({0}/{1})", ingested, batchData.Count);
                        }

                        var merged = await ingestService.MergeTempData<Assignment>(batchId, previousOptions, GetAssignmentMergeMatchFilter, cancellationToken: cancellationToken);

                        _logger.LogInformation("Merge complete: Assignment ({0}/{1})", merged, ingested);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchName);
                        throw new Exception(string.Format("Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchName), ex);
                    }
                    finally
                    {
                        batchId = Guid.CreateVersion7();
                        batchData.Clear();
                    }
                }
            }
        }

        private static readonly IReadOnlyList<string> GetAssignmentMergeMatchFilter = new List<string>() { "fromid", "roleid", "toid" }.AsReadOnly();

        private async Task<(Assignment Assignment, AuditValues Options)> ConvertRoleDelegationModelToAssignment(AppDbContext dbContext, RoleDelegationModel model, string batchId, CancellationToken cancellationToken)
        {
            try
            {
                var role = await GetRole(dbContext, model.RoleTypeCode, cancellationToken);

                Assignment assignment = new Assignment()
                {
                    Id = Guid.CreateVersion7(),
                    FromId = model.FromPartyUuid,
                    ToId = model.ToUserPartyUuid.Value,
                    RoleId = role.Id,
                    Audit_ValidFrom = model.DelegationChangeDateTime?.UtcDateTime ?? DateTime.UtcNow
                };
                
                AuditValues options = new AuditValues(model.PerformedByPartyUuid ?? model.PerformedByUserUuid ?? SystemEntityConstants.Altinn2RoleImportSystem, SystemEntityConstants.Altinn2RoleImportSystem, batchId, model.DelegationChangeDateTime ?? DateTimeOffset.UtcNow);

                return (assignment, options);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to convert model to Assignment. From:{0} To:{1} Role:{2}", model.FromPartyUuid, model.ToUserPartyUuid, model.RoleTypeCode));
            }
        }

        private Dictionary<string, Role> Roles { get; set; } = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);

        private async Task<Role> GetRole(AppDbContext dbContext, string roleCode, CancellationToken cancellationToken)
        {
            string roleIdentifier = $"urn:altinn:rolecode:{roleCode.ToLower()}";

            if (Roles.TryGetValue(roleIdentifier, out var cached))
            {
                return cached;
            }

            var role = await dbContext.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => string.Equals(r.Urn.ToLower(), roleIdentifier), cancellationToken);

            if (role == null)
            {
                throw new Exception(string.Format("Unable to get role '{0}'", roleIdentifier));
            }

            Roles.Add(roleIdentifier, role);
            return role;
        }
    }
}
