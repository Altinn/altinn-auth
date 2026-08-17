using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Data for the entity details page.
/// </summary>
public sealed record EntityDetailsData(
    Entity Entity,
    IReadOnlyList<Entity> Children,
    IReadOnlyList<Assignment> AssignmentsFrom,
    IReadOnlyList<Assignment> AssignmentsTo,
    IReadOnlyList<Delegation> DelegationsGiven,
    IReadOnlyList<Delegation> DelegationsReceived,
    IReadOnlyList<RequestAssignment> RequestsFor,
    IReadOnlyList<RequestAssignment> RequestsBy,
    IReadOnlyList<RequestAssignment> RequestsFrom);

/// <summary>
/// Loads data for the entity details page.
/// </summary>
public sealed class EntityDetailsService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Returns null when the entity does not exist in the environment.
    /// </summary>
    public async Task<EntityDetailsData?> GetAsync(string environment, Guid id, CancellationToken ct = default)
    {
        Entity? entity;
        using (var db = dbFactory.CreateContext(environment))
        {
            entity = await db.Entities
                .AsNoTracking()
                .Include(e => e.Type)
                .Include(e => e.Variant)
                .Include(e => e.Parent)
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        if (entity is null)
        {
            return null;
        }

        // Related lists load in parallel, each on its own context (a context must never be shared across concurrent tasks).
        var childrenTask = LoadChildrenAsync(environment, id, ct);
        var assignmentsTask = LoadAssignmentsAsync(environment, id, ct);
        var delegationsTask = LoadDelegationsAsync(environment, id, ct);
        var requestsTask = LoadRequestsAsync(environment, id, ct);

        await Task.WhenAll(childrenTask, assignmentsTask, delegationsTask, requestsTask);

        var children = childrenTask.Result;
        var (assignmentsFrom, assignmentsTo) = assignmentsTask.Result;
        var (delegationsGiven, delegationsReceived) = delegationsTask.Result;
        var (requestsFor, requestsBy, requestsFrom) = requestsTask.Result;

        return new EntityDetailsData(
            entity,
            children,
            assignmentsFrom,
            assignmentsTo,
            delegationsGiven,
            delegationsReceived,
            requestsFor,
            requestsBy,
            requestsFrom);
    }

    private async Task<List<Entity>> LoadChildrenAsync(string environment, Guid id, CancellationToken ct)
    {
        using var db = dbFactory.CreateContext(environment);
        return await db.Entities
            .AsNoTracking()
            .Include(e => e.Type)
            .Include(e => e.Variant)
            .Where(e => e.ParentId == id)
            .OrderBy(e => e.Name)
            .Take(500)
            .ToListAsync(ct);
    }

    private async Task<(List<Assignment> From, List<Assignment> To)> LoadAssignmentsAsync(string environment, Guid id, CancellationToken ct)
    {
        using var db = dbFactory.CreateContext(environment);
        var assignmentsFrom = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Role)
            .Include(a => a.To)
            .Where(a => a.FromId == id)
            .OrderBy(a => a.Role.Name)
            .Take(200)
            .ToListAsync(ct);

        var assignmentsTo = await db.Assignments
            .AsNoTracking()
            .Include(a => a.Role)
            .Include(a => a.From)
            .Where(a => a.ToId == id)
            .OrderBy(a => a.Role.Name)
            .Take(200)
            .ToListAsync(ct);

        return (assignmentsFrom, assignmentsTo);
    }

    private async Task<(List<Delegation> Given, List<Delegation> Received)> LoadDelegationsAsync(string environment, Guid id, CancellationToken ct)
    {
        using var db = dbFactory.CreateContext(environment);
        var delegationsGiven = await db.Delegations
            .AsNoTracking()
            .Include(d => d.From).ThenInclude(a => a.Role)
            .Include(d => d.From).ThenInclude(a => a.From)
            .Include(d => d.To).ThenInclude(a => a.To)
            .Where(d => d.From.ToId == id)
            .OrderBy(d => d.From.Role.Name)
            .Take(200)
            .ToListAsync(ct);

        var delegationsReceived = await db.Delegations
            .AsNoTracking()
            .Include(d => d.From).ThenInclude(a => a.Role)
            .Include(d => d.From).ThenInclude(a => a.From)
            .Include(d => d.From).ThenInclude(a => a.To)
            .Include(d => d.To).ThenInclude(a => a.Role)
            .Where(d => d.To.ToId == id)
            .OrderBy(d => d.To.Role.Name)
            .Take(200)
            .ToListAsync(ct);

        return (delegationsGiven, delegationsReceived);
    }

    private async Task<(List<RequestAssignment> For, List<RequestAssignment> By, List<RequestAssignment> From)> LoadRequestsAsync(string environment, Guid id, CancellationToken ct)
    {
        using var db = dbFactory.CreateContext(environment);
        var requestsFor = await db.RequestAssignments
            .AsNoTracking()
            .Include(r => r.Role)
            .Include(r => r.From)
            .Include(r => r.By)
            .Where(r => r.ToId == id)
            .OrderByDescending(r => r.Audit_ValidFrom)
            .Take(200)
            .ToListAsync(ct);

        var requestsBy = await db.RequestAssignments
            .AsNoTracking()
            .Include(r => r.Role)
            .Include(r => r.From)
            .Include(r => r.To)
            .Where(r => r.ById == id)
            .OrderByDescending(r => r.Audit_ValidFrom)
            .Take(200)
            .ToListAsync(ct);

        var requestsFrom = await db.RequestAssignments
            .AsNoTracking()
            .Include(r => r.Role)
            .Include(r => r.To)
            .Include(r => r.By)
            .Where(r => r.FromId == id)
            .OrderByDescending(r => r.Audit_ValidFrom)
            .Take(200)
            .ToListAsync(ct);

        return (requestsFor, requestsBy, requestsFrom);
    }
}
