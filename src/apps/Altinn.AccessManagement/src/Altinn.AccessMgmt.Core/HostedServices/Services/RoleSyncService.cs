using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
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
    public async Task SyncRoles(ILease lease, bool isInit = false, CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<(Guid From, Guid To, Guid Role)>();
        var addParent = new Dictionary<Guid, Guid>();
        var removeParent = new List<Guid>();
        var addAssignments = new List<Assignment>();
        var removeAssignments = new List<Assignment>();
        var options = new AuditValues(SystemEntityConstants.RegisterImportSystem);

        using var scope = _serviceProvider.CreateEFScope(options);
        var appDbContextFactory = scope.ServiceProvider.GetRequiredService<AppDbContextFactory>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();
        var leaseData = await lease.Get<RegisterLease>(cancellationToken);

        if (isInit == false && leaseData.IsDbIngested == false)
        {
            return;
        }

        await foreach (var page in await _register.StreamRoles([], leaseData.RoleStreamNextPageLink, cancellationToken))
        {
            if (page.IsProblem)
            {
                Log.ResponseError(_logger, page.StatusCode);
                throw new Exception("Stream page is not successful");
            }

            _logger.LogInformation("Starting proccessing party page ({0}-{1})", page.Content.Stats.PageStart, page.Content.Stats.PageEnd);

            foreach (var item in page.Content.Data)
            {
                var assignment = MapToAssignment(item);
                if (ShouldSetParent(item))
                {
                    if (!seen.Add((From: assignment.FromId, To: assignment.ToId, Role: RoleConstants.HasAsRegistrationUnitBEDR)))
                    {
                        await Flush();
                    }
                    else if (!seen.Add((From: assignment.FromId, To: assignment.ToId, Role: RoleConstants.HasAsRegistrationUnitAAFY)))
                    {
                        await Flush();
                    }
                }
                else
                {
                    if (!seen.Add((From: assignment.FromId, To: assignment.ToId, Role: assignment.RoleId)))
                    {
                        await Flush();
                    }
                }

                if (item.Type == ExternalRoleAssignmentEvent.EventType.Added)
                {
                    addAssignments.Add(assignment);
                    if (ShouldSetParent(item))
                    {
                        addParent[assignment.FromId] = assignment.ToId;
                    }
                }
                else if (item.Type == ExternalRoleAssignmentEvent.EventType.Removed)
                {
                    removeAssignments.Add(assignment);
                    if (ShouldSetParent(item))
                    {
                        removeParent.Add(assignment.FromId);
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
                await Task.WhenAll(
                    RemoveParents(appDbContextFactory, removeParent, cancellationToken),
                    SetParents(appDbContextFactory, addParent, cancellationToken),
                    RemoveAssignments(appDbContextFactory, removeAssignments, cancellationToken),
                    IngestAssigments(ingestService, addAssignments, options, cancellationToken)
                );

                seen.Clear();
                addParent.Clear();
                removeParent.Clear();
                addAssignments.Clear();
                removeAssignments.Clear();
            }
        }
    }

    private async Task RemoveAssignments(AppDbContextFactory dbContextFactory, List<Assignment> relations, CancellationToken cancellationToken = default)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var relationsFrom = relations
            .Select(r => r.FromId)
            .ToList();

        var entities = await dbContext.Assignments
            .AsTracking()
            .Where(e => relationsFrom.Contains(e.FromId))
            .ToListAsync(cancellationToken: cancellationToken);

        var relationSet = relations
            .Select(r => (r.FromId, r.ToId, r.RoleId))
            .ToHashSet();

        entities = entities
            .Where(e => relationSet.Contains((e.FromId, e.ToId, e.RoleId)))
            .ToList();

        dbContext.RemoveRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task IngestAssigments(IIngestService ingestService, List<Assignment> assignments, AuditValues options, CancellationToken cancellationToken)
    {
        var batchId = Guid.CreateVersion7();
        try
        {
            _logger.LogInformation("Ingest and Merge Assignment batch '{0}' to db", batchId.ToString());
            var ingested = await ingestService.IngestTempData<Assignment>(assignments, batchId, cancellationToken);

            if (ingested != assignments.Count)
            {
                _logger.LogWarning("Ingest partial complete: Assignment ({0}/{1})", ingested, assignments.Count);
            }

            var merged = await ingestService.MergeTempData<Assignment>(batchId, options, ["fromid", "roleid", "toid"], cancellationToken: cancellationToken);

            _logger.LogInformation("Merge complete: Assignment ({0}/{1})", merged, ingested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString());
            throw new Exception(string.Format("Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString()), ex);
        }
    }

    private async Task RemoveParents(AppDbContextFactory dbContextFactory, List<Guid> relations, CancellationToken cancellationToken = default)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var entities = await dbContext.Entities
            .AsTracking()
            .Where(e => relations.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.ParentId = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SetParents(AppDbContextFactory dbContextFactory, Dictionary<Guid, Guid> relations, CancellationToken cancellationToken = default)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var fields = relations.Keys.ToList();
        var entities = await dbContext.Entities
            .AsTracking()
            .Where(e => fields.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.ParentId = relations[entity.Id];
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private bool ShouldSetParent(ExternalRoleAssignmentEvent item) =>
        item.RoleIdentifier == RoleConstants.HasAsRegistrationUnitBEDR.Entity.Code || item.RoleIdentifier == RoleConstants.HasAsRegistrationUnitAAFY.Entity.Code;

    private Assignment MapToAssignment(ExternalRoleAssignmentEvent model)
    {
        if (RoleConstants.TryGetByCode(model.RoleIdentifier, out var role))
        {
            return new Assignment()
            {
                FromId = model.FromParty,
                ToId = model.ToParty,
                RoleId = role.Id
            };
        }

        throw new Exception(string.Format("Failed to convert model to Assignment. From:{0} To:{1} Role:{2}", model.FromParty, model.ToParty, model.RoleIdentifier));
    }
}
