using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Components.Pages.Tools;

public sealed class TableDefinition
{
    public string Key { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string? AuditKey { get; init; }

    public required Func<AppDbContext, Task<int>> CountQuery { get; init; }

    public List<TableGrouping> Groupings { get; init; } = [];
}

public sealed class TableGrouping
{
    public string Label { get; init; } = string.Empty;

    public required Func<AppDbContext, Task<List<TableGroupRow>>> Query { get; init; }
}

public sealed class TableGroupRow
{
    public string Key { get; set; } = string.Empty;

    public int Count { get; set; }

    public TableGroupRow()
    {
    }

    public TableGroupRow(string key, int count)
    {
        Key = key;
        Count = count;
    }
}

public static class TableRegistry
{
    public static IReadOnlyList<TableDefinition> All { get; } = Build();

    public static IReadOnlyList<string> Categories { get; } =
        ["Entiteter", "Tilgang", "Innhold", "System", "Historikk"];

    public static TableDefinition? Find(string key) =>
        All.FirstOrDefault(t => t.Key == key);

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static TableGrouping G(string label, Func<AppDbContext, Task<List<TableGroupRow>>> q) =>
        new() { Label = label, Query = q };

    /// <summary>
    /// Generic date grouping via EF.Property — works for any entity that has Audit_ValidFrom.
    /// </summary>
    private static TableGrouping DateG<T>(Func<AppDbContext, DbSet<T>> getSet)
        where T : class =>
        G("Dato (år-mnd)", async db =>
        {
            var raw = await getSet(db)
                .GroupBy(e => new
                {
                    Year = EF.Property<DateTimeOffset>(e, "Audit_ValidFrom").Year,
                    Month = EF.Property<DateTimeOffset>(e, "Audit_ValidFrom").Month,
                })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                .ToListAsync();
            return raw.ConvertAll(g => new TableGroupRow($"{g.Year:D4}-{g.Month:D2}", g.Count));
        });

    private static async Task<List<TableGroupRow>> ByName(IQueryable<string?> q) =>
        await q.GroupBy(v => v)
               .Select(g => new TableGroupRow { Key = g.Key ?? "(null)", Count = g.Count() })
               .OrderByDescending(r => r.Count)
               .ToListAsync();

    // ── Registry ─────────────────────────────────────────────────────────────
    private static List<TableDefinition> Build() =>
    [
        // ── Entiteter ──────────────────────────────────────────────────────
        new()
        {
            Key = "entities", DisplayName = "Entities", Category = "Entiteter",
            AuditKey = "audit-entities",
            CountQuery = db => db.Entities.CountAsync(),
            Groupings =
            [
                G("Variant", db => ByName(
                    db.Entities.Join(db.EntityVariants, e => e.VariantId, v => v.Id, (_, v) => (string?)v.Name))),
                G("Type", db => ByName(
                    db.Entities.Join(db.EntityTypes, e => e.TypeId, t => t.Id, (_, t) => (string?)t.Name))),
                G("IsDeleted", async db => await db.Entities
                    .GroupBy(e => e.IsDeleted)
                    .Select(g => new TableGroupRow { Key = g.Key ? "true" : "false", Count = g.Count() })
                    .ToListAsync()),
                DateG(db => db.Entities),
            ],
        },

        new()
        {
            Key = "entity-lookups", DisplayName = "EntityLookups", Category = "Entiteter",
            AuditKey = "audit-entity-lookups",
            CountQuery = db => db.EntityLookups.CountAsync(),
            Groupings =
            [
                G("Key", async db => await db.EntityLookups
                    .GroupBy(e => e.Key)
                    .Select(g => new TableGroupRow { Key = g.Key, Count = g.Count() })
                    .OrderByDescending(r => r.Count).ToListAsync()),
                DateG(db => db.EntityLookups),
            ],
        },

        new()
        {
            Key = "entity-types", DisplayName = "EntityTypes", Category = "Entiteter",
            AuditKey = "audit-entity-types",
            CountQuery = db => db.EntityTypes.CountAsync(),
            Groupings = [DateG(db => db.EntityTypes)],
        },

        new()
        {
            Key = "entity-variants", DisplayName = "EntityVariants", Category = "Entiteter",
            AuditKey = "audit-entity-variants",
            CountQuery = db => db.EntityVariants.CountAsync(),
            Groupings =
            [
                G("Type", db => ByName(
                    db.EntityVariants.Join(db.EntityTypes, v => v.TypeId, t => t.Id, (_, t) => (string?)t.Name))),
                DateG(db => db.EntityVariants),
            ],
        },

        new()
        {
            Key = "entity-variant-roles", DisplayName = "EntityVariantRoles", Category = "Entiteter",
            AuditKey = "audit-entity-variant-roles",
            CountQuery = db => db.EntityVariantRoles.CountAsync(),
            Groupings = [DateG(db => db.EntityVariantRoles)],
        },

        // ── Tilgang ────────────────────────────────────────────────────────
        new()
        {
            Key = "assignments", DisplayName = "Assignments", Category = "Tilgang",
            AuditKey = "audit-assignments",
            CountQuery = db => db.Assignments.CountAsync(),
            Groupings =
            [
                G("Role", db => ByName(
                    db.Assignments.Join(db.Roles, a => a.RoleId, r => r.Id, (_, r) => (string?)r.Name))),
                DateG(db => db.Assignments),
            ],
        },

        new()
        {
            Key = "assignment-packages", DisplayName = "AssignmentPackages", Category = "Tilgang",
            AuditKey = "audit-assignment-packages",
            CountQuery = db => db.AssignmentPackages.CountAsync(),
            Groupings = [DateG(db => db.AssignmentPackages)],
        },

        new()
        {
            Key = "assignment-resources", DisplayName = "AssignmentResources", Category = "Tilgang",
            AuditKey = "audit-assignment-resources",
            CountQuery = db => db.AssignmentResources.CountAsync(),
            Groupings = [DateG(db => db.AssignmentResources)],
        },

        new()
        {
            Key = "assignment-instances", DisplayName = "AssignmentInstances", Category = "Tilgang",
            AuditKey = "audit-assignment-instances",
            CountQuery = db => db.AssignmentInstances.CountAsync(),
            Groupings = [DateG(db => db.AssignmentInstances)],
        },

        new()
        {
            Key = "delegations", DisplayName = "Delegations", Category = "Tilgang",
            AuditKey = "audit-delegations",
            CountQuery = db => db.Delegations.CountAsync(),
            Groupings = [DateG(db => db.Delegations)],
        },

        new()
        {
            Key = "delegation-packages", DisplayName = "DelegationPackages", Category = "Tilgang",
            AuditKey = "audit-delegation-packages",
            CountQuery = db => db.DelegationPackages.CountAsync(),
            Groupings = [DateG(db => db.DelegationPackages)],
        },

        new()
        {
            Key = "delegation-resources", DisplayName = "DelegationResources", Category = "Tilgang",
            AuditKey = "audit-delegation-resources",
            CountQuery = db => db.DelegationResources.CountAsync(),
            Groupings = [DateG(db => db.DelegationResources)],
        },

        new()
        {
            Key = "connections", DisplayName = "Connections", Category = "Tilgang",
            CountQuery = db => db.Connections.CountAsync(),
            Groupings =
            [
                G("Reason", async db => await db.Connections
                    .GroupBy(c => c.Reason)
                    .Select(g => new TableGroupRow { Key = g.Key ?? "(null)", Count = g.Count() })
                    .OrderByDescending(r => r.Count).ToListAsync()),
                DateG(db => db.Connections),
            ],
        },

        new()
        {
            Key = "request-assignments", DisplayName = "RequestAssignments", Category = "Tilgang",
            AuditKey = "audit-request-assignments",
            CountQuery = db => db.RequestAssignments.CountAsync(),
            Groupings =
            [
                G("Role", db => ByName(
                    db.RequestAssignments.Join(db.Roles, r => r.RoleId, ro => ro.Id, (_, ro) => (string?)ro.Name))),
                DateG(db => db.RequestAssignments),
            ],
        },

        new()
        {
            Key = "request-assignment-packages", DisplayName = "RequestAssignmentPackages", Category = "Tilgang",
            AuditKey = "audit-request-assignment-packages",
            CountQuery = db => db.RequestAssignmentPackages.CountAsync(),
            Groupings = [DateG(db => db.RequestAssignmentPackages)],
        },

        new()
        {
            Key = "request-assignment-resources", DisplayName = "RequestAssignmentResources", Category = "Tilgang",
            AuditKey = "audit-request-assignment-resources",
            CountQuery = db => db.RequestAssignmentResources.CountAsync(),
            Groupings = [DateG(db => db.RequestAssignmentResources)],
        },

        // ── Innhold ────────────────────────────────────────────────────────
        new()
        {
            Key = "packages", DisplayName = "Packages", Category = "Innhold",
            AuditKey = "audit-packages",
            CountQuery = db => db.Packages.CountAsync(),
            Groupings =
            [
                G("Area", db => ByName(
                    db.Packages.Join(db.Areas, p => p.AreaId, a => a.Id, (_, a) => (string?)a.Name))),
                DateG(db => db.Packages),
            ],
        },

        new()
        {
            Key = "package-resources", DisplayName = "PackageResources", Category = "Innhold",
            AuditKey = "audit-package-resources",
            CountQuery = db => db.PackageResources.CountAsync(),
            Groupings = [DateG(db => db.PackageResources)],
        },

        new()
        {
            Key = "resources", DisplayName = "Resources", Category = "Innhold",
            AuditKey = "audit-resources",
            CountQuery = db => db.Resources.CountAsync(),
            Groupings =
            [
                G("ResourceType", db => ByName(
                    db.Resources.Join(db.ResourceTypes, r => r.TypeId, t => t.Id, (_, t) => (string?)t.Name))),
                G("Provider", db => ByName(
                    db.Resources.Join(db.Providers, r => r.ProviderId, p => p.Id, (_, p) => (string?)p.Name))),
                DateG(db => db.Resources),
            ],
        },

        new()
        {
            Key = "resource-types", DisplayName = "ResourceTypes", Category = "Innhold",
            AuditKey = "audit-resource-types",
            CountQuery = db => db.ResourceTypes.CountAsync(),
            Groupings = [DateG(db => db.ResourceTypes)],
        },

        new()
        {
            Key = "roles", DisplayName = "Roles", Category = "Innhold",
            AuditKey = "audit-roles",
            CountQuery = db => db.Roles.CountAsync(),
            Groupings =
            [
                G("Provider", db => ByName(
                    db.Roles.Join(db.Providers, r => r.ProviderId, p => p.Id, (_, p) => (string?)p.Name))),
                DateG(db => db.Roles),
            ],
        },

        new()
        {
            Key = "role-maps", DisplayName = "RoleMaps", Category = "Innhold",
            AuditKey = "audit-role-maps",
            CountQuery = db => db.RoleMaps.CountAsync(),
            Groupings = [DateG(db => db.RoleMaps)],
        },

        new()
        {
            Key = "role-packages", DisplayName = "RolePackages", Category = "Innhold",
            AuditKey = "audit-role-packages",
            CountQuery = db => db.RolePackages.CountAsync(),
            Groupings = [DateG(db => db.RolePackages)],
        },

        new()
        {
            Key = "role-resources", DisplayName = "RoleResources", Category = "Innhold",
            AuditKey = "audit-role-resources",
            CountQuery = db => db.RoleResources.CountAsync(),
            Groupings = [DateG(db => db.RoleResources)],
        },

        new()
        {
            Key = "providers", DisplayName = "Providers", Category = "Innhold",
            AuditKey = "audit-providers",
            CountQuery = db => db.Providers.CountAsync(),
            Groupings =
            [
                G("Type", db => ByName(
                    db.Providers.Join(db.ProviderTypes, p => p.TypeId, t => t.Id, (_, t) => (string?)t.Name))),
                DateG(db => db.Providers),
            ],
        },

        new()
        {
            Key = "provider-types", DisplayName = "ProviderTypes", Category = "Innhold",
            AuditKey = "audit-provider-types",
            CountQuery = db => db.ProviderTypes.CountAsync(),
            Groupings = [DateG(db => db.ProviderTypes)],
        },

        new()
        {
            Key = "areas", DisplayName = "Areas", Category = "Innhold",
            AuditKey = "audit-areas",
            CountQuery = db => db.Areas.CountAsync(),
            Groupings =
            [
                G("AreaGroup", db => ByName(
                    db.Areas.Join(db.AreaGroups, a => a.GroupId, g => g.Id, (_, g) => (string?)g.Name))),
                DateG(db => db.Areas),
            ],
        },

        new()
        {
            Key = "area-groups", DisplayName = "AreaGroups", Category = "Innhold",
            AuditKey = "audit-area-groups",
            CountQuery = db => db.AreaGroups.CountAsync(),
            Groupings = [DateG(db => db.AreaGroups)],
        },

        // ── System ─────────────────────────────────────────────────────────
        new()
        {
            Key = "outbox-messages", DisplayName = "OutboxMessages", Category = "System",
            CountQuery = db => db.OutboxMessages.CountAsync(),
            Groupings =
            [
                G("Handler", async db => await db.OutboxMessages
                    .GroupBy(o => o.Handler)
                    .Select(g => new TableGroupRow { Key = g.Key, Count = g.Count() })
                    .OrderByDescending(r => r.Count).ToListAsync()),
                G("Status", async db => (await db.OutboxMessages
                    .GroupBy(o => o.Status)
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .ToListAsync())
                    .ConvertAll(g => new TableGroupRow(g.Key.ToString(), g.Count))),
                G("Dato (opprettet)", async db =>
                {
                    var raw = await db.OutboxMessages
                        .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                        .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                        .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
                        .ToListAsync();
                    return raw.ConvertAll(g => new TableGroupRow($"{g.Year:D4}-{g.Month:D2}", g.Count));
                }),
            ],
        },

        new()
        {
            Key = "outbox-message-logs", DisplayName = "OutboxMessageLogs", Category = "System",
            CountQuery = db => db.OutboxMessageLogs.CountAsync(),
        },

        new()
        {
            Key = "translation-entries", DisplayName = "TranslationEntries", Category = "System",
            CountQuery = db => db.TranslationEntries.CountAsync(),
        },

        new()
        {
            Key = "error-queue", DisplayName = "ErrorQueue", Category = "System",
            CountQuery = db => db.ErrorQueue.CountAsync(),
        },

        new()
        {
            Key = "right-import-progress", DisplayName = "RightImportProgress", Category = "System",
            CountQuery = db => db.RightImportProgress.CountAsync(),
        },

        // ── Historikk ──────────────────────────────────────────────────────
        new()
        {
            Key = "audit-entities", DisplayName = "AuditEntities", Category = "Historikk",
            CountQuery = db => db.AuditEntities.CountAsync(),
            Groupings = [DateG(db => db.AuditEntities)],
        },
        new()
        {
            Key = "audit-entity-lookups", DisplayName = "AuditEntityLookups", Category = "Historikk",
            CountQuery = db => db.AuditEntityLookups.CountAsync(),
            Groupings = [DateG(db => db.AuditEntityLookups)],
        },
        new()
        {
            Key = "audit-entity-types", DisplayName = "AuditEntityTypes", Category = "Historikk",
            CountQuery = db => db.AuditEntityTypes.CountAsync(),
            Groupings = [DateG(db => db.AuditEntityTypes)],
        },
        new()
        {
            Key = "audit-entity-variants", DisplayName = "AuditEntityVariants", Category = "Historikk",
            CountQuery = db => db.AuditEntityVariants.CountAsync(),
            Groupings = [DateG(db => db.AuditEntityVariants)],
        },
        new()
        {
            Key = "audit-entity-variant-roles", DisplayName = "AuditEntityVariantRoles", Category = "Historikk",
            CountQuery = db => db.AuditEntityVariantRoles.CountAsync(),
            Groupings = [DateG(db => db.AuditEntityVariantRoles)],
        },
        new()
        {
            Key = "audit-assignments", DisplayName = "AuditAssignments", Category = "Historikk",
            CountQuery = db => db.AuditAssignments.CountAsync(),
            Groupings = [DateG(db => db.AuditAssignments)],
        },
        new()
        {
            Key = "audit-assignment-packages", DisplayName = "AuditAssignmentPackages", Category = "Historikk",
            CountQuery = db => db.AuditAssignmentPackages.CountAsync(),
            Groupings = [DateG(db => db.AuditAssignmentPackages)],
        },
        new()
        {
            Key = "audit-assignment-resources", DisplayName = "AuditAssignmentResources", Category = "Historikk",
            CountQuery = db => db.AuditAssignmentResources.CountAsync(),
            Groupings = [DateG(db => db.AuditAssignmentResources)],
        },
        new()
        {
            Key = "audit-delegations", DisplayName = "AuditDelegations", Category = "Historikk",
            CountQuery = db => db.AuditDelegations.CountAsync(),
            Groupings = [DateG(db => db.AuditDelegations)],
        },
        new()
        {
            Key = "audit-delegation-packages", DisplayName = "AuditDelegationPackages", Category = "Historikk",
            CountQuery = db => db.AuditDelegationPackages.CountAsync(),
            Groupings = [DateG(db => db.AuditDelegationPackages)],
        },
        new()
        {
            Key = "audit-delegation-resources", DisplayName = "AuditDelegationResources", Category = "Historikk",
            CountQuery = db => db.AuditDelegationResources.CountAsync(),
            Groupings = [DateG(db => db.AuditDelegationResources)],
        },
        new()
        {
            Key = "audit-request-assignments", DisplayName = "AuditRequestAssignments", Category = "Historikk",
            CountQuery = db => db.AuditRequestAssignments.CountAsync(),
            Groupings = [DateG(db => db.AuditRequestAssignments)],
        },
        new()
        {
            Key = "audit-request-assignment-packages", DisplayName = "AuditRequestAssignmentPackages", Category = "Historikk",
            CountQuery = db => db.AuditRequestAssignmentPackages.CountAsync(),
            Groupings = [DateG(db => db.AuditRequestAssignmentPackages)],
        },
        new()
        {
            Key = "audit-request-assignment-resources", DisplayName = "AuditRequestAssignmentResources", Category = "Historikk",
            CountQuery = db => db.AuditRequestAssignmentResources.CountAsync(),
            Groupings = [DateG(db => db.AuditRequestAssignmentResources)],
        },
        new()
        {
            Key = "audit-packages", DisplayName = "AuditPackages", Category = "Historikk",
            CountQuery = db => db.AuditPackages.CountAsync(),
            Groupings = [DateG(db => db.AuditPackages)],
        },
        new()
        {
            Key = "audit-package-resources", DisplayName = "AuditPackageResources", Category = "Historikk",
            CountQuery = db => db.AuditPackageResources.CountAsync(),
            Groupings = [DateG(db => db.AuditPackageResources)],
        },
        new()
        {
            Key = "audit-resources", DisplayName = "AuditResources", Category = "Historikk",
            CountQuery = db => db.AuditResources.CountAsync(),
            Groupings = [DateG(db => db.AuditResources)],
        },
        new()
        {
            Key = "audit-resource-types", DisplayName = "AuditResourceTypes", Category = "Historikk",
            CountQuery = db => db.AuditResourceTypes.CountAsync(),
            Groupings = [DateG(db => db.AuditResourceTypes)],
        },
        new()
        {
            Key = "audit-roles", DisplayName = "AuditRoles", Category = "Historikk",
            CountQuery = db => db.AuditRoles.CountAsync(),
            Groupings = [DateG(db => db.AuditRoles)],
        },
        new()
        {
            Key = "audit-role-maps", DisplayName = "AuditRoleMaps", Category = "Historikk",
            CountQuery = db => db.AuditRoleMaps.CountAsync(),
            Groupings = [DateG(db => db.AuditRoleMaps)],
        },
        new()
        {
            Key = "audit-role-packages", DisplayName = "AuditRolePackages", Category = "Historikk",
            CountQuery = db => db.AuditRolePackages.CountAsync(),
            Groupings = [DateG(db => db.AuditRolePackages)],
        },
        new()
        {
            Key = "audit-role-resources", DisplayName = "AuditRoleResources", Category = "Historikk",
            CountQuery = db => db.AuditRoleResources.CountAsync(),
            Groupings = [DateG(db => db.AuditRoleResources)],
        },
        new()
        {
            Key = "audit-providers", DisplayName = "AuditProviders", Category = "Historikk",
            CountQuery = db => db.AuditProviders.CountAsync(),
            Groupings = [DateG(db => db.AuditProviders)],
        },
        new()
        {
            Key = "audit-provider-types", DisplayName = "AuditProviderTypes", Category = "Historikk",
            CountQuery = db => db.AuditProviderTypes.CountAsync(),
            Groupings = [DateG(db => db.AuditProviderTypes)],
        },
        new()
        {
            Key = "audit-areas", DisplayName = "AuditAreas", Category = "Historikk",
            CountQuery = db => db.AuditAreas.CountAsync(),
            Groupings = [DateG(db => db.AuditAreas)],
        },
        new()
        {
            Key = "audit-area-groups", DisplayName = "AuditAreaGroups", Category = "Historikk",
            CountQuery = db => db.AuditAreaGroups.CountAsync(),
            Groupings = [DateG(db => db.AuditAreaGroups)],
        },
    ];
}
