using System.Diagnostics;
using System.Reflection;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.Tools;

/// <summary>
/// Compares hardcoded constants against actual database content per environment
/// and builds fixes (insert/update/delete) for the deviations.
/// </summary>
public sealed class ConstantsCheckService(IEnvironmentDbContextFactory dbFactory)
{
    /// <summary>
    /// Runs all constant checks in parallel, each against its own context.
    /// </summary>
    public async Task<IReadOnlyList<CheckResult>> RunAllChecksAsync(string environment, CancellationToken ct = default)
    {
        // Every Check*Async method must be listed here to run; the assert below
        // catches a written-but-unregistered check in debug builds.
        Func<string, CancellationToken, Task<CheckResult>>[] checks =
        [
            CheckEntityTypesAsync,
            CheckEntityVariantsAsync,
            CheckAreaGroupsAsync,
            CheckAreasAsync,
            CheckProviderTypesAsync,
            CheckProvidersAsync,
            CheckSystemEntitiesAsync,
            CheckRolesAsync,
            CheckPackagesAsync,
        ];

        Debug.Assert(
            checks.Length == typeof(ConstantsCheckService)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Count(m => m.Name.StartsWith("Check", StringComparison.Ordinal) && m.Name.EndsWith("Async", StringComparison.Ordinal)),
            "Every Check*Async method must be registered in the checks list above.");

        return await Task.WhenAll(checks.Select(check => check(environment, ct)));
    }

    /// <summary>
    /// Executes an issue's fix delegate against a fresh context for the environment.
    /// </summary>
    public async Task ExecuteFixAsync(string environment, FixableIssue issue)
    {
        if (issue.Fix is null)
        {
            throw new InvalidOperationException("Issue has no fix.");
        }

        using var db = dbFactory.CreateContext(environment);
        await issue.Fix(db);
    }

    // ── Individual checks ────────────────────────────────────────────────────
    private async Task<CheckResult> CheckEntityTypesAsync(string environment, CancellationToken ct)
    {
        var constants = EntityTypeConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.EntityTypes.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // EntityType is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("EntityTypeConstants", "entity_type",
            constants.Select(c => (c.Id, Label: c.Entity.Name)),
            dbRows.Select(r => (r.Id, Label: r.Name)),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.EntityTypes.Add(new EntityType
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.EntityTypes
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.EntityTypes.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckEntityVariantsAsync(string environment, CancellationToken ct)
    {
        var constants = EntityVariantConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.EntityVariants.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // EntityVariant is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("EntityVariantConstants", "entity_variant",
            constants.Select(c => (c.Id, Label: c.Entity.Name)),
            dbRows.Select(r => (r.Id, Label: r.Name)),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.EntityVariants.Add(new EntityVariant
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    Description = c.Entity.Description,
                    TypeId = c.Entity.TypeId,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.EntityVariants
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
            AddMismatch(result, c.Id, "Description", c.Entity.Description, dbRow.Description,
                async dbCtx => await dbCtx.EntityVariants
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Description, c.Entity.Description)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.EntityVariants.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckAreaGroupsAsync(string environment, CancellationToken ct)
    {
        var constants = AreaGroupConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.AreaGroups.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // AreaGroup is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("AreaGroupConstants", "area_group",
            constants.Select(c => (c.Id, Label: c.Entity.Name)),
            dbRows.Select(r => (r.Id, Label: r.Name)),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.AreaGroups.Add(new AreaGroup
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    Description = c.Entity.Description,
                    EntityTypeId = c.Entity.EntityTypeId,
                    Urn = c.Entity.Urn,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.AreaGroups
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
            AddMismatch(result, c.Id, "Description", c.Entity.Description, dbRow.Description,
                async dbCtx => await dbCtx.AreaGroups
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Description, c.Entity.Description)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.AreaGroups.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckAreasAsync(string environment, CancellationToken ct)
    {
        var constants = AreaConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.Areas.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // Area is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("AreaConstants", "area",
            constants.Select(c => (c.Id, Label: c.Entity.Name)),
            dbRows.Select(r => (r.Id, Label: r.Name)),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.Areas.Add(new Area
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    Description = c.Entity.Description,
                    GroupId = c.Entity.GroupId,
                    Urn = c.Entity.Urn,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.Areas
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
            AddMismatch(result, c.Id, "Description", c.Entity.Description, dbRow.Description,
                async dbCtx => await dbCtx.Areas
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Description, c.Entity.Description)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.Areas.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckProviderTypesAsync(string environment, CancellationToken ct)
    {
        var constants = ProviderTypeConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.ProviderTypes.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // ProviderType is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("ProviderTypeConstants", "provider_type",
            constants.Select(c => (c.Id, Label: c.Entity.Name)),
            dbRows.Select(r => (r.Id, Label: r.Name)),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.ProviderTypes.Add(new ProviderType
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.ProviderTypes
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.ProviderTypes.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckProvidersAsync(string environment, CancellationToken ct)
    {
        var constants = ProviderConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.Providers.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        var result = IdCompare("ProviderConstants", "provider",
            constants.Select(c => (c.Id, Label: $"{c.Entity.Code} ({c.Entity.Name})")),
            dbRows.Select(r => (r.Id, Label: $"{r.Code} ({r.Name})")));

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.Providers.Add(new Provider
                {
                    Id = c.Entity.Id,
                    Code = c.Entity.Code,
                    Name = c.Entity.Name,
                    TypeId = c.Entity.TypeId,
                    RefId = c.Entity.RefId,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Code", c.Entity.Code, dbRow.Code,
                async dbCtx => await dbCtx.Providers
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Code, c.Entity.Code)));
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.Providers
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
        }

        return result;
    }

    private async Task<CheckResult> CheckSystemEntitiesAsync(string environment, CancellationToken ct)
    {
        var constants = SystemEntityConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var ids = constants.Select(c => c.Id).ToList();
        var dbRows = await db.Entities.AsNoTracking().Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        var internalTypeId = EntityTypeConstants.Internal.Id;
        var allInternal = await db.Entities.AsNoTracking()
            .Where(e => e.TypeId == internalTypeId)
            .ToListAsync(ct);

        var result = IdCompare("SystemEntityConstants", "entity (Internal)",
            constants.Select(c => (c.Id, Label: $"{c.Entity.RefId} — {c.Entity.Name}")),
            dbRows.Select(r => (r.Id, Label: $"{r.RefId} — {r.Name}")));

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.Entities.Add(new Entity
                {
                    Id = c.Entity.Id,
                    Name = c.Entity.Name,
                    RefId = c.Entity.RefId,
                    TypeId = c.Entity.TypeId,
                    VariantId = c.Entity.VariantId,
                    ParentId = c.Entity.ParentId,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Name", c.Entity.Name, dbRow.Name,
                async dbCtx => await dbCtx.Entities
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Name, c.Entity.Name)));
            AddMismatch(result, c.Id, "RefId", c.Entity.RefId, dbRow.RefId,
                async dbCtx => await dbCtx.Entities
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.RefId, c.Entity.RefId)));
        }

        // Extra: internal entities in DB not defined in constants — no auto-fix
        var constIds = constants.Select(c => c.Id).ToHashSet();
        foreach (var e in allInternal.Where(e => !constIds.Contains(e.Id)))
        {
            result.Extra.Add(new FixableIssue
            {
                EntityId = e.Id,
                Description = $"{e.Id}  {e.RefId} — {e.Name}",
            });
        }

        return result;
    }

    private async Task<CheckResult> CheckRolesAsync(string environment, CancellationToken ct)
    {
        var constants = RoleConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.Roles.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // Role is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("RoleConstants", "role",
            constants.Select(c => (c.Id, Label: $"{c.Entity.Code} — {c.Entity.Name}")),
            dbRows.Select(r => (r.Id, Label: $"{r.Code} — {r.Name}")),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.Roles.Add(new Role
                {
                    Id = c.Entity.Id,
                    Code = c.Entity.Code,
                    Name = c.Entity.Name,
                    Description = c.Entity.Description,
                    Urn = c.Entity.Urn,
                    IsKeyRole = c.Entity.IsKeyRole,
                    IsAssignable = c.Entity.IsAssignable,
                    ProviderId = c.Entity.ProviderId,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Code", c.Entity.Code, dbRow.Code,
                async dbCtx => await dbCtx.Roles
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Code, c.Entity.Code)));
            AddMismatch(result, c.Id, "Urn", c.Entity.Urn, dbRow.Urn,
                async dbCtx => await dbCtx.Roles
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Urn, c.Entity.Urn)));
            AddMismatch(result, c.Id, "Description", c.Entity.Description, dbRow.Description,
                async dbCtx => await dbCtx.Roles
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Description, c.Entity.Description)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.Roles.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    private async Task<CheckResult> CheckPackagesAsync(string environment, CancellationToken ct)
    {
        var constants = PackageConstants.AllEntities().ToList();
        using var db = dbFactory.CreateContext(environment);
        var dbRows = await db.Packages.AsNoTracking().ToListAsync(ct);
        var dbById = dbRows.ToDictionary(r => r.Id);

        // Package is fully controlled by constants — extra DB rows may be deleted.
        var result = IdCompare("PackageConstants", "package",
            constants.Select(c => (c.Id, Label: $"{c.Entity.Code} — {c.Entity.Name}")),
            dbRows.Select(r => (r.Id, Label: $"{r.Code} — {r.Name}")),
            allowDeleteExtra: true);

        foreach (var issue in result.Missing)
        {
            var c = constants.First(x => x.Id == issue.EntityId);
            issue.Fix = async dbCtx =>
            {
                dbCtx.Packages.Add(new Package
                {
                    Id = c.Entity.Id,
                    Code = c.Entity.Code,
                    Name = c.Entity.Name,
                    Description = c.Entity.Description,
                    Urn = c.Entity.Urn,
                    AreaId = c.Entity.AreaId,
                    Audit_ValidFrom = DateTimeOffset.UtcNow,
                });
                await dbCtx.SaveChangesAsync();
            };
        }

        foreach (var c in constants.Where(x => dbById.ContainsKey(x.Id)))
        {
            var dbRow = dbById[c.Id];
            AddMismatch(result, c.Id, "Code", c.Entity.Code, dbRow.Code,
                async dbCtx => await dbCtx.Packages
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Code, c.Entity.Code)));
            AddMismatch(result, c.Id, "Urn", c.Entity.Urn, dbRow.Urn,
                async dbCtx => await dbCtx.Packages
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Urn, c.Entity.Urn)));
            AddMismatch(result, c.Id, "Description", c.Entity.Description, dbRow.Description,
                async dbCtx => await dbCtx.Packages
                    .Where(e => e.Id == c.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Description, c.Entity.Description)));
        }

        foreach (var issue in result.Extra)
        {
            var id = issue.EntityId;
            issue.Fix = async dbCtx =>
                await dbCtx.Packages.Where(e => e.Id == id).ExecuteDeleteAsync();
        }

        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static CheckResult IdCompare(
        string name,
        string table,
        IEnumerable<(Guid Id, string Label)> constants,
        IEnumerable<(Guid Id, string Label)> dbRows,
        bool allowDeleteExtra = false)
    {
        var constList = constants.ToList();
        var dbList = dbRows.ToList();
        var dbIds = dbList.Select(r => r.Id).ToHashSet();
        var constIds = constList.Select(c => c.Id).ToHashSet();

        return new CheckResult
        {
            Name = name,
            Table = table,
            ConstantsCount = constList.Count,
            DbCount = dbList.Count,
            AllowDeleteExtra = allowDeleteExtra,
            Missing = constList
                .Where(c => !dbIds.Contains(c.Id))
                .Select(c => new FixableIssue { EntityId = c.Id, Description = $"{c.Id}  {c.Label}" })
                .ToList(),
            Extra = dbList
                .Where(r => !constIds.Contains(r.Id))
                .Select(r => new FixableIssue { EntityId = r.Id, Description = $"{r.Id}  {r.Label}" })
                .ToList(),
            Expanded = constList.Any(c => !dbIds.Contains(c.Id)),
        };
    }

    private static void AddMismatch(
        CheckResult result,
        Guid id,
        string field,
        string? expected,
        string? actual,
        Func<AppDbContext, Task>? fix = null)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return;
        }

        result.Mismatches.Add(new FixableIssue
        {
            EntityId = id,
            Description = $"{id}  [{field}]  kode: {expected}  |  DB: {actual}",
            Fix = fix,
        });
        result.Expanded = true;
    }
}
