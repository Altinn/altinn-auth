using System.Collections.Concurrent;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Responsible for enriching connection query records with entity, child, and role data.
/// </summary>
internal class ConnectionEntityEnricher(AppDbContext db, ILogger logger)
{
    /// <summary>
    /// Role ids the drift warning has already been logged for. Drift is a state, not an event:
    /// a retired role stays in the table and referenced until someone cleans it up, so without
    /// this the warning would repeat on every enrichment call for as long as it lasts.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, byte> WarnedStrayRoleIds = new();

    /// <summary>
    /// Enriches the given records with entity, role, and child-nesting data.
    /// </summary>
    public async Task<List<ConnectionQueryExtendedRecord>> EnrichAsync(List<ConnectionQueryExtendedRecord> allKeys, ConnectionQueryFilter filter, bool doChildNesting, bool applyFromFilter, CancellationToken ct)
    {
        var entityDict = await FetchEntitiesAsync(allKeys, ct);
        var childrenDict = doChildNesting ? await FetchChildrenAsync(entityDict, filter, applyFromFilter, ct) : [];
        var rolesDict = await ResolveRolesAsync(allKeys, ct);

        return ApplyEnrichment(allKeys, entityDict, childrenDict, rolesDict, doChildNesting, applyFromFilter, filter);
    }

    /// <summary>
    /// Resolves the roles referenced by the records, from the constants where possible and from
    /// the database for any id they do not cover.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Roles are seeded from <see cref="RoleConstants"/> and never written at runtime, so the
    /// role table is only a projection of the constants. Projecting the referenced roles from
    /// the constants removes a load of all roles, joined to provider and provider type, from
    /// every enrichment call.
    /// </para>
    /// <para>
    /// The projection is rebuilt per call rather than cached. The result is handed to callers as
    /// ordinary mutable models, and a process-wide cache would turn any later in-place edit, such
    /// as a translation applied to the wrong object, into corrupted role data for every request
    /// until restart. Fresh instances per call keep the same ownership the database query had.
    /// </para>
    /// <para>
    /// StaticDataIngest never deletes, so a role dropped from <see cref="RoleConstants"/> in an
    /// earlier release can still exist in the database and be referenced by an assignment made
    /// while it was current. Falling back keeps reads working through that drift instead of
    /// failing the whole query on one stale row.
    /// </para>
    /// </remarks>
    private async Task<Dictionary<Guid, Role>> ResolveRolesAsync(List<ConnectionQueryExtendedRecord> allKeys, CancellationToken ct)
    {
        HashSet<Guid> referenced = [];
        foreach (var key in allKeys)
        {
            if (key.RoleId != Guid.Empty)
            {
                referenced.Add(key.RoleId);
            }

            if (key.ViaRoleId is { } viaRoleId && viaRoleId != Guid.Empty)
            {
                referenced.Add(viaRoleId);
            }
        }

        Dictionary<Guid, Role> resolved = [];
        Dictionary<Guid, Provider> providersById = [];
        HashSet<Guid> missing = [];
        foreach (var roleId in referenced)
        {
            if (RoleConstants.TryGetById(roleId, out var definition))
            {
                resolved[roleId] = ProjectRole(definition.Entity, providersById);
            }
            else
            {
                missing.Add(roleId);
            }
        }

        if (missing.Count == 0)
        {
            return resolved;
        }

        var unwarned = missing.Where(id => WarnedStrayRoleIds.TryAdd(id, 0)).ToList();
        if (unwarned.Count > 0)
        {
            logger.LogWarning(
                "RoleConstants does not cover {StrayRoleCount} role(s) referenced by connections: {StrayRoleIds}. Falling back to the role table; the seeded constants and the database have drifted.",
                unwarned.Count,
                string.Join(", ", unwarned));
        }

        var strays = await db.Roles
            .Include(r => r.Provider).ThenInclude(p => p.Type)
            .AsNoTracking()
            .Where(r => missing.Contains(r.Id))
            .ToListAsync(ct);

        foreach (var stray in strays)
        {
            resolved[stray.Id] = stray;
        }

        return resolved;
    }

    /// <summary>
    /// Bulk-loads entities by collected party IDs from the records.
    /// </summary>
    private async Task<Dictionary<Guid, Entity>> FetchEntitiesAsync(List<ConnectionQueryExtendedRecord> allKeys, CancellationToken ct)
    {
        HashSet<Guid> parties = [];
        foreach (var item in allKeys)
        {
            parties.Add(item.FromId);
            parties.Add(item.ToId);

            if (item.ViaId != null)
            {
                parties.Add((Guid)item.ViaId);
            }
        }

        var entities = await db
            .Entities
            .AsNoTracking()
            .Where(e => parties.Contains(e.Id))
            .Select(e => new Entity()
            {
                Id = e.Id,
                Name = e.Name,
                OrganizationIdentifier = e.OrganizationIdentifier,
                ParentId = e.ParentId,
                PersonIdentifier = e.PersonIdentifier,
                DateOfBirth = e.DateOfBirth,
                DateOfDeath = e.DateOfDeath,
                PartyId = e.PartyId,
                IsDeleted = e.IsDeleted,
                DeletedAt = e.DeletedAt,
                UserId = e.UserId,
                Username = e.Username,
                TypeId = e.TypeId,
                VariantId = e.VariantId,
                EmailIdentifier = e.EmailIdentifier,
                Parent = e.Parent != null ? new Entity()
                {
                    Id = e.Parent.Id,
                    Name = e.Parent.Name,
                    OrganizationIdentifier = e.Parent.OrganizationIdentifier,
                    ParentId = e.Parent.ParentId,
                    PersonIdentifier = e.Parent.PersonIdentifier,
                    DateOfBirth = e.Parent.DateOfBirth,
                    DateOfDeath = e.Parent.DateOfDeath,
                    PartyId = e.Parent.PartyId,
                    IsDeleted = e.Parent.IsDeleted,
                    DeletedAt = e.Parent.DeletedAt,
                    UserId = e.Parent.UserId,
                    Username = e.Parent.Username,
                    TypeId = e.Parent.TypeId,
                    VariantId = e.Parent.VariantId,
                    EmailIdentifier = e.Parent.EmailIdentifier
                }
                : null
            })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        Dictionary<Guid, Entity> entityDict = [];
        foreach (var entity in entities)
        {
            entityDict.Add(entity.Id, entity);
        }

        return entityDict;
    }

    /// <summary>
    /// Loads child entities for hierarchy nesting.
    /// </summary>
    private async Task<Dictionary<Guid, List<Entity>>> FetchChildrenAsync(Dictionary<Guid, Entity> entityDict, ConnectionQueryFilter filter, bool applyFromFilter, CancellationToken ct)
    {
        var allChildren = await db
            .Entities
            .AsNoTracking()
            .Where(e => e.ParentId != null && entityDict.Keys.Contains((Guid)e.ParentId))
            .Select(e => new Entity()
            {
                Id = e.Id,
                Name = e.Name,
                OrganizationIdentifier = e.OrganizationIdentifier,
                ParentId = e.ParentId,
                Parent = entityDict[(Guid)e.ParentId],
                PersonIdentifier = e.PersonIdentifier,
                DateOfBirth = e.DateOfBirth,
                DateOfDeath = e.DateOfDeath,
                PartyId = e.PartyId,
                IsDeleted = e.IsDeleted,
                DeletedAt = e.DeletedAt,
                UserId = e.UserId,
                Username = e.Username,
                TypeId = e.TypeId,
                VariantId = e.VariantId
            })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(ct);

        if (applyFromFilter && filter.FromIds != null && filter.FromIds.Count > 0)
        {
            allChildren = allChildren.Where(c => filter.FromIds.Contains(c.Id)).ToList();
        }

        Dictionary<Guid, List<Entity>> childrenDict = [];
        foreach (var child in allChildren)
        {
            if (!childrenDict.ContainsKey((Guid)child.ParentId))
            {
                childrenDict.Add((Guid)child.ParentId, [child]);
            }
            else
            {
                childrenDict[(Guid)child.ParentId].Add(child);
            }
        }

        return childrenDict;
    }

    /// <summary>
    /// Projects a role constant into the same shape the previous query produced: the role with
    /// its provider, and that provider with its type. <see cref="Role.EntityType"/> is left
    /// unset because the query did not include it either.
    /// </summary>
    /// <remarks>
    /// The graph is rebuilt here rather than assigned onto the constants' own entities. Those
    /// instances are the seeds StaticDataIngest hands to EF, and a populated reference navigation
    /// on a seed makes <c>DbSet.Add</c> cascade into an insert of the referenced provider.
    /// Providers are shared between the roles of one call through <paramref name="providersById"/>,
    /// which matches what the query's include produced.
    /// </remarks>
    private static Role ProjectRole(Role seed, Dictionary<Guid, Provider> providersById)
    {
        if (!providersById.TryGetValue(seed.ProviderId, out var provider))
        {
            provider = ProjectProvider(seed.ProviderId);
            if (provider is not null)
            {
                providersById[seed.ProviderId] = provider;
            }
        }

        return new Role
        {
            Id = seed.Id,
            Name = seed.Name,
            Code = seed.Code,
            LegacyCode = seed.LegacyCode,
            Description = seed.Description,
            Urn = seed.Urn,
            LegacyUrn = seed.LegacyUrn,
            IsKeyRole = seed.IsKeyRole,
            IsAssignable = seed.IsAssignable,
            IsAvailableForServiceOwners = seed.IsAvailableForServiceOwners,
            EntityTypeId = seed.EntityTypeId,
            ProviderId = seed.ProviderId,
            Provider = provider,
        };
    }

    /// <summary>
    /// Projects a provider constant with its type, or returns null when the constants do not
    /// cover the id.
    /// </summary>
    private static Provider? ProjectProvider(Guid providerId)
    {
        if (!ProviderConstants.TryGetById(providerId, out var definition))
        {
            return null;
        }

        var seed = definition.Entity;
        return new Provider
        {
            Id = seed.Id,
            Name = seed.Name,
            RefId = seed.RefId,
            LogoUrl = seed.LogoUrl,
            Code = seed.Code,
            TypeId = seed.TypeId,
            Type = ProviderTypeConstants.TryGetById(seed.TypeId, out var providerType)
                ? new ProviderType { Id = providerType.Entity.Id, Name = providerType.Entity.Name }
                : null,
        };
    }

    /// <summary>
    /// Attaches entities/roles to records, and expands children.
    /// </summary>
    private static List<ConnectionQueryExtendedRecord> ApplyEnrichment(List<ConnectionQueryExtendedRecord> allKeys, Dictionary<Guid, Entity> entityDict, Dictionary<Guid, List<Entity>> childrenDict, Dictionary<Guid, Role> rolesDict, bool doChildNesting, bool applyFromFilter, ConnectionQueryFilter filter)
    {
        List<ConnectionQueryExtendedRecord> keysWithChildren = [];
        foreach (var c in allKeys)
        {
            c.From = entityDict[c.FromId];
            c.To = entityDict[c.ToId];
            c.Via = c.ViaId != null ? entityDict[(Guid)c.ViaId] : null;
            c.Role = c.RoleId != Guid.Empty ? rolesDict[c.RoleId] : null;
            c.ViaRole = c.ViaRoleId != null && c.ViaRoleId != Guid.Empty ? rolesDict[(Guid)c.ViaRoleId] : null;
            keysWithChildren.Add(c);

            if (doChildNesting && c.Reason != ConnectionReason.Hierarchy && childrenDict.TryGetValue(c.From.Id, out List<Entity> childrenForKey))
            {
                foreach (var child in childrenForKey)
                {
                    keysWithChildren.Add(new()
                    {
                        AssignmentId = c.AssignmentId,
                        FromId = child.Id,
                        From = child,
                        To = c.To,
                        ToId = c.ToId,
                        ViaId = c.FromId,
                        Via = c.From,
                        RoleId = c.RoleId,
                        Role = c.Role,
                        ViaRoleId = c.ViaRoleId,
                        ViaRole = c.ViaRole,

                        DelegationId = c.DelegationId,
                        IsKeyRoleAccess = c.IsKeyRoleAccess,
                        IsMainUnitAccess = true,
                        IsRoleMap = c.IsRoleMap,
                        Reason = ConnectionReason.Hierarchy,

                        Packages = c.Packages,
                        Resources = c.Resources
                    });
                }
            }
        }

        if (applyFromFilter && filter.FromIds != null && filter.FromIds.Count > 0)
        {
            keysWithChildren = keysWithChildren.Where(c => filter.FromIds.Contains(c.FromId)).ToList();
        }

        return keysWithChildren;
    }
}
