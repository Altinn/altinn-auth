using System.Diagnostics;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public class RoleSyncService : BaseSyncService, IRoleSyncService
{
    public RoleSyncService(
        IAltinnRegister register,
        ILogger<RoleSyncService> logger,
        IServiceProvider serviceProvider
    )
    {
        _register = register;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private readonly IAltinnRegister _register;
    private readonly ILogger<RoleSyncService> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public async Task SyncRoles(ILease lease, CancellationToken cancellationToken)
    {
        var batchData = new List<Assignment>();
        var seen = new HashSet<(Guid, Guid, Guid)>();

        var options = new AuditValues(SystemEntityConstants.RegisterImportSystem);

        using var scope = _serviceProvider.CreateEFScope(options);
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();

        OrgType = EntityTypeConstants.Organisation;

        Provider = await appDbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == "ccr", cancellationToken);

        Roles = (await appDbContext.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken)).ToDictionary(t => t.Code, t => t);

        var leaseData = await lease.Get<RegisterLease>(cancellationToken);

        await foreach (var page in await _register.StreamRoles([], leaseData.RoleStreamNextPageLink, cancellationToken))
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

            _logger.LogInformation("Starting proccessing party page ({0}-{1})", page.Content.Stats.PageStart, page.Content.Stats.PageEnd);

            if (page.Content != null)
            {
                foreach (var item in page.Content.Data)
                {
                    var assignment = await ConvertRoleModel(appDbContext, item, options: options, cancellationToken) ?? throw new Exception("Failed to convert RoleModel to Assignment");

                    var key = (assignment.FromId, assignment.ToId, assignment.RoleId);
                    if (!seen.Add(key))
                    {
                        // If changes on same assignment then execute as-is before continuing.
                        await Flush();
                    }

                    if (item.Type == "Added")
                    {
                        batchData.Add(assignment);
                        if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                        {
                            await SetParent(appDbContext, assignment.FromId, assignment.ToId, cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        var deleteAssignment = await appDbContext.Assignments
                            .AsTracking()
                            .Where(a => a.FromId == assignment.FromId && a.ToId == assignment.ToId && a.RoleId == assignment.RoleId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (deleteAssignment is { })
                        {
                            appDbContext.Remove(deleteAssignment);
                            await appDbContext.SaveChangesAsync(cancellationToken);
                        }

                        if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                        {
                            await RemoveParent(appDbContext, assignment.FromId, cancellationToken: cancellationToken);
                        }
                    }
                }
            }

            await Flush();

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            leaseData.RoleStreamNextPageLink = page.Content.Links.Next;
            await lease.Update(leaseData, cancellationToken);

            async Task Flush()
            {
                var batchId = Guid.CreateVersion7();
                var batchName = batchId.ToString("N");

                try
                {
                    _logger.LogInformation("Ingest and Merge Assignment batch '{0}' to db", batchId.ToString());
                    var ingested = await ingestService.IngestTempData<Assignment>(batchData, batchId, cancellationToken);

                    if (ingested != batchData.Count)
                     {
                        _logger.LogWarning("Ingest partial complete: Assignment ({0}/{1})", ingested, batchData.Count);
                    }

                    var merged = await ingestService.MergeTempData<Assignment>(batchId, options, ["fromid", "roleid", "toid"], cancellationToken: cancellationToken);

                    _logger.LogInformation("Merge complete: Assignment ({0}/{1})", merged, ingested);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString());
                    throw new Exception(string.Format("Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString()), ex);
                }
                finally
                {
                    batchData.Clear();
                    seen.Clear();
                }
            }
        }
    }

    private async Task SetParent(AppDbContext dbContext, Guid childId, Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await dbContext.Entities
                .AsTracking()
                .FirstAsync(e => e.Id == childId, cancellationToken: cancellationToken);

            entity.ParentId = parentId;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to set '{1}' as parent to '{0}'", childId, parentId), ex);
        }
    }

    private async Task RemoveParent(AppDbContext dbContext, Guid childId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await dbContext.Entities
                .AsTracking()
                .FirstAsync(e => e.Id == childId, cancellationToken: cancellationToken);

            entity.ParentId = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to remove parent for '{0}'", childId), ex);
        }
    }

    private Dictionary<string, Role> Roles { get; set; } = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);

    private async Task<Assignment> ConvertRoleModel(AppDbContext dbContext, RoleModel model, AuditValues options, CancellationToken cancellationToken)
    {
        try
        {
            var role = await GetOrCreateRole(dbContext, model.RoleIdentifier, model.RoleSource, cancellationToken);
            return new Assignment()
            {
                FromId = Guid.Parse(model.FromParty),
                ToId = Guid.Parse(model.ToParty),
                RoleId = role.Id
            };
        }
        catch
        {
            throw new Exception(string.Format("Failed to convert model to Assignment. From:{0} To:{1} Role:{2}", model.FromParty, model.ToParty, model.RoleIdentifier));
        }
    }

    private async Task<Role> GetOrCreateRole(AppDbContext dbContext, string roleIdentifier, string roleSource, CancellationToken cancellationToken)
    {
        if (Roles.TryGetValue(roleIdentifier, out var cached))
        {
            return cached;
        }

        var role = await dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Code == roleIdentifier, cancellationToken);
        if (role is null)
        {
            role = new Role()
            {
                Id = Guid.CreateVersion7(),
                Name = roleIdentifier,
                Description = roleIdentifier,
                Code = roleIdentifier,
                Urn = roleIdentifier,
                EntityTypeId = OrgType.Id,
                ProviderId = Provider.Id,
            };

            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        Roles.Add(role.Code, role);
        return role;
    }

    private EntityType OrgType { get; set; }

    private Provider Provider { get; set; }
}
